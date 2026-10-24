using System.Globalization;

namespace Empleados.Nomina;

public static class Codigos
{
    public const string Salario = "SALARIO";
    public const string Ausencia = "AUSENCIA";
    public const string HoraExtra = "HEXTRA";
    public const string HoraExtraExceso = "HEXTRA_EXCESO";
    public const string Nocturno = "NOCTURNO";
    public const string Feriado = "FERIADO";
    public const string OtroIngreso = "OTRO";
    public const string Afp = "AFP";
    public const string Sfs = "SFS";
    public const string SfsDependientes = "SFS_DEP";
    public const string Isr = "ISR";
    public const string Deduccion = "DED";
    public const string AfpPatronal = "AFP_PAT";
    public const string SfsPatronal = "SFS_PAT";
    public const string Srl = "SRL";
    public const string Infotep = "INFOTEP";
}

// Cálculo de un recibo de nómina (República Dominicana). Función pura: sin base de datos ni reloj.
// Reglas (a validar con quien hace la nómina):
// - Salario fijo: mensual = salario; quincena = salario / 2. Ingreso o salida a mitad de período: proporcional a días calendario.
// - Salario diario = mensual / divisor (23.83); valor hora = diario / horas de la jornada (8). Ausencias sin pago: salario diario × días.
// - Horas extra: valor hora × (1 + recargo). Nocturno y feriado: solo el recargo, porque esas horas ya están en el salario.
// - TSS con topes mensuales: el período que cierra el mes calcula el aporte del mes y descuenta lo ya retenido.
// - ISR: 1.ª quincena proyecta el mes (base de la quincena × 2) y retiene la mitad; la 2.ª recalcula el mes completo
//   y retiene la diferencia (puede ser una devolución). Mensual: ISR del mes.
public static class MotorNomina
{
    private static readonly CultureInfo Formato = CultureInfo.InvariantCulture;

