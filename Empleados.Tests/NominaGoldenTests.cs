using Empleados.Nomina;

namespace Empleados.Tests;

// Casos "golden": los montos esperados se calcularon aparte (a mano y con un script independiente del motor)
// con los parámetros 2026. Los mismos casos están explicados en docs/CASOS_NOMINA.md para validarlos.
public class NominaGoldenTests
{
    internal static readonly ParametrosNomina Parametros2026 = new()
    {
        AfpEmpleado = 2.87m, AfpEmpleador = 7.10m,
        SfsEmpleado = 3.04m, SfsEmpleador = 7.09m,
        SalarioMinimoCotizable = 23223m, TopeAfpSalarios = 20, TopeSfsSalarios = 10, TopeSrlSalarios = 4,
        InfotepEmpleador = 1.00m, DivisorSalarioDiario = 23.83m, HorasJornadaDiaria = 8,
        RecargoHoraExtra = 35, RecargoHoraExtraExceso = 100, RecargoNocturno = 15, RecargoFeriado = 100,
        TasaSrl = 1.20m,
        EscalaIsr = new[]
        {
            new TramoEscala(0m, 416220m, 0m, 0m),
            new TramoEscala(416220m, 624329m, 15m, 0m),
            new TramoEscala(624329m, 867123m, 20m, 31216m),
            new TramoEscala(867123m, null, 25m, 79776m)
        }
    };

    private static readonly PeriodoNomina Octubre = PeriodoNomina.Mensual(2026, 10);
    private static readonly PeriodoNomina OctubreQ1 = PeriodoNomina.Quincenal(2026, 10, 1);
    private static readonly PeriodoNomina OctubreQ2 = PeriodoNomina.Quincenal(2026, 10, 2);

    private static ReciboNomina Calcular(EntradaNomina entrada) => MotorNomina.Calcular(entrada, Parametros2026);

    // Calcula la 1.ª quincena y luego la 2.ª con lo acumulado
    private static (ReciboNomina Q1, ReciboNomina Q2) Mes(EntradaNomina q1, EntradaNomina q2)
    {
        var r1 = Calcular(q1);
        return (r1, Calcular(q2 with { Acumulado = r1.AcumuladoAlCierre }));
    }

    private static void Ley(ReciboNomina r, decimal afp, decimal sfs, decimal isr, decimal neto)
    {
        Assert.Equal(afp, r.Monto(Codigos.Afp));
        Assert.Equal(sfs, r.Monto(Codigos.Sfs));
        Assert.Equal(isr, r.Monto(Codigos.Isr));
        Assert.Equal(neto, r.Neto);
    }

    private static void Patronal(ReciboNomina r, decimal afp, decimal sfs, decimal srl, decimal infotep)
    {
        Assert.Equal(afp, r.Monto(Codigos.AfpPatronal));
        Assert.Equal(sfs, r.Monto(Codigos.SfsPatronal));
        Assert.Equal(srl, r.Monto(Codigos.Srl));
        Assert.Equal(infotep, r.Monto(Codigos.Infotep));
    }

    [Fact]
    public void A_Mensual35000_ExentoDeIsr()
    {
        var r = Calcular(new EntradaNomina { Periodo = Octubre, Salario = 35000 });

        Ley(r, afp: 1004.50m, sfs: 1064.00m, isr: 0m, neto: 32931.50m);
        Patronal(r, 2485.00m, 2481.50m, 420.00m, 350.00m);
        Assert.Equal(40736.50m, r.CostoEmpleador);
    }

    [Fact]
    public void B_Mensual60000_Tramo20()
    {
        var r = Calcular(new EntradaNomina { Periodo = Octubre, Salario = 60000 });

        Ley(r, afp: 1722.00m, sfs: 1824.00m, isr: 3486.65m, neto: 52967.35m);
        Assert.Equal(56454.00m, r.BaseIsrMensual);
        Assert.Equal(69834.00m, r.CostoEmpleador);
    }

    [Fact]
    public void C_Quincenal60000_LaSumaDelMesIgualaAlMensual()
    {
        var (q1, q2) = Mes(
            new EntradaNomina { Periodo = OctubreQ1, Salario = 60000 },
            new EntradaNomina { Periodo = OctubreQ2, Salario = 60000 });

        Ley(q1, afp: 861.00m, sfs: 912.00m, isr: 1743.33m, neto: 26483.67m);
        Ley(q2, afp: 861.00m, sfs: 912.00m, isr: 1743.32m, neto: 26483.68m);
        Assert.Equal(52967.35m, q1.Neto + q2.Neto); // igual al caso B
    }

    [Fact]
    public void D_Mensual300000_TopeSfsYSrl_Tramo25()
    {
        var r = Calcular(new EntradaNomina { Periodo = Octubre, Salario = 300000 });

        Ley(r, afp: 8610.00m, sfs: 7059.79m, isr: 59665.49m, neto: 224664.72m);
        Patronal(r, 21300.00m, 16465.11m, 1114.70m, 3000.00m);
        Assert.Equal(341879.81m, r.CostoEmpleador);
    }

