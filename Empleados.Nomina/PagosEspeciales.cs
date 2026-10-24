using System.Globalization;

namespace Empleados.Nomina;

public record CalculoSalarioNavidad(decimal Monto, decimal TotalDevengado, IReadOnlyList<string> Explicacion, IReadOnlyList<string> Advertencias);

public record ParticipanteBonificacion(int Id, decimal SalarioMensual, DateTime Ingreso);

public record ParteBonificacion(int Id, decimal FactorTiempo, decimal Proporcional, int DiasTope, decimal Tope, decimal Monto);

// Salario de Navidad (art. 219) y participación en los beneficios (art. 223 del Código de Trabajo)
public static class PagosEspeciales
{
    public const decimal PorcentajeUtilidades = 10m;
    public const int DiasTopeMenos3Anios = 45;
    public const int DiasTope3AniosOMas = 60;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    // 1/12 del salario ordinario del año. Mes con nómina completa: lo registrado (salario menos ausencias);
    // mes sin nómina: el salario mensual proporcional a los días en la empresa.
    public static CalculoSalarioNavidad SalarioNavidad(int anio, DateTime ingreso, decimal salarioMensual, bool porHora,
        IReadOnlyDictionary<int, decimal> registradoPorMes)
    {
        var explicacion = new List<string>();
        var advertencias = new List<string>();
        decimal total = 0;
        for (var mes = 1; mes <= 12; mes++)
        {
            var nombre = Cultura.DateTimeFormat.GetAbbreviatedMonthName(mes);
            if (registradoPorMes.TryGetValue(mes, out var registrado))
            {
                total += registrado;
                explicacion.Add($"{nombre}: {N(registrado)} (nómina)");
                continue;
            }

            var inicioMes = new DateTime(anio, mes, 1);
            var finMes = inicioMes.AddMonths(1).AddDays(-1);
            if (ingreso.Date > finMes)
                continue;
            if (porHora)
            {
                advertencias.Add($"{nombre}: sin nómina registrada; en salario por hora no se puede estimar");
                continue;
            }
            var dias = (finMes - (ingreso.Date > inicioMes ? ingreso.Date : inicioMes)).Days + 1;
            var estimado = R(salarioMensual * dias / DateTime.DaysInMonth(anio, mes));
            total += estimado;
            explicacion.Add($"{nombre}: {N(estimado)} (estimado{(dias < DateTime.DaysInMonth(anio, mes) ? $", {dias} días" : "")})");
        }

        var monto = R(total / 12);
        explicacion.Add($"Salario ordinario del año {N(total)} / 12 = {N(monto)}");
        return new CalculoSalarioNavidad(monto, total, explicacion, advertencias);
    }

    // El 10 % de la utilidad se reparte en proporción al salario y al tiempo trabajado en el ejercicio,
    // con tope de 45 días de salario (menos de 3 años) o 60 días (3 años o más)
    public static IReadOnlyList<ParteBonificacion> Bonificacion(decimal utilidadNeta, DateTime inicioEjercicio, DateTime cierreEjercicio,
        IReadOnlyList<ParticipanteBonificacion> empleados, decimal divisorSalarioDiario)
    {
        if (utilidadNeta <= 0 || empleados.Count == 0)
            return empleados.Select(e => new ParteBonificacion(e.Id, 0, 0, 0, 0, 0)).ToList();

        var diasEjercicio = (cierreEjercicio.Date - inicioEjercicio.Date).Days + 1;
        var factores = empleados.ToDictionary(e => e.Id, e =>
        {
            var desde = e.Ingreso.Date > inicioEjercicio.Date ? e.Ingreso.Date : inicioEjercicio.Date;
            return desde > cierreEjercicio.Date ? 0m : Math.Round((decimal)((cierreEjercicio.Date - desde).Days + 1) / diasEjercicio, 6);
        });
        var pesoTotal = empleados.Sum(e => e.SalarioMensual * factores[e.Id]);
        var aRepartir = R(utilidadNeta * PorcentajeUtilidades / 100);

        return empleados.Select(e =>
        {
            var factor = factores[e.Id];
            var proporcional = pesoTotal == 0 ? 0 : R(aRepartir * e.SalarioMensual * factor / pesoTotal);
            var dias = AniosCumplidos(e.Ingreso, cierreEjercicio) >= 3 ? DiasTope3AniosOMas : DiasTopeMenos3Anios;
            var tope = R(e.SalarioMensual / divisorSalarioDiario * dias * factor);
            return new ParteBonificacion(e.Id, factor, proporcional, dias, tope, Math.Min(proporcional, tope));
        }).ToList();
    }

    private static int AniosCumplidos(DateTime desde, DateTime hasta)
    {
        var anios = hasta.Year - desde.Year;
        return desde.Date.AddYears(anios) > hasta.Date ? anios - 1 : anios;
    }

    private static decimal R(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    private static string N(decimal valor) => valor.ToString("N2", Cultura);
}