    public static ReciboNomina Calcular(EntradaNomina entrada, ParametrosNomina p)
    {
        Validar(entrada, p);

        var periodo = entrada.Periodo;
        var acum = entrada.Acumulado;
        var lineas = new List<LineaRecibo>();
        var advertencias = new List<string>();
        var explicacion = new List<string>();

        // 1. Salario del período
        decimal valorHora;
        if (entrada.Modalidad == ModalidadSalario.FijoMensual)
        {
            var salarioPeriodo = periodo.Frecuencia == Frecuencia.Mensual ? entrada.Salario : entrada.Salario / 2;
            var desde = Max(periodo.Inicio, entrada.FechaIngreso.Date);
            var hasta = Min(periodo.Fin, entrada.FechaSalida?.Date ?? periodo.Fin);
            var dias = hasta >= desde ? (hasta - desde).Days + 1 : 0;

            var salario = salarioPeriodo * dias / periodo.Dias;
            explicacion.Add(periodo.Frecuencia == Frecuencia.Mensual
                ? $"Salario mensual: {N(entrada.Salario)}"
                : $"Salario de la quincena: {N(entrada.Salario)} / 2 = {N(salarioPeriodo)}");
            if (dias < periodo.Dias)
            {
                explicacion.Add($"Proporcional a {dias} de {periodo.Dias} días del período: {N(salarioPeriodo)} × {dias} / {periodo.Dias} = {N(R(salario))}");
                if (dias == 0)
                    advertencias.Add("El empleado no trabajó ningún día del período (fecha de ingreso o salida)");
            }
            lineas.Add(Ingreso(Codigos.Salario, "Salario", R(salario), dias));

            var salarioDiario = entrada.Salario / p.DivisorSalarioDiario;
            valorHora = salarioDiario / p.HorasJornadaDiaria;
            if (entrada.DiasAusenciaSinPago > 0)
            {
                var ausencia = R(salarioDiario * entrada.DiasAusenciaSinPago);
                lineas.Add(Ingreso(Codigos.Ausencia, "Ausencias sin pago", -ausencia, entrada.DiasAusenciaSinPago));
                explicacion.Add($"Ausencias: salario diario {N(entrada.Salario)} / {N(p.DivisorSalarioDiario)} = {N(R(salarioDiario))} × {N(entrada.DiasAusenciaSinPago)} días = -{N(ausencia)}");
            }
        }
        else
        {
            valorHora = entrada.Salario;
            var salario = R(entrada.Salario * entrada.HorasTrabajadas);
            lineas.Add(Ingreso(Codigos.Salario, "Salario por horas", salario, entrada.HorasTrabajadas));
            explicacion.Add($"Salario por horas: {N(entrada.Salario)} × {N(entrada.HorasTrabajadas)} h = {N(salario)}");
            if (entrada.DiasAusenciaSinPago > 0)
                advertencias.Add("Las ausencias no se descuentan en salario por hora: se pagan solo las horas trabajadas");
        }

        // 2. Horas extra y recargos
        if (entrada.HorasExtra + entrada.HorasExtraExceso + entrada.HorasNocturnas + entrada.HorasFeriado > 0)
            explicacion.Add($"Valor hora: {(entrada.Modalidad == ModalidadSalario.FijoMensual ? $"{N(entrada.Salario)} / {N(p.DivisorSalarioDiario)} / {N(p.HorasJornadaDiaria)} = " : "")}{N4(valorHora)}");
        AgregarHoras(lineas, explicacion, Codigos.HoraExtra, "Horas extra", entrada.HorasExtra, valorHora, 1 + p.RecargoHoraExtra / 100, p.RecargoHoraExtra);
        AgregarHoras(lineas, explicacion, Codigos.HoraExtraExceso, "Horas extra sobre el límite semanal", entrada.HorasExtraExceso, valorHora, 1 + p.RecargoHoraExtraExceso / 100, p.RecargoHoraExtraExceso);
        AgregarHoras(lineas, explicacion, Codigos.Nocturno, "Recargo nocturno", entrada.HorasNocturnas, valorHora, p.RecargoNocturno / 100, p.RecargoNocturno);
        AgregarHoras(lineas, explicacion, Codigos.Feriado, "Recargo por feriado o descanso trabajado", entrada.HorasFeriado, valorHora, p.RecargoFeriado / 100, p.RecargoFeriado);

        // 3. Otros ingresos
        foreach (var otro in entrada.OtrosIngresos)
            lineas.Add(new LineaRecibo(TipoLinea.Ingreso, Codigos.OtroIngreso, otro.Concepto, R(otro.Monto), null, otro.CotizaTss, otro.GravaIsr));

        var totalIngresos = lineas.Sum(l => l.Monto);
        if (totalIngresos < 0)
            throw new ArgumentException("Los descuentos por ausencia superan el salario del período");
        var baseTssPeriodo = lineas.Where(l => l.CotizaTss).Sum(l => l.Monto);
        var gravablesPeriodo = lineas.Where(l => l.GravaIsr).Sum(l => l.Monto);

        // 4. TSS del empleado, con topes mensuales
        var baseTssMes = acum.BaseTss + baseTssPeriodo;
        var afpMes = R(Math.Min(baseTssMes, p.TopeAfp) * p.AfpEmpleado / 100);
        var sfsMes = R(Math.Min(baseTssMes, p.TopeSfs) * p.SfsEmpleado / 100);
        var afp = afpMes - acum.AfpEmpleado;
        var sfs = sfsMes - acum.SfsEmpleado;
        lineas.Add(new LineaRecibo(TipoLinea.DeduccionLey, Codigos.Afp, $"AFP ({N(p.AfpEmpleado)} %)", afp));
        lineas.Add(new LineaRecibo(TipoLinea.DeduccionLey, Codigos.Sfs, $"SFS ({N(p.SfsEmpleado)} %)", sfs));
        explicacion.Add(ExplicarTss("AFP", baseTssMes, p.TopeAfp, p.AfpEmpleado, afpMes, acum.AfpEmpleado));
        explicacion.Add(ExplicarTss("SFS", baseTssMes, p.TopeSfs, p.SfsEmpleado, sfsMes, acum.SfsEmpleado));

        if (entrada.DependientesSfs > 0)
        {
            if (p.CostoDependienteSfs is not decimal costo)
                advertencias.Add($"El empleado tiene {entrada.DependientesSfs} dependiente(s) adicional(es) del SFS, pero su costo no está configurado en Parámetros legales; no se descontó");
            else if (periodo.CierraElMes)
            {
                var dependientes = R(costo * entrada.DependientesSfs);
                lineas.Add(new LineaRecibo(TipoLinea.DeduccionLey, Codigos.SfsDependientes, "SFS dependientes adicionales", dependientes, entrada.DependientesSfs));
                explicacion.Add($"SFS dependientes adicionales: {entrada.DependientesSfs} × {N(costo)} = {N(dependientes)} (se descuenta en el último pago del mes)");
            }
        }

        // 5. ISR
        decimal isr, baseIsrMensual;
        if (periodo.Frecuencia == Frecuencia.Quincenal && periodo.Quincena == 1)
        {
            baseIsrMensual = Math.Max(0, (gravablesPeriodo - afp - sfs) * 2);
            var isrMesProyectado = IsrAnual(baseIsrMensual * 12, p.EscalaIsr) / 12;
            isr = R(isrMesProyectado / 2);
            explicacion.Add($"ISR (proyección del mes): ({N(gravablesPeriodo)} − AFP {N(afp)} − SFS {N(sfs)}) × 2 = {N(baseIsrMensual)} mensual");
            explicacion.Add(ExplicarIsr(baseIsrMensual, p.EscalaIsr) + $" → la mitad en esta quincena: {N(isr)}");
        }
        else
        {
            var gravablesMes = acum.IngresosGravables + gravablesPeriodo;
            baseIsrMensual = Math.Max(0, gravablesMes - afpMes - sfsMes);
            var isrMes = R(IsrAnual(baseIsrMensual * 12, p.EscalaIsr) / 12);
            isr = isrMes - acum.Isr;
            explicacion.Add($"ISR del mes: ingresos gravables {N(gravablesMes)} − AFP {N(afpMes)} − SFS {N(sfsMes)} = {N(baseIsrMensual)} mensual");
            explicacion.Add(ExplicarIsr(baseIsrMensual, p.EscalaIsr) + (acum.Isr != 0
                ? $" → ISR del mes {N(isrMes)} − ya retenido {N(acum.Isr)} = {N(isr)}"
                : ""));
        }
        lineas.Add(new LineaRecibo(TipoLinea.DeduccionLey, Codigos.Isr, isr < 0 ? "ISR (devolución por ajuste del mes)" : "ISR", isr));

        // 6. Deducciones internas, en orden de prioridad, sin dejar el neto negativo
        var totalLey = lineas.Where(l => l.Tipo == TipoLinea.DeduccionLey).Sum(l => l.Monto);
        var disponible = totalIngresos - totalLey;
        foreach (var deduccion in entrada.Deducciones.OrderBy(d => d.Prioridad))
        {
            var monto = R(deduccion.Monto);
            var aplicado = Math.Min(monto, Math.Max(disponible, 0));
            if (aplicado > 0)
                lineas.Add(new LineaRecibo(TipoLinea.DeduccionInterna, Codigos.Deduccion, deduccion.Concepto, aplicado));
            if (aplicado < monto)
                advertencias.Add($"{deduccion.Concepto}: no se descontaron {N(monto - aplicado)} porque el neto no alcanza");
            disponible -= aplicado;
        }
        var totalInternas = lineas.Where(l => l.Tipo == TipoLinea.DeduccionInterna).Sum(l => l.Monto);

        // 7. Aportes del empleador (no se descuentan al empleado)
        var afpPatMes = R(Math.Min(baseTssMes, p.TopeAfp) * p.AfpEmpleador / 100);
        var sfsPatMes = R(Math.Min(baseTssMes, p.TopeSfs) * p.SfsEmpleador / 100);
        var srlMes = R(Math.Min(baseTssMes, p.TopeSrl) * p.TasaSrl / 100);
        var infotep = R(baseTssPeriodo * p.InfotepEmpleador / 100);
        lineas.Add(new LineaRecibo(TipoLinea.AportePatronal, Codigos.AfpPatronal, $"AFP empleador ({N(p.AfpEmpleador)} %)", afpPatMes - acum.AfpEmpleador));
        lineas.Add(new LineaRecibo(TipoLinea.AportePatronal, Codigos.SfsPatronal, $"SFS empleador ({N(p.SfsEmpleador)} %)", sfsPatMes - acum.SfsEmpleador));
        lineas.Add(new LineaRecibo(TipoLinea.AportePatronal, Codigos.Srl, $"Riesgos laborales ({N(p.TasaSrl)} %)", srlMes - acum.SrlEmpleador));
        lineas.Add(new LineaRecibo(TipoLinea.AportePatronal, Codigos.Infotep, $"INFOTEP ({N(p.InfotepEmpleador)} %)", infotep));
        var totalPatronal = lineas.Where(l => l.Tipo == TipoLinea.AportePatronal).Sum(l => l.Monto);

        var neto = totalIngresos - totalLey - totalInternas;
        explicacion.Add($"Neto: ingresos {N(totalIngresos)} − deducciones de ley {N(totalLey)} − deducciones internas {N(totalInternas)} = {N(neto)}");

        return new ReciboNomina
        {
            Periodo = periodo,
            Lineas = lineas,
            TotalIngresos = totalIngresos,
            TotalDeduccionesLey = totalLey,
            TotalDeduccionesInternas = totalInternas,
            Neto = neto,
            TotalAportesPatronales = totalPatronal,
            CostoEmpleador = totalIngresos + totalPatronal,
            BaseTssPeriodo = baseTssPeriodo,
            BaseIsrMensual = baseIsrMensual,
            RentaAnualProyectada = baseIsrMensual * 12,
            AcumuladoAlCierre = new AcumuladoMes
            {
                BaseTss = baseTssMes,
                AfpEmpleado = afpMes,
                SfsEmpleado = sfsMes,
                IngresosGravables = acum.IngresosGravables + gravablesPeriodo,
                Isr = acum.Isr + isr,
                AfpEmpleador = afpPatMes,
                SfsEmpleador = sfsPatMes,
                SrlEmpleador = srlMes
            },
            Advertencias = advertencias,
            Explicacion = explicacion
        };
    }

