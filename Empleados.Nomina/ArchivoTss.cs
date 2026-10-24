using System.Globalization;
using System.Text;

namespace Empleados.Nomina;

public record TrabajadorTss(
    string ClaveNomina,
    // C = cédula, N = NSS, P = pasaporte
    char TipoDocumento,
    string Documento,
    string Nombres,
    string PrimerApellido,
    string SegundoApellido,
    char? Sexo,
    DateTime? FechaNacimiento,
    decimal SalarioSs,
    // Solo si difiere del salario de la TSS
    decimal SalarioIsr,
    // Lo que grava ISR pero no cotiza a la TSS (bonificación, etc.)
    decimal OtrasRemuneraciones,
    // 0001 normal, 0004 no laboró el mes completo
    string TipoIngreso,
    decimal Regalia = 0,
    decimal Prestaciones = 0);

// Archivo de autodeterminación mensual (AM) para el SUIR Plus: texto de ancho fijo con
// encabezado (E), un detalle (D) por trabajador y sumario (S).
// shortcut: layout tomado de una implementación de referencia del instructivo TSS v6.0;
// validarlo con el "Validador de estructura de archivos" del SUIR antes del primer envío real.
public static class ArchivoTss
{
    public const string Autodeterminacion = "AM";
    public const string TipoNormal = "0001";
    public const string TipoMesIncompleto = "0004";
    public const int LongitudDetalle = 312;
    public const int LongitudConExentos = 366;

    public static string Detalle(TrabajadorTss t)
    {
        var linea = new StringBuilder()
            .Append('D')
            .Append(t.ClaveNomina.PadLeft(3, '0')[..3])
            .Append(t.TipoDocumento)
            .Append(Texto(t.Documento, 25))
            .Append(Texto(t.Nombres, 50))
            .Append(Texto(t.PrimerApellido, 40))
            .Append(Texto(t.SegundoApellido, 40))
            .Append(t.Sexo is 'M' or 'F' ? t.Sexo.Value : ' ')
            .Append(t.FechaNacimiento?.ToString("ddMMyyyy") ?? new string(' ', 8))
            .Append(Monto(t.SalarioSs))
            .Append(Monto(0))                 // aporte ordinario voluntario
            .Append(Monto(t.SalarioIsr == t.SalarioSs ? 0 : t.SalarioIsr))
            .Append(Monto(t.OtrasRemuneraciones))
            .Append(new string(' ', 11))      // RNC del agente de retención único
            .Append(Monto(0))                 // remuneraciones de otros empleadores
            .Append(Monto(0))                 // ingresos exentos: se detallan al final
            .Append(Monto(0))                 // saldo a favor
            .Append(Monto(0))                 // salario INFOTEP: igual al de la TSS
            .Append(t.TipoIngreso);
        // Regalía (01), preaviso y cesantía (02) y pensión alimenticia (03), solo si hay alguno
        if (t.Regalia > 0 || t.Prestaciones > 0)
            linea.Append("01").Append(Monto(t.Regalia)).Append("02").Append(Monto(t.Prestaciones)).Append("03").Append(Monto(0));

        var texto = linea.ToString();
        if (texto.Length is not (LongitudDetalle or LongitudConExentos))
            throw new InvalidOperationException($"Registro de la TSS con longitud {texto.Length} para el documento {t.Documento}");
        return texto;
    }

    // (nombre, contenido) con CRLF; el sumario cuenta todos los registros, incluidos E y S
    public static (string Nombre, string Contenido) Generar(string rnc, int anio, int mes, IEnumerable<TrabajadorTss> trabajadores)
    {
        var rncLimpio = new string(rnc.Where(char.IsDigit).ToArray());
        var mesAnio = $"{mes:00}{anio:0000}";
        var lineas = new List<string> { "E" + Autodeterminacion + rncLimpio.PadLeft(11) + mesAnio };
        lineas.AddRange(trabajadores.Select(Detalle));
        lineas.Add("S" + (lineas.Count + 1).ToString("000000"));
        return ($"{Autodeterminacion}_{rncLimpio}_{mesAnio}.txt", string.Join("\r\n", lineas) + "\r\n");
    }

    // Mayúsculas sin tildes ni eñes: cada carácter ocupa un byte
    public static string Texto(string? valor, int largo)
    {
        var sinTildes = new string((valor ?? "").Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c < 128).ToArray());
        var limpio = string.Join(' ', sinTildes.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return (limpio.Length > largo ? limpio[..largo] : limpio).PadRight(largo);
    }

    // 16 posiciones: ceros a la izquierda y dos decimales con punto
    public static string Monto(decimal valor) =>
        Math.Max(Math.Round(valor, 2, MidpointRounding.AwayFromZero), 0).ToString("0000000000000.00", CultureInfo.InvariantCulture);
}
