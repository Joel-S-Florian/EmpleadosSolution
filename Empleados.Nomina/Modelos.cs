namespace Empleados.Nomina;

public enum Frecuencia
{
    Quincenal,
    Mensual
}

public enum ModalidadSalario
{
    FijoMensual,
    PorHora
}

// Tramo de la escala anual de ISR: ISR = CuotaFija + (renta - Desde) * Tasa%, para Desde < renta <= Hasta
public record TramoEscala(decimal Desde, decimal? Hasta, decimal Tasa, decimal CuotaFija);

// Valores legales vigentes en el período (porcentajes como 2.87 = 2.87 %)
public record ParametrosNomina
{
    public decimal AfpEmpleado { get; init; }
    public decimal AfpEmpleador { get; init; }
    public decimal SfsEmpleado { get; init; }
    public decimal SfsEmpleador { get; init; }
    public decimal SalarioMinimoCotizable { get; init; }
    public decimal TopeAfpSalarios { get; init; }
    public decimal TopeSfsSalarios { get; init; }
    public decimal TopeSrlSalarios { get; init; }
    public decimal InfotepEmpleador { get; init; }
    public decimal DivisorSalarioDiario { get; init; }
    public decimal HorasJornadaDiaria { get; init; }
    public decimal RecargoHoraExtra { get; init; }
    public decimal RecargoHoraExtraExceso { get; init; }
    public decimal RecargoNocturno { get; init; }
    public decimal RecargoFeriado { get; init; }
    // Costo mensual por cada dependiente adicional del SFS; null = no configurado
    public decimal? CostoDependienteSfs { get; init; }
    // Tasa SRL asignada a la empresa por la TSS
    public decimal TasaSrl { get; init; }
    public IReadOnlyList<TramoEscala> EscalaIsr { get; init; } = Array.Empty<TramoEscala>();

    public decimal TopeAfp => SalarioMinimoCotizable * TopeAfpSalarios;
    public decimal TopeSfs => SalarioMinimoCotizable * TopeSfsSalarios;
    public decimal TopeSrl => SalarioMinimoCotizable * TopeSrlSalarios;
}

public record PeriodoNomina(DateTime Inicio, DateTime Fin, Frecuencia Frecuencia, int Quincena)
{
    public static PeriodoNomina Mensual(int anio, int mes) =>
        new(new DateTime(anio, mes, 1), new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)), Frecuencia.Mensual, 0);

    // 1.ª quincena: del 1 al 15; 2.ª quincena: del 16 al último día del mes
    public static PeriodoNomina Quincenal(int anio, int mes, int quincena)
    {
        if (quincena is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(quincena), "La quincena debe ser 1 o 2");
        return quincena == 1
            ? new(new DateTime(anio, mes, 1), new DateTime(anio, mes, 15), Frecuencia.Quincenal, 1)
            : new(new DateTime(anio, mes, 16), new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)), Frecuencia.Quincenal, 2);
    }

    public int Dias => (Fin - Inicio).Days + 1;
    // El último período del mes ajusta los topes mensuales de la TSS y el ISR del mes
    public bool CierraElMes => Frecuencia == Frecuencia.Mensual || Quincena == 2;

    public string Descripcion => Frecuencia == Frecuencia.Mensual
        ? $"Mensual {Inicio:MM/yyyy}"
        : $"{Quincena}.ª quincena {Inicio:MM/yyyy} ({Inicio:dd} al {Fin:dd})";
}

public record IngresoAdicional(string Concepto, decimal Monto, bool CotizaTss = true, bool GravaIsr = true);

public record DeduccionInterna(string Concepto, decimal Monto, int Prioridad = 0);

// Lo ya calculado en el mismo mes antes de este período (para la 2.ª quincena)
public record AcumuladoMes
{
    public decimal BaseTss { get; init; }
    public decimal AfpEmpleado { get; init; }
    public decimal SfsEmpleado { get; init; }
    // Ingresos gravables brutos (antes de restar AFP y SFS)
    public decimal IngresosGravables { get; init; }
    public decimal Isr { get; init; }
    public decimal AfpEmpleador { get; init; }
    public decimal SfsEmpleador { get; init; }
    public decimal SrlEmpleador { get; init; }

    public static readonly AcumuladoMes Vacio = new();
}

public record EntradaNomina
{
    public required PeriodoNomina Periodo { get; init; }
    public ModalidadSalario Modalidad { get; init; } = ModalidadSalario.FijoMensual;
    // Salario mensual (fijo) o tarifa por hora
    public decimal Salario { get; init; }
    public DateTime FechaIngreso { get; init; } = DateTime.MinValue;
    public DateTime? FechaSalida { get; init; }
    public int DependientesSfs { get; init; }
    // Solo para salario por hora
    public decimal HorasTrabajadas { get; init; }
    public decimal DiasAusenciaSinPago { get; init; }
    // Horas extra hasta el umbral semanal (68 h) y por encima de él
    public decimal HorasExtra { get; init; }
    public decimal HorasExtraExceso { get; init; }
    public decimal HorasNocturnas { get; init; }
    public decimal HorasFeriado { get; init; }
    public IReadOnlyList<IngresoAdicional> OtrosIngresos { get; init; } = Array.Empty<IngresoAdicional>();
    public IReadOnlyList<DeduccionInterna> Deducciones { get; init; } = Array.Empty<DeduccionInterna>();
    public AcumuladoMes Acumulado { get; init; } = AcumuladoMes.Vacio;
}

public enum TipoLinea
{
    Ingreso,
    DeduccionLey,
    DeduccionInterna,
    AportePatronal
}

public record LineaRecibo(
    TipoLinea Tipo,
    string Codigo,
    string Concepto,
    decimal Monto,
    decimal? Cantidad = null,
    bool CotizaTss = false,
    bool GravaIsr = false);

public record ReciboNomina
{
    public required PeriodoNomina Periodo { get; init; }
    public required IReadOnlyList<LineaRecibo> Lineas { get; init; }
    public decimal TotalIngresos { get; init; }
    public decimal TotalDeduccionesLey { get; init; }
    public decimal TotalDeduccionesInternas { get; init; }
    public decimal Neto { get; init; }
    public decimal TotalAportesPatronales { get; init; }
    public decimal CostoEmpleador { get; init; }
    public decimal BaseTssPeriodo { get; init; }
    // Base del ISR del mes: ingresos gravables menos AFP y SFS del empleado (proyectada en la 1.ª quincena)
    public decimal BaseIsrMensual { get; init; }
    public decimal RentaAnualProyectada { get; init; }
    public required AcumuladoMes AcumuladoAlCierre { get; init; }
    public required IReadOnlyList<string> Advertencias { get; init; }
    // Pasos del cálculo en lenguaje natural, para revisarlos a mano
    public required IReadOnlyList<string> Explicacion { get; init; }

    public decimal Monto(string codigo) => Lineas.Where(l => l.Codigo == codigo).Sum(l => l.Monto);
}