    // ISR anual sin redondear según la escala
    public static decimal IsrAnual(decimal rentaAnual, IReadOnlyList<TramoEscala> escala)
    {
        var tramo = Tramo(rentaAnual, escala);
        return tramo == null ? 0 : tramo.CuotaFija + (rentaAnual - tramo.Desde) * tramo.Tasa / 100;
    }

    private static TramoEscala? Tramo(decimal rentaAnual, IReadOnlyList<TramoEscala> escala) =>
        rentaAnual <= 0 ? null : escala.FirstOrDefault(t => rentaAnual > t.Desde && (t.Hasta == null || rentaAnual <= t.Hasta));

    private static void Validar(EntradaNomina e, ParametrosNomina p)
    {
        if (e.Salario <= 0)
            throw new ArgumentException("El salario debe ser mayor que cero");
        if (new[] { e.HorasTrabajadas, e.DiasAusenciaSinPago, e.HorasExtra, e.HorasExtraExceso, e.HorasNocturnas, e.HorasFeriado }.Any(v => v < 0))
            throw new ArgumentException("Las horas y los días no pueden ser negativos");
        if (e.OtrosIngresos.Any(i => i.Monto < 0) || e.Deducciones.Any(d => d.Monto < 0))
            throw new ArgumentException("Los montos de otros ingresos y deducciones no pueden ser negativos");
        if (p.EscalaIsr.Count == 0)
            throw new ArgumentException("No hay escala de ISR vigente para el período");
        if (p.DivisorSalarioDiario <= 0 || p.HorasJornadaDiaria <= 0)
            throw new ArgumentException("El divisor del salario diario y las horas de la jornada deben ser mayores que cero");
    }