    [Fact]
    public void E_Quincenal400000_ElTopeDelSfsSeAjustaEnLaSegundaQuincena()
    {
        var (q1, q2) = Mes(
            new EntradaNomina { Periodo = OctubreQ1, Salario = 400000 },
            new EntradaNomina { Periodo = OctubreQ2, Salario = 400000 });

        Ley(q1, afp: 5740.00m, sfs: 6080.00m, isr: 41336.47m, neto: 146843.53m);
        Ley(q2, afp: 5740.00m, sfs: 979.79m, isr: 42611.52m, neto: 150668.69m);
        Patronal(q1, 14200.00m, 14180.00m, 1114.70m, 2000.00m);
        Patronal(q2, 14200.00m, 2285.11m, 0.00m, 2000.00m);
    }

    [Fact]
    public void F_IngresoAMitadDeQuincena_SalarioProporcional()
    {
        var r = Calcular(new EntradaNomina { Periodo = OctubreQ2, Salario = 30000, FechaIngreso = new DateTime(2026, 10, 24) });

        Assert.Equal(7500.00m, r.Monto(Codigos.Salario)); // 15,000 × 8 / 16 días
        Ley(r, afp: 215.25m, sfs: 228.00m, isr: 0m, neto: 7056.75m);
    }

    [Fact]
    public void G_HorasExtraNocturnoYFeriado_SobreSalarioMinimo()
    {
        var r = Calcular(new EntradaNomina
        {
            Periodo = Octubre, Salario = 29988,
            HorasExtra = 10, HorasExtraExceso = 2, HorasNocturnas = 8, HorasFeriado = 8
        });

        Assert.Equal(2123.57m, r.Monto(Codigos.HoraExtra));
        Assert.Equal(629.21m, r.Monto(Codigos.HoraExtraExceso));
        Assert.Equal(188.76m, r.Monto(Codigos.Nocturno));
        Assert.Equal(1258.41m, r.Monto(Codigos.Feriado));
        Assert.Equal(34187.95m, r.TotalIngresos);
        Ley(r, afp: 981.19m, sfs: 1039.31m, isr: 0m, neto: 32167.45m);
    }

    [Fact]
    public void H_AusenciasSinPago_SeDescuentanConSalarioDiario()
    {
        var r = Calcular(new EntradaNomina { Periodo = Octubre, Salario = 35000, DiasAusenciaSinPago = 2 });

        Assert.Equal(-2937.47m, r.Monto(Codigos.Ausencia)); // 35,000 / 23.83 × 2
        Ley(r, afp: 920.19m, sfs: 974.70m, isr: 0m, neto: 30167.64m);
    }

    [Fact]
    public void I_SalarioPorHora()
    {
        var r = Calcular(new EntradaNomina { Periodo = OctubreQ1, Modalidad = ModalidadSalario.PorHora, Salario = 180, HorasTrabajadas = 88 });

        Assert.Equal(15840.00m, r.Monto(Codigos.Salario));
        Ley(r, afp: 454.61m, sfs: 481.54m, isr: 0m, neto: 14903.85m);
    }

    [Fact]
    public void J_DeduccionesInternas_EnOrdenDePrioridad_SinNetoNegativo()
    {
        var r = Calcular(new EntradaNomina
        {
            Periodo = Octubre, Salario = 35000,
            Deducciones = new[]
            {
                new DeduccionInterna("Cooperativa", 5000, Prioridad: 2),
                new DeduccionInterna("Préstamo", 30000, Prioridad: 1)
            }
        });

        var internas = r.Lineas.Where(l => l.Tipo == TipoLinea.DeduccionInterna).ToList();
        Assert.Equal(("Préstamo", 30000.00m), (internas[0].Concepto, internas[0].Monto));
        Assert.Equal(("Cooperativa", 2931.50m), (internas[1].Concepto, internas[1].Monto));
        Assert.Equal(0m, r.Neto);
        Assert.Contains(r.Advertencias, a => a.Contains("Cooperativa") && a.Contains("2,068.50"));
    }

    [Fact]
    public void K_BonoEnLaPrimeraQuincena()
    {
        var (q1, q2) = Mes(
            new EntradaNomina { Periodo = OctubreQ1, Salario = 80000, OtrosIngresos = new[] { new IngresoAdicional("Bono", 50000) } },
            new EntradaNomina { Periodo = OctubreQ2, Salario = 80000 });

        Ley(q1, afp: 2583.00m, sfs: 2736.00m, isr: 15461.72m, neto: 69219.28m);
        Ley(q2, afp: 1148.00m, sfs: 1216.00m, isr: 3700.47m, neto: 33935.53m);
    }

    [Fact]
    public void L_DevolucionDeIsrEnLaSegundaQuincena()
    {
        var (q1, q2) = Mes(
            new EntradaNomina { Periodo = OctubreQ1, Salario = 40000, OtrosIngresos = new[] { new IngresoAdicional("Bono", 50000) } },
            new EntradaNomina { Periodo = OctubreQ2, Salario = 40000 });

        Ley(q1, afp: 2009.00m, sfs: 2128.00m, isr: 10757.22m, neto: 55105.78m);
        Ley(q2, afp: 574.00m, sfs: 608.00m, isr: -1004.03m, neto: 19822.03m);
        Assert.Contains(q2.Lineas, l => l.Codigo == Codigos.Isr && l.Concepto.Contains("devolución"));
    }
}
