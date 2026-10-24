using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Empleados.Nomina;

public enum CausaTerminacion
{
    [Display(Name = "Desahucio por el empleador")] Desahucio,
    [Display(Name = "Despido injustificado")] DespidoInjustificado,
    [Display(Name = "Despido justificado")] DespidoJustificado,
    [Display(Name = "Dimisión justificada")] DimisionJustificada,
    [Display(Name = "Renuncia (desahucio del trabajador)")] Renuncia,
    [Display(Name = "Muerte, incapacidad o cierre de la empresa")] MuerteIncapacidadCierre,
    [Display(Name = "Mutuo acuerdo")] MutuoAcuerdo
}

public record EntradaPrestaciones(
    CausaTerminacion Causa,
    DateTime Ingreso,
    DateTime Salida,
    // Salario promedio mensual del último año (base de todas las prestaciones)
    decimal SalarioPromedioMensual,
    // El empleador avisó con el tiempo de ley: no se paga preaviso
    bool PreavisoOtorgado,
    // Días de vacaciones ganados y no tomados del año de servicio ya cumplido
    decimal DiasVacacionesPendientes,
    // Salario ordinario del año calendario hasta la salida (para la Navidad proporcional)
    decimal SalarioOrdinarioAnio,
    decimal DivisorSalarioDiario,
    decimal MontoAcordado = 0);

public record LineaPrestacion(string Codigo, string Concepto, decimal Dias, decimal Monto);

public record ResultadoPrestaciones(int Anios, int Meses, int Dias, decimal SalarioDiario, IReadOnlyList<LineaPrestacion> Lineas, IReadOnlyList<string> Explicacion)
{
    public decimal Total => Lineas.Sum(l => l.Monto);
    public string TiempoServicio => $"{Anios} año(s), {Meses} mes(es) y {Dias} día(s)";
}

// Prestaciones laborales al terminar el contrato (Código de Trabajo, arts. 76, 80, 82, 177, 180 y 219)
public static class Prestaciones
{
    public static class Codigos
    {
        public const string Preaviso = "PREAVISO";
        public const string Cesantia = "CESANTIA";
        public const string Asistencia = "ASISTENCIA";
        public const string Vacaciones = "VACACIONES";
        public const string Navidad = "NAVIDAD";
        public const string Acordado = "ACORDADO";
    }

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    public static ResultadoPrestaciones Calcular(EntradaPrestaciones e)
    {
        if (e.Salida.Date < e.Ingreso.Date)
            throw new ArgumentException("La fecha de salida no puede ser anterior al ingreso");
        if (e.SalarioPromedioMensual < 0 || e.DiasVacacionesPendientes < 0 || e.SalarioOrdinarioAnio < 0 || e.MontoAcordado < 0)
            throw new ArgumentException("Los montos no pueden ser negativos");

        var (anios, meses, dias) = Tiempo(e.Ingreso, e.Salida);
        var mesesTotales = anios * 12 + meses;
        var diario = e.SalarioPromedioMensual / e.DivisorSalarioDiario;
        var lineas = new List<LineaPrestacion>();
        var explicacion = new List<string>
        {
            $"Tiempo de servicio: {anios} año(s), {meses} mes(es) y {dias} día(s)",
            $"Salario diario: {N(e.SalarioPromedioMensual)} / {N(e.DivisorSalarioDiario)} = {N(R(diario))}"
        };

        void Agregar(string codigo, string concepto, decimal cantidad, string regla)
        {
            if (cantidad <= 0)
                return;
            var monto = R(diario * cantidad);
            lineas.Add(new LineaPrestacion(codigo, concepto, cantidad, monto));
            explicacion.Add($"{concepto}: {regla} = {N(cantidad)} días × {N(R(diario))} = {N(monto)}");
        }

        var pagaPreavisoYCesantia = e.Causa is CausaTerminacion.Desahucio or CausaTerminacion.DespidoInjustificado or CausaTerminacion.DimisionJustificada;
        if (pagaPreavisoYCesantia && !e.PreavisoOtorgado)
            Agregar(Codigos.Preaviso, "Preaviso (art. 76)", DiasPreaviso(mesesTotales), DescripcionPreaviso(mesesTotales));
        if (pagaPreavisoYCesantia)
            Agregar(Codigos.Cesantia, "Auxilio de cesantía (art. 80)", DiasCesantia(anios, meses), DescripcionCesantia(anios, meses));
        if (e.Causa == CausaTerminacion.MuerteIncapacidadCierre)
            Agregar(Codigos.Asistencia, "Asistencia económica (art. 82)", DiasAsistencia(anios, meses), DescripcionAsistencia(anios, meses));

        // Vacaciones: lo pendiente del año cumplido más la proporción del año en curso (art. 180)
        var proporcion = DiasVacacionesProporcionales(meses);
        Agregar(Codigos.Vacaciones, "Vacaciones no disfrutadas", e.DiasVacacionesPendientes + proporcion,
            $"{N(e.DiasVacacionesPendientes)} pendientes + {proporcion} por {meses} mes(es) del año en curso");

        var navidad = R(e.SalarioOrdinarioAnio / 12);
        if (navidad > 0)
        {
            lineas.Add(new LineaPrestacion(Codigos.Navidad, "Salario de Navidad proporcional (art. 219)", 0, navidad));
            explicacion.Add($"Salario de Navidad: {N(e.SalarioOrdinarioAnio)} del año / 12 = {N(navidad)}");
        }

        if (e.MontoAcordado > 0)
        {
            lineas.Add(new LineaPrestacion(Codigos.Acordado, "Monto acordado", 0, R(e.MontoAcordado)));
            explicacion.Add($"Monto acordado entre las partes: {N(e.MontoAcordado)}");
        }

        return new ResultadoPrestaciones(anios, meses, dias, R(diario), lineas, explicacion);
    }

