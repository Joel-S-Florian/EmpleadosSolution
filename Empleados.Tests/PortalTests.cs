using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace Empleados.Tests;

// Guarda los correos en memoria; falla con las direcciones que contienen "falla"
internal class EnviadorFalso : IEnviadorCorreo
{
    // Destino y tipos de los adjuntos: el mensaje se libera después del envío
    public List<(string Destino, List<string> Adjuntos)> Enviados { get; } = new();

    public Task EnviarAsync(MailMessage mensaje)
    {
        if (mensaje.To.Any(t => t.Address.Contains("falla")))
            throw new SmtpException("Buzón no disponible");
        Enviados.Add((mensaje.To.Single().Address, mensaje.Attachments.Select(a => a.ContentType.MediaType).ToList()));
        return Task.CompletedTask;
    }
}

public class PortalTests
{
    private const string UsuarioPortal = "portal1", UsuarioRrhh = "rrhh1";

    // 1 = empleado con usuario del portal; 2 = empleado cuyo correo falla
    private static (EmpleadosDbContext Context, CicloNominaService Ciclo, PortalService Portal, RecibosCorreoService Correo, EnviadorFalso Enviador, IncidenciaService Incidencias)
        Crear(string usuarioActual = UsuarioPortal)
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated();
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.AddRange(
                Empleado(1, "ana@test.com", UsuarioPortal, new DateTime(1990, 10, 24)),
                Empleado(2, "falla@test.com", null, new DateTime(1990, 1, 1)));
            semilla.UsuariosEmpresas.AddRange(
                new UsuarioEmpresa { UsuarioId = UsuarioPortal, EmpresaId = 1 },
                new UsuarioEmpresa { UsuarioId = UsuarioRrhh, EmpresaId = 1 });
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo(usuarioId: usuarioActual);
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        var notificaciones = new NotificacionService(context);
        var incidencias = new IncidenciaService(context, parametros, contexto);
        var enviador = new EnviadorFalso();
        return (context, new CicloNominaService(context, new NominaService(context, parametros), contexto, notificaciones),
            new PortalService(context, incidencias, notificaciones, contexto), new RecibosCorreoService(context, enviador), enviador, incidencias);
    }

    private static Empleado Empleado(int id, string correo, string? usuario, DateTime nacimiento) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = 1, Nombre = "N" + id, Apellido = "A", CorreoElectronico = correo,
        Telefono = "1", Direccion = "d", Salario = 45000, FechaNacimiento = nacimiento, UsuarioId = usuario,
        FechaContratacion = new DateTime(2020, 1, 1), FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = $"0010000000{id}", Nss = "N" + id
    };

    private static async Task<NominaPeriodo> Pagada(CicloNominaService ciclo, int mes)
    {
        var nomina = (await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = mes, Frecuencia = FrecuenciaPago.Mensual })).Valor!;
        Assert.True((await ciclo.AprobarAsync(nomina.Id)).Ok);
        Assert.True((await ciclo.PagarAsync(nomina.Id, new DateTime(2026, mes, 28))).Ok);
        return nomina;
    }

    [Fact]
    public async Task Portal_SoloMuestraLosRecibosPagadosDelEmpleado()
    {
        var (_, ciclo, portal, _, _, _) = Crear();
        var septiembre = await Pagada(ciclo, 9);
        await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual }); // sin pagar

        var empleado = await portal.EmpleadoAsync();
        var recibos = await portal.RecibosAsync(empleado!.Id);
        var ajeno = (await ciclo.ObtenerAsync(septiembre.Id))!.Recibos.Single(r => r.EmpleadoId == 2);

        Assert.Equal(1, empleado.Id);
        Assert.Single(recibos);
        Assert.Null(await portal.ReciboAsync(empleado.Id, ajeno.Id));
    }

    [Fact]
    public async Task Solicitud_AvisaSoloARrhh()
    {
        var (context, _, portal, _, _, _) = Crear();
        var empleado = (await portal.EmpleadoAsync())!;

        var falta = await portal.SolicitarAsync(empleado, new IncidenciaDto { Tipo = TipoIncidencia.Falta, FechaInicio = new DateTime(2026, 11, 2), FechaFin = new DateTime(2026, 11, 2) });
        var vacaciones = await portal.SolicitarAsync(empleado, new IncidenciaDto { Tipo = TipoIncidencia.Vacaciones, FechaInicio = new DateTime(2026, 11, 2), FechaFin = new DateTime(2026, 11, 6) });

        Assert.False(falta.Ok);
        Assert.True(vacaciones.Ok, vacaciones.Mensaje);
        var incidencia = await context.Incidencias.SingleAsync();
        Assert.Equal(EstadoIncidencia.Pendiente, incidencia.Estado);
        var avisos = await context.Notificaciones.Where(n => n.Mensaje.Contains("solicitó vacaciones")).ToListAsync();
        Assert.Equal(new string?[] { UsuarioRrhh }, avisos.Select(a => a.UsuarioDestinoId));
    }

    [Fact]
    public async Task RecibosPorCorreo_RegistraEnviadosYFallidos()
    {
        var (context, ciclo, _, correo, enviador, _) = Crear(UsuarioRrhh);
        var nomina = await Pagada(ciclo, 9);

        var resultado = await correo.EnviarNominaAsync(nomina.Id);

        Assert.True(resultado.Ok);
        Assert.Contains("1 enviado(s), 1 fallido(s)", resultado.Mensaje);
        var mensaje = Assert.Single(enviador.Enviados);
        Assert.Equal("ana@test.com", mensaje.Destino);
        Assert.Equal(new[] { "application/pdf" }, mensaje.Adjuntos);
        var envios = await correo.EnviosAsync(nomina.Id);
        Assert.Contains(envios, e => e.Estado == EstadoEnvio.Fallido && e.Error == "Buzón no disponible");
    }

    [Fact]
    public async Task RecibosPorCorreo_NominaSinPagar_NoSeEnvia()
    {
        var (_, ciclo, _, correo, _, _) = Crear(UsuarioRrhh);
        var nomina = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 9, Frecuencia = FrecuenciaPago.Mensual });

        Assert.False((await correo.EnviarNominaAsync(nomina.Valor!.Id)).Ok);
    }

    [Fact]
    public async Task NominaNueva_SinErrores_AvisaQueEstaListaParaAprobar()
    {
        var (context, ciclo, _, _, _, _) = Crear(UsuarioRrhh);

        await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 9, Frecuencia = FrecuenciaPago.Mensual });

        Assert.Contains(await context.Notificaciones.ToListAsync(), n => n.Mensaje.StartsWith("La nómina Mensual 09/2026 está calculada y lista para aprobar") && n.UsuarioDestinoId == UsuarioRrhh);
    }

    [Fact]
    public async Task Recordatorios_CumpleanosUnaVezPorDia()
    {
        var (context, _, _, _, _, incidencias) = Crear(UsuarioRrhh);
        var recordatorios = new RecordatoriosService(context, incidencias, new NotificacionService(context));

        var primera = await recordatorios.GenerarAsync(new DateTime(2026, 10, 24));
        var segunda = await recordatorios.GenerarAsync(new DateTime(2026, 10, 24));

        Assert.Equal(1, primera);
        Assert.Equal(0, segunda);
        Assert.Single(await context.Notificaciones.Where(n => n.Mensaje == "Hoy cumple años N1 A").ToListAsync());
    }

    [Fact]
    public async Task CartaDeTrabajo_SeGenera()
    {
        var (context, _, _, _, _, _) = Crear();
        var empleado = await context.Empleados.Include(e => e.PuestoNav).Include(e => e.Departamento).FirstAsync();

        var pdf = CartaTrabajoPdf.Generar(await context.Empresas.FirstAsync(), empleado, new DateTime(2026, 10, 24));

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
