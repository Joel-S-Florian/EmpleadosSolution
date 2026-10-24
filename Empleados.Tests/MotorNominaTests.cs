using Empleados.Nomina;

namespace Empleados.Tests;

// Reglas puntuales del motor (los montos de referencia están en NominaGoldenTests)
public class MotorNominaTests
{
    private static readonly ParametrosNomina P = NominaGoldenTests.Parametros2026;
    private static readonly PeriodoNomina Octubre = PeriodoNomina.Mensual(2026, 10);

    [Theory]
    [InlineData(1, 1, 15)]
    [InlineData(2, 16, 31)]
    public void Quincenas_Del1Al15_YDel16AlFinDeMes(int quincena, int inicio, int fin)
    {
        var periodo = PeriodoNomina.Quincenal(2026, 10, quincena);

        Assert.Equal((inicio, fin), (periodo.Inicio.Day, periodo.Fin.Day));
        Assert.Equal(quincena == 2, periodo.CierraElMes);
    }

    [Fact]
    public void SegundaQuincenaDeFebrero_TieneLosDiasDelMes()
    {
        Assert.Equal(13, PeriodoNomina.Quincenal(2026, 2, 2).Dias); // 16 al 28
        Assert.Equal(14, PeriodoNomina.Quincenal(2028, 2, 2).Dias); // año bisiesto
    }

    [Theory]
    [InlineData(416220, 0)]          // límite del tramo exento
    [InlineData(416221, 0.15)]       // primer peso gravado
    [InlineData(624329, 31216.35)]   // fin del tramo del 15 %
    [InlineData(867123, 79774.80)]   // fin del tramo del 20 %; la DGII publica 79,776 como cuota fija del siguiente (salto de 1.20 al año)
    public void EscalaIsr_EnLosLimitesDeCadaTramo(decimal renta, decimal esperado)
    {
        Assert.Equal(esperado, Math.Round(MotorNomina.IsrAnual(renta, P.EscalaIsr), 2));
    }

    [Fact]
    public void SalidaAntesDelPeriodo_NoPagaSalario_YAdvierte()
    {
        var r = MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = 35000, FechaSalida = new DateTime(2026, 9, 30) }, P);

        Assert.Equal(0m, r.Neto);
        Assert.NotEmpty(r.Advertencias);
    }

    [Fact]
    public void DependientesSfs_SinCostoConfigurado_Advierte_YConCostoSeDescuentaAlCerrarElMes()
    {
        var sinCosto = MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = 35000, DependientesSfs = 2 }, P);
        var conCosto = MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = 35000, DependientesSfs = 2 }, P with { CostoDependienteSfs = 1000m });
        var primeraQuincena = MotorNomina.Calcular(new EntradaNomina { Periodo = PeriodoNomina.Quincenal(2026, 10, 1), Salario = 35000, DependientesSfs = 2 }, P with { CostoDependienteSfs = 1000m });

        Assert.Contains(sinCosto.Advertencias, a => a.Contains("dependiente"));
        Assert.Equal(2000m, conCosto.Monto(Codigos.SfsDependientes));
        Assert.Equal(0m, primeraQuincena.Monto(Codigos.SfsDependientes));
    }

    [Fact]
    public void OtroIngresoQueNoCotizaNiGrava_SoloSumaAlNeto()
    {
        var conViatico = MotorNomina.Calcular(new EntradaNomina
        {
            Periodo = Octubre, Salario = 60000,
            OtrosIngresos = new[] { new IngresoAdicional("Viático", 5000, CotizaTss: false, GravaIsr: false) }
        }, P);
        var sinViatico = MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = 60000 }, P);

        Assert.Equal(sinViatico.Monto(Codigos.Afp), conViatico.Monto(Codigos.Afp));
        Assert.Equal(sinViatico.Monto(Codigos.Isr), conViatico.Monto(Codigos.Isr));
        Assert.Equal(sinViatico.Neto + 5000, conViatico.Neto);
    }

    [Fact]
    public void Explicacion_PermiteSeguirElCalculo()
    {
        var r = MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = 60000 }, P);

        Assert.Contains(r.Explicacion, e => e.Contains("677,448.00") && e.Contains("tramo 20.00 %"));
        Assert.Contains(r.Explicacion, e => e.StartsWith("Neto:") && e.Contains("52,967.35"));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(35000, -1)]
    public void EntradasInvalidas_SeRechazan(decimal salario, decimal horasExtra)
    {
        Assert.Throws<ArgumentException>(() =>
            MotorNomina.Calcular(new EntradaNomina { Periodo = Octubre, Salario = salario, HorasExtra = horasExtra }, P));
    }

    [Fact]
    public void AusenciasMayoresQueElSalario_SeRechazan()
    {
        Assert.Throws<ArgumentException>(() =>
            MotorNomina.Calcular(new EntradaNomina { Periodo = PeriodoNomina.Quincenal(2026, 10, 1), Salario = 30000, DiasAusenciaSinPago = 20 }, P));
    }
}