    // Art. 76: 7 días de 3 a 6 meses, 14 días de 6 meses a 1 año, 28 días después de un año
    public static int DiasPreaviso(int mesesTotales) => mesesTotales switch
    {
        < 3 => 0,
        < 6 => 7,
        < 12 => 14,
        _ => 28
    };

    // Art. 80: 6 días de 3 a 6 meses, 13 de 6 meses a 1 año, 21 por año de 1 a 5 años, 23 por año desde 5 años;
    // la fracción de año de más de 3 meses suma 6 o 13 días
    public static decimal DiasCesantia(int anios, int meses)
    {
        var fraccion = meses switch { < 3 => 0, < 6 => 6, _ => 13 };
        return anios == 0 ? fraccion : anios * (anios >= 5 ? 23 : 21) + fraccion;
    }

    // Art. 82: 5 días de 3 a 6 meses, 10 de 6 meses a 1 año, 15 por año después de un año
    public static decimal DiasAsistencia(int anios, int meses)
    {
        if (anios == 0)
            return meses switch { < 3 => 0, < 6 => 5, _ => 10 };
        return anios * 15 + meses switch { < 3 => 0, < 6 => 5, _ => 10 };
    }

    // Art. 180: 6 días con 5 meses, uno más por cada mes hasta 12 días con 11 meses
    public static int DiasVacacionesProporcionales(int mesesDelAnioEnCurso) =>
        mesesDelAnioEnCurso < 5 ? 0 : Math.Min(mesesDelAnioEnCurso + 1, 12);

    public static (int Anios, int Meses, int Dias) Tiempo(DateTime ingreso, DateTime salida)
    {
        var desde = ingreso.Date;
        var hasta = salida.Date.AddDays(1); // el día de salida se trabaja
        var meses = (hasta.Year - desde.Year) * 12 + hasta.Month - desde.Month;
        if (desde.AddMonths(meses) > hasta)
            meses--;
        var dias = (hasta - desde.AddMonths(meses)).Days;
        return (meses / 12, meses % 12, dias);
    }

    private static string DescripcionPreaviso(int mesesTotales) => mesesTotales switch
    {
        < 6 => "de 3 a 6 meses",
        < 12 => "de 6 meses a 1 año",
        _ => "más de un año"
    };

    private static string DescripcionCesantia(int anios, int meses) =>
        anios == 0 ? (meses < 6 ? "de 3 a 6 meses" : "de 6 meses a 1 año")
            : $"{anios} año(s) × {(anios >= 5 ? 23 : 21)}" + (meses >= 3 ? $" + {(meses < 6 ? 6 : 13)} por {meses} mes(es)" : "");

    private static string DescripcionAsistencia(int anios, int meses) =>
        (anios > 0 ? $"{anios} año(s) × 15" : "") + (meses >= 3 ? $"{(anios > 0 ? " + " : "")}{(meses < 6 ? 5 : 10)} por {meses} mes(es)" : "");

    private static decimal R(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    private static string N(decimal valor) => valor.ToString("N2", Cultura);
}
