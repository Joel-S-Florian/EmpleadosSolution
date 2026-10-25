using Empleados.Data.Entities;
using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Empleados.Data.Services;

// Exporta una TablaReporte a .xlsx (SpreadsheetML mínimo, sin dependencias):
// título, encabezado en negrita con autofiltro e inmovilizado, números con formato y fila de totales
public static class ExcelReporte
{
    private const int FilaEncabezado = 4;

    // Índices de cellXfs en styles.xml
    private const int Titulo = 1, Subtitulo = 2, Encabezado = 3, Moneda = 4, Entero = 5, General = 6, TotalTexto = 7, TotalMoneda = 8, TotalEntero = 9, TotalGeneral = 10;

    public static byte[] Generar(TablaReporte tabla, Empresa empresa)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            Escribir(zip, "[Content_Types].xml",
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                "</Types>");
            Escribir(zip, "_rels/.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>");
            Escribir(zip, "xl/workbook.xml",
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                $"<sheets><sheet name=\"{Hoja(tabla.Titulo)}\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            Escribir(zip, "xl/_rels/workbook.xml.rels",
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "</Relationships>");
            Escribir(zip, "xl/styles.xml", Estilos());
            Escribir(zip, "xl/worksheets/sheet1.xml", Hoja(tabla, empresa));
        }
        return memoria.ToArray();
    }

    private static string Hoja(TablaReporte tabla, Empresa empresa)
    {
        var columnas = tabla.Columnas.Count;
        var ultima = Letra(columnas - 1);
        var xml = new StringBuilder("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        xml.Append($"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"{FilaEncabezado}\" topLeftCell=\"A{FilaEncabezado + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");

        // Ancho según el texto más largo de cada columna
        xml.Append("<cols>");
        for (var i = 0; i < columnas; i++)
        {
            var largo = tabla.Filas.Select(f => GeneradorReportes.Celda(f[i], tabla.Columnas[i]).Length).Append(tabla.Columnas[i].Titulo.Length).Max();
            xml.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{Math.Clamp(largo + 3, 10, 50)}\" customWidth=\"1\"/>");
        }
        xml.Append("</cols><sheetData>");

        xml.Append("<row r=\"1\">").Append(Texto("A1", $"{empresa.RazonSocial} · {tabla.Titulo}", Titulo)).Append("</row>");
        xml.Append("<row r=\"2\">").Append(Texto("A2", $"{tabla.Subtitulo} · generado el {DateTime.Now:dd/MM/yyyy HH:mm}", Subtitulo)).Append("</row>");
        xml.Append($"<row r=\"{FilaEncabezado}\">");
        for (var i = 0; i < columnas; i++)
            xml.Append(Texto($"{Letra(i)}{FilaEncabezado}", tabla.Columnas[i].Titulo, Encabezado));
        xml.Append("</row>");

        var fila = FilaEncabezado;
        foreach (var valores in tabla.Filas)
            Fila(xml, ++fila, valores, tabla.Columnas, total: false);
        if (tabla.Totales is { } totales)
            Fila(xml, ++fila, totales, tabla.Columnas, total: true);

        xml.Append("</sheetData>");
        xml.Append($"<autoFilter ref=\"A{FilaEncabezado}:{ultima}{Math.Max(fila, FilaEncabezado + 1)}\"/>");
        return xml.Append("</worksheet>").ToString();
    }

    private static void Fila(StringBuilder xml, int numero, object?[] valores, List<ColumnaReporte> columnas, bool total)
    {
        xml.Append($"<row r=\"{numero}\">");
        for (var i = 0; i < valores.Length; i++)
        {
            var referencia = $"{Letra(i)}{numero}";
            if (valores[i] is decimal d)
            {
                var estilo = columnas[i].Formato switch
                {
                    "N2" => total ? TotalMoneda : Moneda,
                    "0" => total ? TotalEntero : Entero,
                    _ => total ? TotalGeneral : General
                };
                xml.Append($"<c r=\"{referencia}\" s=\"{estilo}\"><v>{d.ToString(CultureInfo.InvariantCulture)}</v></c>");
            }
            else if (valores[i] != null)
                xml.Append(Texto(referencia, valores[i]!.ToString()!, total ? TotalTexto : 0));
        }
        xml.Append("</row>");
    }

    private static string Texto(string referencia, string valor, int estilo) =>
        $"<c r=\"{referencia}\" t=\"inlineStr\"{(estilo > 0 ? $" s=\"{estilo}\"" : "")}><is><t xml:space=\"preserve\">{SecurityElement.Escape(valor)}</t></is></c>";

    // A, B, …, Z, AA, AB…
    public static string Letra(int indice) => indice < 26 ? ((char)('A' + indice)).ToString() : Letra(indice / 26 - 1) + (char)('A' + indice % 26);

    // Las hojas de Excel admiten 31 caracteres y no admiten : \ / ? * [ ]
    private static string Hoja(string titulo) =>
        SecurityElement.Escape(new string(titulo.Where(c => !":\\/?*[]".Contains(c)).Take(31).ToArray()));

    private static string Estilos() =>
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"4\"><font><sz val=\"10\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"10\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Calibri\"/></font><font><sz val=\"9\"/><color rgb=\"FF6B7280\"/><name val=\"Calibri\"/></font></fonts>" +
        "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE5E7EB\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
        "<border><left/><right/><top style=\"thin\"><color rgb=\"FF9CA3AF\"/></top><bottom/><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"11\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/>" +
        "<xf numFmtId=\"4\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
        "<xf numFmtId=\"1\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"4\" fontId=\"1\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"1\" fontId=\"1\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyBorder=\"1\"/>" +
        "</cellXfs></styleSheet>";

    private static void Escribir(ZipArchive zip, string ruta, string contenido)
    {
        using var escritor = new StreamWriter(zip.CreateEntry(ruta, CompressionLevel.Optimal).Open(), new UTF8Encoding(false));
        escritor.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        escritor.Write(contenido);
    }
}
