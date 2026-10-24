namespace Empleados.Data.Entities;

// Valores legales con fecha de vigencia: una nómina usa siempre los vigentes en su período.
// Nunca se editan: un cambio de ley se registra como una nueva vigencia.
public class ParametroLegal
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime VigenteDesde { get; set; }
    public string? Fuente { get; set; }
}

// Escala anual de ISR de asalariados: ISR = CuotaFija + (renta anual - Desde) * Tasa%, para Desde < renta <= Hasta
public class TramoIsr
{
    public int Id { get; set; }
    public DateTime VigenteDesde { get; set; }
    public decimal Desde { get; set; }
    public decimal? Hasta { get; set; }
    public decimal Tasa { get; set; }
    public decimal CuotaFija { get; set; }
}

// shortcut: solo sector privado no sectorizado; agregar sector cuando haya empresas de zona franca, hoteles, etc.
public class SalarioMinimo
{
    public int Id { get; set; }
    public DateTime VigenteDesde { get; set; }
    public TamanoEmpresa Tamano { get; set; }
    public decimal Monto { get; set; }
}

// Fecha en que se disfruta el feriado (ya aplicado el traslado de la Ley 139-97)
public class Feriado
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public static class ClavesParametro
{
    public const string AfpEmpleado = "TSS.AFP.Empleado";
    public const string AfpEmpleador = "TSS.AFP.Empleador";
    public const string SfsEmpleado = "TSS.SFS.Empleado";
    public const string SfsEmpleador = "TSS.SFS.Empleador";
    public const string SalarioMinimoCotizable = "TSS.SalarioMinimoCotizable";
    public const string TopeAfpSalarios = "TSS.TopeAFP.SalariosMinimos";
    public const string TopeSfsSalarios = "TSS.TopeSFS.SalariosMinimos";
    public const string TopeSrlSalarios = "TSS.TopeSRL.SalariosMinimos";
    public const string InfotepEmpleador = "INFOTEP.Empleador";
    public const string InfotepBonificacion = "INFOTEP.EmpleadoBonificacion";
    public const string DivisorSalarioDiario = "Laboral.DivisorSalarioDiario";
    public const string HorasJornadaDiaria = "Laboral.HorasJornadaDiaria";
    public const string HorasJornadaSemanal = "Laboral.HorasJornadaSemanal";
    public const string UmbralHorasExtraExceso = "Laboral.UmbralHorasExtraExceso";
    public const string RecargoHoraExtra = "Laboral.RecargoHoraExtra";
    public const string RecargoHoraExtraExceso = "Laboral.RecargoHoraExtraExceso";
    public const string RecargoNocturno = "Laboral.RecargoNocturno";
    public const string RecargoFeriado = "Laboral.RecargoFeriado";
    public const string VacacionesHasta5Anios = "Laboral.VacacionesDias.Hasta5Anios";
    public const string VacacionesMas5Anios = "Laboral.VacacionesDias.Mas5Anios";
    public const string LicenciaPaternidadDias = "Laboral.LicenciaPaternidadDias";
    public const string LicenciaMaternidadSemanas = "Laboral.LicenciaMaternidadSemanas";

    // Clave -> descripción para pantallas y validación
    public static readonly IReadOnlyDictionary<string, string> Descripciones = new Dictionary<string, string>
    {
        [AfpEmpleado] = "AFP: aporte del empleado (%)",
        [AfpEmpleador] = "AFP: aporte del empleador (%)",
        [SfsEmpleado] = "SFS: aporte del empleado (%)",
        [SfsEmpleador] = "SFS: aporte del empleador (%)",
        [SalarioMinimoCotizable] = "TSS: salario mínimo cotizable (RD$), base de los topes",
        [TopeAfpSalarios] = "Tope AFP (cantidad de salarios mínimos cotizables)",
        [TopeSfsSalarios] = "Tope SFS (cantidad de salarios mínimos cotizables)",
        [TopeSrlSalarios] = "Tope SRL (cantidad de salarios mínimos cotizables)",
        [InfotepEmpleador] = "INFOTEP: aporte del empleador sobre la nómina (%)",
        [InfotepBonificacion] = "INFOTEP: retención al empleado sobre la bonificación (%)",
        [DivisorSalarioDiario] = "Divisor del salario diario (días laborables promedio al mes)",
        [HorasJornadaDiaria] = "Horas de la jornada diaria",
        [HorasJornadaSemanal] = "Horas de la jornada semanal",
        [UmbralHorasExtraExceso] = "Horas semanales a partir de las que el recargo extra es mayor",
        [RecargoHoraExtra] = "Recargo hora extra (%)",
        [RecargoHoraExtraExceso] = "Recargo hora extra por encima del umbral (%)",
        [RecargoNocturno] = "Recargo por jornada nocturna (%)",
        [RecargoFeriado] = "Recargo por trabajar feriado o descanso semanal (%)",
        [VacacionesHasta5Anios] = "Días de vacaciones de 1 a 5 años de servicio",
        [VacacionesMas5Anios] = "Días de vacaciones con más de 5 años de servicio",
        [LicenciaPaternidadDias] = "Días de licencia de paternidad",
        [LicenciaMaternidadSemanas] = "Semanas de licencia de maternidad"
    };
}
