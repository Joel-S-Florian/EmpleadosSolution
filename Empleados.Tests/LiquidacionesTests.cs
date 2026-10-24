using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Tests;

public class PrestacionesTests
{
    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    [Fact]
    public void TiempoDeServicio_CuentaElDiaDeSalida()
    {
        Assert.Equal((6, 9, 23), Prestaciones.Tiempo(new DateTime(2020, 1, 1), new DateTime(2026, 10, 23)));
        Assert.Equal((1, 0, 0), Prestaciones.Tiempo(new DateTime(2025, 3, 1), new DateTime(2026, 2, 28)));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(4, 7)]
    [InlineData(8, 14)]
    [InlineData(13, 28)]
    public void Preaviso_Art76(int meses, int dias) => Assert.Equal(dias, Prestaciones.DiasPreaviso(meses));

    [Theory]
    [InlineData(0, 2, 0)]
    [InlineData(0, 4, 6)]
    [InlineData(0, 8, 13)]
    [InlineData(3, 0, 63)]
    [InlineData(3, 4, 69)]
    [InlineData(6, 7, 151)]
    public void Cesantia_Art80(int anios, int meses, int dias) => Assert.Equal(dias, Prestaciones.DiasCesantia(anios, meses));

    [Theory]
    [InlineData(4, 0)]
    [InlineData(5, 6)]
    [InlineData(11, 12)]
    public void VacacionesProporcionales_Art180(int meses, int dias) => Assert.Equal(dias, Prestaciones.DiasVacacionesProporcionales(meses));

    private static EntradaPrestaciones Entrada(CausaTerminacion causa, bool preaviso = false) =>
        new(causa, new DateTime(2020, 1, 1), new DateTime(2026, 10, 23), 45000, preaviso, 10, 427500, 23.83m);

    private static decimal Monto(ResultadoPrestaciones r, string codigo) => r.Lineas.Where(l => l.Codigo == codigo).Sum(l => l.Monto);

    [Fact]
    public void Desahucio_PagaPreavisoCesantiaVacacionesYNavidad()
    {
        var r = Prestaciones.Calcular(Entrada(CausaTerminacion.Desahucio));
        var diario = 45000m / 23.83m;

        Assert.Equal(R(diario * 28), Monto(r, Prestaciones.Codigos.Preaviso));
        Assert.Equal(R(diario * 151), Monto(r, Prestaciones.Codigos.Cesantia)); // 6 años × 23 + 13 por 9 meses
        Assert.Equal(R(diario * 20), Monto(r, Prestaciones.Codigos.Vacaciones)); // 10 pendientes + 10 por 9 meses
        Assert.Equal(35625, Monto(r, Prestaciones.Codigos.Navidad));
        Assert.Equal(0, Monto(r, Prestaciones.Codigos.Asistencia));
    }

    [Fact]
    public void ConPreavisoOtorgado_NoPagaPreaviso()
    {
        var r = Prestaciones.Calcular(Entrada(CausaTerminacion.DespidoInjustificado, preaviso: true));

        Assert.Equal(0, Monto(r, Prestaciones.Codigos.Preaviso));
        Assert.True(Monto(r, Prestaciones.Codigos.Cesantia) > 0);
    }

    [Theory]
    [InlineData(CausaTerminacion.DespidoJustificado)]
    [InlineData(CausaTerminacion.Renuncia)]
    public void SinResponsabilidadDelEmpleador_SoloDerechosAdquiridos(CausaTerminacion causa)
    {
        var r = Prestaciones.Calcular(Entrada(causa));

        Assert.Equal(new[] { Prestaciones.Codigos.Vacaciones, Prestaciones.Codigos.Navidad }, r.Lineas.Select(l => l.Codigo));
    }

    [Fact]
    public void CierreDeLaEmpresa_PagaAsistenciaEconomica()
    {
        var r = Prestaciones.Calcular(Entrada(CausaTerminacion.MuerteIncapacidadCierre));

        Assert.Equal(R(45000m / 23.83m * 100), Monto(r, Prestaciones.Codigos.Asistencia)); // 6 × 15 + 10 por 9 meses
        Assert.Equal(0, Monto(r, Prestaciones.Codigos.Cesantia));
    }

    [Fact]
    public void SalarioNavidad_HastaLaSalida()
    {
        var calculo = PagosEspeciales.SalarioNavidad(2026, new DateTime(2020, 1, 1), 45000, false, new Dictionary<int, decimal>(), new DateTime(2026, 10, 23));

        Assert.Equal(45000m * 9 + R(45000m * 23 / 31), calculo.TotalDevengado);
    }
}

