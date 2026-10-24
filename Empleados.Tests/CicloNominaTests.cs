using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Empleados.Tests;

public class CicloNominaTests
{
    // Quincenales: 1 = 60,000 con datos completos; 2 = 35,000 sin cédula ni NSS. Mensual: 3 = 45,000.
    private const int Completo = 1, SinDocumento = 2, Mensual = 3;

    private static (EmpleadosDbContext Context, CicloNominaService Ciclo, DbContextOptions<EmpleadosDbContext> Options) Crear(int empresaActual = 1)
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated(); // parámetros legales 2026
            semilla.Empresas.AddRange(
                new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m },
                new Empresa { Id = 2, Rnc = "101000002", RazonSocial = "Dos SRL" });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.AddRange(
                Empleado(Completo, 60000, FrecuenciaPago.Quincenal, "00112345678", "111"),
                Empleado(SinDocumento, 35000, FrecuenciaPago.Quincenal, null, null),
                Empleado(Mensual, 45000, FrecuenciaPago.Mensual, "00187654321", "333"));
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo(empresaId: empresaActual);
        var context = new EmpleadosDbContext(options, contexto);
        var ciclo = new CicloNominaService(context, new NominaService(context, new ParametrosLegalesService(context)), contexto);
        return (context, ciclo, options);
    }

    private static Empleado Empleado(int id, decimal salario, FrecuenciaPago frecuencia, string? documento, string? nss) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = 1, Nombre = "N" + id, Apellido = "A", CorreoElectronico = $"e{id}@test.com",
        Telefono = "1", Direccion = "d", Salario = salario, FechaNacimiento = new DateTime(1990, 1, 1),
        FechaContratacion = new DateTime(2020, 1, 1), FrecuenciaPago = frecuencia, NumeroDocumento = documento, Nss = nss
    };

    private static CrearNominaDto Octubre(int quincena) => new() { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Quincenal, Quincena = quincena };

    private static async Task<NominaPeriodo> CrearCalculada(CicloNominaService ciclo, int quincena)
    {
        var resultado = await ciclo.CrearAsync(Octubre(quincena));
        Assert.True(resultado.Ok, resultado.Mensaje);
        return (await ciclo.ObtenerAsync(resultado.Valor!.Id))!;
    }

    private static async Task CompletarDatos(EmpleadosDbContext context)
    {
        var empleado = await context.Empleados.FirstAsync(e => e.Id == SinDocumento);
        empleado.NumeroDocumento = "00122223333";
        empleado.Nss = "222";
        await context.SaveChangesAsync();
    }

    private static decimal Linea(ReciboPago recibo, string codigo) => recibo.Lineas.Where(l => l.Codigo == codigo).Sum(l => l.Monto);

    [Fact]
    public async Task Crear_IncluyeSoloEmpleadosDeLaFrecuencia_YCalcula()
    {
        var (context, ciclo, _) = Crear();

        var nomina = await CrearCalculada(ciclo, 1);

        Assert.Equal(EstadoNomina.Calculada, nomina.Estado);
        Assert.Equal(new[] { Completo, SinDocumento }, nomina.Recibos.Select(r => r.EmpleadoId).OrderBy(i => i));
        var recibo = nomina.Recibos.Single(r => r.EmpleadoId == Completo);
        Assert.Equal(1743.33m, Linea(recibo, Codigos.Isr)); // caso golden C
        Assert.Equal(26483.67m, recibo.Neto);
        Assert.Equal(nomina.Recibos.Sum(r => r.Neto), nomina.TotalNeto);
        Assert.Equal("00112345678", recibo.Documento); // copia del empleado
    }

    [Fact]
    public async Task Aprobar_SeBloqueaSiFaltanDatosDeTss_HastaCompletarlosYRecalcular()
    {
        var (context, ciclo, _) = Crear();
        var nomina = await CrearCalculada(ciclo, 1);

        Assert.Equal(TipoResultado.Invalido, (await ciclo.AprobarAsync(nomina.Id)).Tipo);
        Assert.NotNull(nomina.Recibos.Single(r => r.EmpleadoId == SinDocumento).ErroresJson);

        await CompletarDatos(context);
        Assert.True((await ciclo.CalcularAsync(nomina.Id)).Ok);
        Assert.True((await ciclo.AprobarAsync(nomina.Id)).Ok);
    }

    [Fact]
    public async Task Aprobada_NoAceptaNovedadesNiRecalculo_HastaReabrir()
    {
        var (context, ciclo, _) = Crear();
        await CompletarDatos(context);
        var nomina = await CrearCalculada(ciclo, 1);
        await ciclo.AprobarAsync(nomina.Id);
        var reciboId = nomina.Recibos.First().Id;

        Assert.Equal(TipoResultado.Invalido, (await ciclo.GuardarNovedadesAsync(reciboId, new NovedadesReciboDto { HorasExtra = 2 })).Tipo);
        Assert.Equal(TipoResultado.Invalido, (await ciclo.CalcularAsync(nomina.Id)).Tipo);

        Assert.True((await ciclo.ReabrirAsync(nomina.Id)).Ok);
        Assert.True((await ciclo.GuardarNovedadesAsync(reciboId, new NovedadesReciboDto { HorasExtra = 2 })).Ok);
    }

    [Fact]
    public async Task Novedades_SeGuardanYRecalculanElRecibo()
    {
        var (context, ciclo, _) = Crear();
        var nomina = await CrearCalculada(ciclo, 1);
        var recibo = nomina.Recibos.Single(r => r.EmpleadoId == Completo);

        var resultado = await ciclo.GuardarNovedadesAsync(recibo.Id, new NovedadesReciboDto
        {
            OtrosIngresos = { new IngresoSimuladoDto { Concepto = "Bono", Monto = 10000 }, new IngresoSimuladoDto() },
            Deducciones = { new DeduccionSimuladaDto { Concepto = "Uniforme", Monto = 500 } }
        });

        Assert.True(resultado.Ok, resultado.Mensaje);
        recibo = (await ciclo.ObtenerReciboAsync(recibo.Id))!;
        Assert.Equal(40000m, recibo.TotalIngresos);
        Assert.Equal(500m, Linea(recibo, Codigos.Deduccion));
        Assert.Equal(2, recibo.Novedades.Count);
    }

    [Fact]
    public async Task SegundaQuincena_NecesitaLaPrimeraAprobada_YUsaLoAcumulado()
    {
        var (context, ciclo, _) = Crear();
        await CompletarDatos(context);
        var primera = await CrearCalculada(ciclo, 1);

        var segunda = await ciclo.CrearAsync(Octubre(2));
        Assert.Contains("Apruebe la 1.ª quincena", segunda.Mensaje);

        await ciclo.AprobarAsync(primera.Id);
        Assert.True((await ciclo.CalcularAsync(segunda.Valor!.Id)).Ok);
        var recibo = (await ciclo.ObtenerAsync(segunda.Valor.Id))!.Recibos.Single(r => r.EmpleadoId == Completo);
        Assert.Equal(1743.32m, Linea(recibo, Codigos.Isr)); // caso golden C
        Assert.Equal(26483.68m, recibo.Neto);

        // Con la 2.ª creada, la 1.ª ya no se puede reabrir
        Assert.Equal(TipoResultado.Invalido, (await ciclo.ReabrirAsync(primera.Id)).Tipo);
    }

    [Fact]
    public async Task SegundaQuincenaSinPrimeraRegistrada_CalculaYAdvierte()
    {
        var (context, ciclo, _) = Crear();

        var segunda = await CrearCalculada(ciclo, 2);

        Assert.Contains(CicloNominaService.Lista(segunda.Recibos.First().AdvertenciasJson), a => a.Contains("No hay 1.ª quincena"));
    }

    [Fact]
    public async Task Prestamo_SeDescuentaPorCuotas_YElSaldoBajaAlPagar_SinDescontarDeMas()
    {
        var (context, ciclo, _) = Crear();
        await CompletarDatos(context);
        var prestamos = new PrestamoService(context);
        var prestamo = (await prestamos.CrearAsync(new PrestamoDto
        {
            EmpleadoId = Completo, Concepto = "Préstamo", MontoTotal = 3000, MontoCuota = 2000, FechaInicio = new DateTime(2026, 10, 1)
        })).Valor!;

        var primera = await CrearCalculada(ciclo, 1);
        Assert.Equal(2000m, Linea(primera.Recibos.Single(r => r.EmpleadoId == Completo), Codigos.Deduccion));
        await ciclo.AprobarAsync(primera.Id);

        // La 1.ª está aprobada pero sin pagar: la 2.ª solo puede descontar lo que queda (1,000)
        var segunda = await CrearCalculada(ciclo, 2);
        Assert.Equal(1000m, Linea(segunda.Recibos.Single(r => r.EmpleadoId == Completo), Codigos.Deduccion));

        Assert.True((await ciclo.PagarAsync(primera.Id, new DateTime(2026, 10, 15))).Ok);
        Assert.Equal(1000m, (await context.Prestamos.FindAsync(prestamo.Id))!.Saldo);
        await ciclo.AprobarAsync(segunda.Id);
        await ciclo.PagarAsync(segunda.Id, new DateTime(2026, 10, 30));
        var liquidado = (await context.Prestamos.FindAsync(prestamo.Id))!;
        Assert.Equal(0m, liquidado.Saldo);
        Assert.False(liquidado.Activo);
    }

    [Fact]
    public async Task PrestamoSoloFinDeMes_NoSeDescuentaEnLaPrimeraQuincena()
    {
        var (context, ciclo, _) = Crear();
        await CompletarDatos(context);
        await new PrestamoService(context).CrearAsync(new PrestamoDto
        {
            EmpleadoId = Completo, Concepto = "Cooperativa", MontoTotal = 10000, MontoCuota = 1000,
            FechaInicio = new DateTime(2026, 10, 1), SoloFinDeMes = true
        });

        var primera = await CrearCalculada(ciclo, 1);
        await ciclo.AprobarAsync(primera.Id);
        var segunda = await CrearCalculada(ciclo, 2);

        Assert.Equal(0m, Linea(primera.Recibos.Single(r => r.EmpleadoId == Completo), Codigos.Deduccion));
        Assert.Equal(1000m, Linea(segunda.Recibos.Single(r => r.EmpleadoId == Completo), Codigos.Deduccion));
    }

    [Fact]
    public async Task Estados_SiguenElOrden_YPagadaEsInmutable()
    {
        var (context, ciclo, _) = Crear();
        await CompletarDatos(context);
        var nomina = await CrearCalculada(ciclo, 1);

        Assert.Equal(TipoResultado.Invalido, (await ciclo.CrearAsync(Octubre(1))).Tipo); // período repetido
        Assert.Equal(TipoResultado.Invalido, (await ciclo.PagarAsync(nomina.Id, DateTime.Today)).Tipo);
        Assert.Equal(TipoResultado.Invalido, (await ciclo.CerrarAsync(nomina.Id)).Tipo);
        await ciclo.AprobarAsync(nomina.Id);
        await ciclo.PagarAsync(nomina.Id, new DateTime(2026, 10, 15));

        Assert.Equal(TipoResultado.Invalido, (await ciclo.CalcularAsync(nomina.Id)).Tipo);
        Assert.Equal(TipoResultado.Invalido, (await ciclo.ReabrirAsync(nomina.Id)).Tipo);
        Assert.Equal(TipoResultado.Invalido, (await ciclo.EliminarAsync(nomina.Id)).Tipo);
        Assert.True((await ciclo.CerrarAsync(nomina.Id)).Ok);
    }

    [Fact]
    public async Task Eliminar_SoloEnBorradorOCalculada()
    {
        var (context, ciclo, _) = Crear();
        var nomina = await CrearCalculada(ciclo, 1);

        Assert.True((await ciclo.EliminarAsync(nomina.Id)).Ok);
        Assert.Empty(await context.Nominas.ToListAsync());
    }

    [Fact]
    public async Task NominaDeOtraEmpresa_NoSeVe()
    {
        var (_, ciclo, options) = Crear();
        var nomina = await CrearCalculada(ciclo, 1);

        var contextoDos = new ContextoFijo(empresaId: 2);
        using var contextDos = new EmpleadosDbContext(options, contextoDos);
        var cicloDos = new CicloNominaService(contextDos, new NominaService(contextDos, new ParametrosLegalesService(contextDos)), contextoDos);

        Assert.Empty(await cicloDos.ListarAsync());
        Assert.Null(await cicloDos.ObtenerAsync(nomina.Id));
        Assert.Equal(TipoResultado.NoEncontrado, (await cicloDos.EliminarAsync(nomina.Id)).Tipo);
    }

    [Fact]
    public async Task ReciboPdf_GeneraUnPdfConUnaPaginaPorEmpleado()
    {
        var (context, ciclo, _) = Crear();
        var nomina = await CrearCalculada(ciclo, 1);
        var empresa = await context.Empresas.FirstAsync(e => e.Id == 1);

        var pdf = ReciboPdf.Generar(empresa, nomina, nomina.Recibos);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(pdf.Length > 1000);
    }
}
