namespace Empleados.Nomina;

// Año de servicio en curso y los días de vacaciones que le corresponden
public record DerechoVacaciones(int AniosServicio, DateTime Desde, DateTime Hasta, int Dias);

// Feriados (Ley 139-97), días laborables y vacaciones (art. 177 del Código de Trabajo)
public static class CalendarioLaboral
{
    // Feriados del año con la fecha en que se disfrutan
    public static IReadOnlyList<(DateTime Fecha, string Nombre)> Feriados(int anio)
    {
        var pascua = DomingoDePascua(anio);
        return new[]
        {
            (new DateTime(anio, 1, 1), "Año Nuevo"),
            Movible(new DateTime(anio, 1, 6), "Día de los Santos Reyes"),
            (new DateTime(anio, 1, 21), "Día de la Altagracia"),
            Movible(new DateTime(anio, 1, 26), "Día de Duarte"),
            (new DateTime(anio, 2, 27), "Día de la Independencia"),
            (pascua.AddDays(-2), "Viernes Santo"),
            Movible(new DateTime(anio, 5, 1), "Día del Trabajo"),
            (pascua.AddDays(60), "Corpus Christi"),
            (new DateTime(anio, 8, 16), "Día de la Restauración"),
            (new DateTime(anio, 9, 24), "Día de las Mercedes"),
            Movible(new DateTime(anio, 11, 6), "Día de la Constitución"),
            (new DateTime(anio, 12, 25), "Navidad")
        }.OrderBy(f => f.Item1).ToList();
    }

    // Ley 139-97: martes y miércoles pasan al lunes anterior; jueves y viernes, al lunes siguiente
    private static (DateTime, string) Movible(DateTime fecha, string nombre)
    {
        var dias = fecha.DayOfWeek switch
        {
            DayOfWeek.Tuesday => -1,
            DayOfWeek.Wednesday => -2,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 3,
            _ => 0
        };
        return dias == 0 ? (fecha, nombre) : (fecha.AddDays(dias), nombre + " (trasladado)");
    }

    // Algoritmo gregoriano anónimo (Meeus/Jones/Butcher)
    public static DateTime DomingoDePascua(int anio)
    {
        int a = anio % 19, b = anio / 100, c = anio % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int mes = (h + l - 7 * m + 114) / 31, dia = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(anio, mes, dia);
    }

    // shortcut: cuenta lunes a viernes (sin feriados); si la empresa trabaja sábados, el usuario ajusta los días a mano
    public static int DiasLaborables(DateTime desde, DateTime hasta, IEnumerable<DateTime> feriados)
    {
        var libres = feriados.Select(f => f.Date).ToHashSet();
        var dias = 0;
        for (var d = desde.Date; d <= hasta.Date; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !libres.Contains(d))
                dias++;
        return dias;
    }

    // Art. 177: 14 días laborables al cumplir de 1 a 4 años; 18 desde los 5 años de servicio
    public static DerechoVacaciones Vacaciones(DateTime ingreso, DateTime fecha, int diasHasta5Anios, int diasMas5Anios)
    {
        var anios = fecha.Year - ingreso.Year;
        if (ingreso.Date.AddYears(anios) > fecha.Date)
            anios--;
        anios = Math.Max(anios, 0);
        var desde = ingreso.Date.AddYears(anios);
        var dias = anios == 0 ? 0 : anios >= 5 ? diasMas5Anios : diasHasta5Anios;
        return new DerechoVacaciones(anios, desde, desde.AddYears(1).AddDays(-1), dias);
    }
}