public class LiquidacionesTests
{
    private static decimal R(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // 1 = 45,000 mensual desde 2020, con un préstamo de 5,000 de saldo
    private static (EmpleadosDbContext Context, LiquidacionService Liquidaciones, CicloNominaService Ciclo, IncidenciaService Incidencias) Crear()
    {
        var options = new DbContextOptionsBuilder<EmpleadosDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using (var semilla = new EmpleadosDbContext(options))
        {
            semilla.Database.EnsureCreated();
            semilla.Empresas.Add(new Empresa { Id = 1, Rnc = "101000001", RazonSocial = "Uno SRL", TasaRiesgoSrl = 1.20m });
            semilla.Departamentos.Add(new Departamento { Id = 1, EmpresaId = 1, Nombre = "TI" });
            semilla.Empleados.Add(new Empleado
            {
                Id = 1, EmpresaId = 1, DepartamentoId = 1, Nombre = "Ana", Apellido = "Pérez", CorreoElectronico = "a@test.com",
                Telefono = "1", Direccion = "d", Salario = 45000, FechaNacimiento = new DateTime(1990, 1, 1),
                FechaContratacion = new DateTime(2020, 1, 1), FrecuenciaPago = FrecuenciaPago.Mensual, NumeroDocumento = "00100000001", Nss = "N1"
            });
            semilla.Prestamos.Add(new Prestamo { Id = 1, EmpresaId = 1, EmpleadoId = 1, Concepto = "Préstamo", MontoTotal = 8000, MontoCuota = 1000, Saldo = 5000, Activo = true, FechaInicio = new DateTime(2026, 1, 1) });
            semilla.SaveChanges();
        }
        var contexto = new ContextoFijo();
        var context = new EmpleadosDbContext(options, contexto);
        var parametros = new ParametrosLegalesService(context);
        var incidencias = new IncidenciaService(context, parametros, contexto);
        return (context, new LiquidacionService(context, parametros, incidencias, contexto),
            new CicloNominaService(context, new NominaService(context, parametros), contexto), incidencias);
    }

    private static LiquidacionDto Desahucio(string salida = "2026-10-23") => new()
    {
        EmpleadoId = 1, Causa = CausaTerminacion.Desahucio, FechaSalida = DateTime.Parse(salida)
    };

    private static decimal Monto(Liquidacion l, string codigo) => LiquidacionService.Lineas(l.LineasJson).Where(x => x.Codigo == codigo).Sum(x => x.Monto);

    [Fact]
    public async Task Desahucio_UsaElSaldoDeVacacionesYDescuentaElPrestamo()
    {
        var (_, liquidaciones, _, _) = Crear();

        var resultado = await liquidaciones.CrearAsync(Desahucio());

        Assert.True(resultado.Ok, resultado.Mensaje);
        var l = resultado.Valor!;
        var diario = 45000m / 23.83m;
        Assert.Equal(45000, l.SalarioPromedio);
        Assert.Equal(R(diario * (18 + 10)), Monto(l, Prestaciones.Codigos.Vacaciones)); // 18 del 6.º año + 10 por 9 meses
        Assert.Equal(R((45000m * 9 + R(45000m * 23 / 31)) / 12), Monto(l, Prestaciones.Codigos.Navidad));
        Assert.Equal(5000, l.TotalDeducciones);
        Assert.Equal(l.TotalIngresos - 5000, l.Neto);
        Assert.Equal(EstadoLiquidacion.Borrador, l.Estado);
    }

    [Fact]
    public async Task SalarioPromedio_UsaLasNominasDelUltimoAnio()
    {
        var (_, liquidaciones, ciclo, incidencias) = Crear();
        var falta = await incidencias.CrearAsync(new IncidenciaDto { EmpleadoId = 1, Tipo = TipoIncidencia.Falta, FechaInicio = new DateTime(2026, 10, 5), FechaFin = new DateTime(2026, 10, 6) });
        await incidencias.AprobarAsync(falta.Valor!.Id);
        Assert.True((await ciclo.CrearAsync(new CrearNominaDto { Anio = 2026, Mes = 10, Frecuencia = FrecuenciaPago.Mensual })).Ok);

        var l = (await liquidaciones.CrearAsync(Desahucio("2026-11-20"))).Valor!;

        Assert.Equal(R((45000m * 11 + 45000 - R(45000m / 23.83m * 2)) / 12), l.SalarioPromedio);
    }

    [Fact]
    public async Task Pagar_SacaAlEmpleadoYSaldaElPrestamo()
    {
        var (context, liquidaciones, _, _) = Crear();
        var l = (await liquidaciones.CrearAsync(Desahucio())).Valor!;

        var pagar = await liquidaciones.PagarAsync(l.Id, new DateTime(2026, 10, 30));

        Assert.True(pagar.Ok, pagar.Mensaje);
        var empleado = await context.Empleados.FirstAsync(e => e.Id == 1);
        Assert.Equal(EstadoEmpleado.Despedido, empleado.Estado);
        Assert.Equal(new DateTime(2026, 10, 23), empleado.FechaTerminacion);
        var prestamo = await context.Prestamos.FirstAsync();
        Assert.Equal(0, prestamo.Saldo);
        Assert.False(prestamo.Activo);
        Assert.Contains(await context.MovimientosEmpleados.ToListAsync(), m => m.ValorNuevo == "Terminación: Desahucio por el empleador");
        Assert.False((await liquidaciones.EliminarAsync(l.Id)).Ok);
        Assert.False((await liquidaciones.CrearAsync(Desahucio())).Ok);
    }

    [Fact]
    public async Task Pdf_SeGeneraConElReciboDeDescargo()
    {
        var (context, liquidaciones, _, _) = Crear();
        var l = (await liquidaciones.CrearAsync(Desahucio())).Valor!;

        var pdf = LiquidacionPdf.Generar(await context.Empresas.FirstAsync(), (await liquidaciones.ObtenerAsync(l.Id))!);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}