    private static void AgregarHoras(List<LineaRecibo> lineas, List<string> explicacion, string codigo, string concepto,
        decimal horas, decimal valorHora, decimal factor, decimal recargo)
    {
        if (horas <= 0)
            return;
        var monto = R(valorHora * horas * factor);
        lineas.Add(Ingreso(codigo, $"{concepto} ({N(recargo)} %)", monto, horas));
        explicacion.Add($"{concepto}: {N4(valorHora)} × {N(horas)} h × {factor.ToString("0.####", Formato)} = {N(monto)}");
    }

    private static LineaRecibo Ingreso(string codigo, string concepto, decimal monto, decimal cantidad) =>
        new(TipoLinea.Ingreso, codigo, concepto, monto, cantidad, CotizaTss: true, GravaIsr: true);

    private static string ExplicarTss(string nombre, decimal baseMes, decimal tope, decimal tasa, decimal aporteMes, decimal retenido)
    {
        var texto = $"{nombre}: {(baseMes > tope ? $"tope {N(tope)} (base del mes {N(baseMes)})" : $"base del mes {N(baseMes)}")} × {N(tasa)} % = {N(aporteMes)}";
        return retenido != 0 ? $"{texto} − ya retenido {N(retenido)} = {N(aporteMes - retenido)}" : texto;
    }

    private static string ExplicarIsr(decimal baseMensual, IReadOnlyList<TramoEscala> escala)
    {
        var anual = baseMensual * 12;
        var tramo = Tramo(anual, escala);
        var inicio = $"renta anual {N(baseMensual)} × 12 = {N(anual)}";
        if (tramo == null || tramo.Tasa == 0)
            return $"{inicio} → exento";
        var isrAnual = IsrAnual(anual, escala);
        return $"{inicio} → tramo {N(tramo.Tasa)} %: {N(tramo.CuotaFija)} + ({N(anual)} − {N(tramo.Desde)}) × {N(tramo.Tasa)} % = {N(isrAnual)} anual ÷ 12 = {N(isrAnual / 12)} mensual";
    }

    private static decimal R(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    private static string N(decimal valor) => valor.ToString("#,0.00", Formato);
    private static string N4(decimal valor) => valor.ToString("#,0.0000", Formato);
    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
