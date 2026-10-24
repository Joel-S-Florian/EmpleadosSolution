using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class CalendarioLaboralTests
{
    [Fact]
    public void Feriados2026_CoincidenConLosPublicadosPorElMinisterio()
    {
        using var context = new EmpleadosDbContext(new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.Database.EnsureCreated();
        var sembrados = context.Feriados.OrderBy(f => f.Fecha).Select(f => new { f.Fecha, f.Nombre }).ToList();

        var calculados = CalendarioLaboral.Feriados(2026).Select(f => new { f.Fecha, f.Nombre }).ToList();

        Assert.Equal(sembrados, calculados);
    }

    [Fact]
    public void Feriados2027_AplicaLosTrasladosDeLaLey139_97()
    {
        var feriados = CalendarioLaboral.Feriados(2027).ToDictionary(f => f.Nombre, f => f.Fecha);

        Assert.Equal(new DateTime(2027, 1, 4), feriados["Día de los Santos Reyes (trasladado)"]); // miércoles 6 -> lunes 4
        Assert.Equal(new DateTime(2027, 1, 25), feriados["Día de Duarte (trasladado)"]);          // martes 26 -> lunes 25
        Assert.Equal(new DateTime(2027, 5, 1), feriados["Día del Trabajo"]);                      // sábado: no se mueve
        Assert.Equal(new DateTime(2027, 3, 26), feriados["Viernes Santo"]);
        Assert.Equal(new DateTime(2027, 5, 27), feriados["Corpus Christi"]);
        Assert.Equal(new DateTime(2027, 8, 16), feriados["Día de la Restauración"]);              // nunca se mueve
    }

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    public void DomingoDePascua(int anio, int mes, int dia) =>
        Assert.Equal(new DateTime(anio, mes, dia), CalendarioLaboral.DomingoDePascua(anio));

    [Fact]
    public void DiasLaborables_SinFinesDeSemanaNiFeriados()
    {
        Assert.Equal(11, CalendarioLaboral.DiasLaborables(new DateTime(2026, 10, 1), new DateTime(2026, 10, 15), Array.Empty<DateTime>()));
        Assert.Equal(9, CalendarioLaboral.DiasLaborables(new DateTime(2026, 11, 2), new DateTime(2026, 11, 13), new[] { new DateTime(2026, 11, 9) }));
    }

    [Theory]
    [InlineData("2020-01-01", "2026-10-24", 6, 18, "2026-01-01")]
    [InlineData("2021-10-24", "2026-10-24", 5, 18, "2026-10-24")]
    [InlineData("2023-03-15", "2026-03-14", 2, 14, "2025-03-15")]
    [InlineData("2026-01-10", "2026-10-24", 0, 0, "2026-01-10")]
    public void Vacaciones_SegunAniosDeServicio(string ingreso, string fecha, int anios, int dias, string desde)
    {
        var derecho = CalendarioLaboral.Vacaciones(DateTime.Parse(ingreso), DateTime.Parse(fecha), 14, 18);

        Assert.Equal(anios, derecho.AniosServicio);
        Assert.Equal(dias, derecho.Dias);
        Assert.Equal(DateTime.Parse(desde), derecho.Desde);
    }
}

public class IncidenciasTests
{
    // 1 = 45,000 mensual desde 2020 (6 años de servicio); 2 = 30,000 mensual desde marzo de 2026
    private const int Antiguo = 1, Nuevo = 2;

    private static (EmpleadosDbContext Context, IncidenciaService Incidencias, CicloNominaService Ciclo) Crear()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated(); // parámetros y feriados 2026
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.AddRange(
                Empleado(Antiguo, 45000, new DateTime(2020, 1, 1), "00112345678"),
                Empleado(Nuevo, 30000, new DateTime(2026, 3, 1), "00187654321"));
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo();
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        return (context, new IncidenciaService(context, parametros, contexto), new CicloNominaService(context, new NominaService(context, parametros), contexto));
    }

    private static Empleado Empleado(int id, decimal salario, DateTime ingreso, string documento) => new()
    {
        Id = id, EmpresaId = 1, DepartamentoId = 1, Nombre = "N" + id, Apellido = "A", CorreoElectronico = $"e{id}@test.com",
        Telefono = "1", Direccion = "d", Salario = salario, FechaNacimiento = new DateTime(1990, 1, 1),
        FechaContratacion = ingreso, FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = documento, Nss = "N" + id
    };

    private static IncidenciaDto Dto(int empleadoId, TipoIncidencia tipo, string inicio, string fin, decimal horas = 0) => new()
    {
        EmpleadoId = empleadoId, Tipo = tipo, FechaInicio = DateTime.Parse(inicio), FechaFin = DateTime.Parse(fin), Horas = horas
    };

    private static async Task<Incidencia> Aprobada(IncidenciaService servicio, IncidenciaDto dto)
    {
        var creada = await servicio.CrearAsync(dto);
        Assert.True(creada.Ok, creada.Mensaje);
        Assert.True((await servicio.AprobarAsync(creada.Valor!.Id)).Ok);
        return creada.Valor;
    }

    private static async Task<ReciboPago> ReciboOctubre(CicloNominaService ciclo, int empleadoId)
    {
        var nomina = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual });
        Assert.True(nomina.Ok, nomina.Mensaje);
        return (await ciclo.ObtenerAsync(nomina.Valor!.Id))!.Recibos.Single(r => r.EmpleadoId == empleadoId);
    }

    private static decimal Ausencia(ReciboPago recibo) => recibo.Lineas.Where(l => l.Codigo == Codigos.Ausencia).Sum(l => l.Monto);

    [Fact]
    public async Task Falta_CuentaDiasLaborablesSinElFeriado()
    {
        var (_, incidencias, _) = Crear();

        var resultado = await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.Falta, "2026-11-02", "2026-11-13"));

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Equal(9, resultado.Valor!.Dias); // 10 días de lunes a viernes menos el 9 de noviembre
        Assert.True(resultado.Valor.Descontar);
        Assert.Equal(EstadoIncidencia.Pendiente, resultado.Valor.Estado);
    }

    [Fact]
    public async Task Vacaciones_NoPuedenPasarDelSaldo()
    {
        var (_, incidencias, _) = Crear();

        Assert.True((await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.Vacaciones, "2026-10-26", "2026-11-06"))).Ok); // 10 días
        var segunda = await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.Vacaciones, "2026-11-16", "2026-11-27"));
        var sinAnio = await incidencias.CrearAsync(Dto(Nuevo, TipoIncidencia.Vacaciones, "2026-10-26", "2026-10-30"));

        Assert.False(segunda.Ok);
        Assert.Contains("8 días", segunda.Mensaje);
        Assert.False(sinAnio.Ok);
        var saldo = await incidencias.SaldoVacacionesAsync(Antiguo, new DateTime(2026, 10, 24));
        Assert.Equal(18, saldo!.Derecho.Dias);
        Assert.Equal(10, saldo.PorAprobar);
        Assert.Equal(8, saldo.Disponibles);
    }

    [Fact]
    public async Task NoSePermitenIncidenciasSuperpuestas()
    {
        var (_, incidencias, _) = Crear();
        await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.Falta, "2026-10-05", "2026-10-06"));

        var otra = await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.PermisoConGoce, "2026-10-06", "2026-10-06"));

        Assert.False(otra.Ok);
    }

    [Fact]
    public async Task FaltaYRetardoAprobados_SeDescuentanEnLaNomina()
    {
        var (_, incidencias, ciclo) = Crear();
        await Aprobada(incidencias, Dto(Antiguo, TipoIncidencia.Falta, "2026-10-05", "2026-10-06"));
        await Aprobada(incidencias, Dto(Antiguo, TipoIncidencia.Retardo, "2026-10-08", "2026-10-08", horas: 2));
        await incidencias.CrearAsync(Dto(Antiguo, TipoIncidencia.Falta, "2026-10-12", "2026-10-12")); // por aprobar: no cuenta
        await Aprobada(incidencias, Dto(Antiguo, TipoIncidencia.PermisoConGoce, "2026-10-13", "2026-10-13")); // con goce: no cuenta

        var recibo = await ReciboOctubre(ciclo, Antiguo);

        Assert.Equal(2.25m, recibo.DiasIncidencias); // 2 días + 2 h / 8 h
        Assert.Equal(-Math.Round(45000m / 23.83m * 2.25m, 2, MidpointRounding.AwayFromZero), Ausencia(recibo));
        Assert.Contains(CicloNominaService.Lista(recibo.ExplicacionJson), e => e.StartsWith("Incidencias aprobadas: Falta 05/10-06/10"));
    }

    [Fact]
    public async Task IncidenciaQueCruzaDePeriodo_SeRepartePorDiasLaborables()
    {
        var (context, incidencias, ciclo) = Crear();
        var empleado = await context.Empleados.FirstAsync(e => e.Id == Antiguo);
        empleado.FrecuenciaPago = FrecuenciaPago.Quincenal;
        await context.SaveChangesAsync();
        await Aprobada(incidencias, Dto(Antiguo, TipoIncidencia.PermisoSinGoce, "2026-10-13", "2026-10-20")); // 6 días: 3 y 3

        var primera = await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Quincenal, Quincena = 1 });
        var recibo = (await ciclo.ObtenerAsync(primera.Valor!.Id))!.Recibos.Single();

        Assert.Equal(3, recibo.DiasIncidencias);
    }

    [Fact]
    public async Task NoSeEliminaUnaFaltaYaDescontadaEnNominaAprobada()
    {
        var (_, incidencias, ciclo) = Crear();
        var falta = await Aprobada(incidencias, Dto(Antiguo, TipoIncidencia.Falta, "2026-10-05", "2026-10-05"));
        var recibo = await ReciboOctubre(ciclo, Antiguo);
        Assert.True((await ciclo.AprobarAsync(recibo.NominaId)).Ok);

        var eliminar = await incidencias.EliminarAsync(falta.Id);

        Assert.False(eliminar.Ok);
        Assert.Contains("ya se descontó", eliminar.Mensaje);
    }

    [Fact]
    public async Task VacacionesColectivas_OmitenAQuienNoTieneSaldo()
    {
        var (context, incidencias, _) = Crear();

        var resultado = await incidencias.VacacionesColectivasAsync(new VacacionesColectivasDto { FechaInicio = new DateTime(2026, 12, 21), FechaFin = new DateTime(2026, 12, 31) });

        Assert.True(resultado.Ok, resultado.Mensaje);
        Assert.Contains("N2 A", resultado.Mensaje);
        var registrada = await context.Incidencias.SingleAsync();
        Assert.Equal(Antiguo, registrada.EmpleadoId);
        Assert.Equal(EstadoIncidencia.Aprobada, registrada.Estado);
        Assert.Equal(8, registrada.Dias); // 21-31 dic: 9 días de lunes a viernes menos Navidad
    }

    [Fact]
    public async Task GenerarFeriados_AgregaSoloLosQueFaltan()
    {
        var (context, _, _) = Crear();
        var parametros = new ParametrosLegalesService(context);

        Assert.True((await parametros.GenerarFeriadosAsync(2027)).Ok);
        var segunda = await parametros.GenerarFeriadosAsync(2027);

        Assert.Equal(12, await context.Feriados.CountAsync(f => f.Fecha.Year == 2027));
        Assert.Contains("ya estaban", segunda.Mensaje);
    }
}
