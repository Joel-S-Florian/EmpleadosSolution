using Empleados.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Empleados.Data.Services;

// Hoja de prestaciones y recibo de descargo
public static class LiquidacionPdf
{
    private static readonly CultureInfo Cultura = new("es-DO");

    static LiquidacionPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(Empresa empresa, Liquidacion liquidacion) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            var empleado = liquidacion.Empleado!;
            var nombre = $"{empleado.Nombre} {empleado.Apellido}";
            var lineas = LiquidacionService.Lineas(liquidacion.LineasJson);
            pagina.Size(PageSizes.Letter);
            pagina.Margin(40);
            pagina.DefaultTextStyle(t => t.FontSize(10));

            pagina.Header().Column(col =>
            {
                col.Item().Row(fila =>
                {
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().Text(empresa.RazonSocial).Bold().FontSize(14);
                        c.Item().Text($"RNC {empresa.Rnc}").FontColor(Colors.Grey.Darken1);
                    });
                    fila.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("Liquidación de prestaciones laborales").Bold().FontSize(13);
                        c.Item().AlignRight().Text(LiquidacionService.Texto(liquidacion.Causa));
                    });
                });
                if (liquidacion.Estado != EstadoLiquidacion.Pagada)
                    col.Item().PaddingTop(6).Background(Colors.Orange.Lighten4).Padding(4).AlignCenter()
                        .Text("BORRADOR: no es un comprobante de pago").Bold().FontColor(Colors.Orange.Darken3);
                col.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
            });

            pagina.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(10);
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    void Dato(string etiqueta, string? valor) =>
                        tabla.Cell().PaddingBottom(3).Text(t =>
                        {
                            t.Span(etiqueta + ": ").FontColor(Colors.Grey.Darken1);
                            t.Span(string.IsNullOrWhiteSpace(valor) ? "—" : valor).SemiBold();
                        });
                    Dato("Empleado", nombre);
                    Dato("Documento", empleado.NumeroDocumento);
                    Dato("Ingreso", liquidacion.FechaIngreso.ToString("dd/MM/yyyy"));
                    Dato("Salida", liquidacion.FechaSalida.ToString("dd/MM/yyyy"));
                    Dato("Tiempo de servicio", liquidacion.TiempoServicio);
                    Dato("Preaviso otorgado", liquidacion.PreavisoOtorgado ? "Sí" : "No");
                    Dato("Salario promedio", $"RD$ {M(liquidacion.SalarioPromedio)}");
                    Dato("Salario diario", $"RD$ {M(liquidacion.SalarioDiario)}");
                });

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    tabla.Header(h =>
                    {
                        foreach (var (titulo, derecha) in new[] { ("Concepto", false), ("Días", true), ("Pagos", true), ("Descuentos", true) })
                        {
                            var celda = h.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                            (derecha ? celda.AlignRight() : celda).Text(titulo).Bold();
                        }
                    });
                    foreach (var linea in lineas)
                    {
                        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(linea.Concepto);
                        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(linea.Dias > 0 ? linea.Dias.ToString("0.##", Cultura) : "");
                        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(linea.EsIngreso ? M(linea.Monto) : "");
                        tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(linea.EsIngreso ? "" : M(linea.Monto));
                    }
                    tabla.Cell().Padding(4).Text("Totales").Bold();
                    tabla.Cell().Padding(4);
                    tabla.Cell().Padding(4).AlignRight().Text(M(liquidacion.TotalIngresos)).Bold();
                    tabla.Cell().Padding(4).AlignRight().Text(M(liquidacion.TotalDeducciones)).Bold();
                });

                col.Item().AlignRight().Background(Colors.Grey.Lighten4).Padding(8)
                    .Text($"Neto a pagar: RD$ {M(liquidacion.Neto)}").Bold().FontSize(14);

                col.Item().PaddingTop(10).Text("Recibo de descargo").Bold().FontSize(12);
                col.Item().Text(
                    $"Yo, {nombre}, portador(a) del documento {empleado.NumeroDocumento ?? "________________"}, declaro haber recibido de " +
                    $"{empresa.RazonSocial} la suma de RD$ {M(liquidacion.Neto)} por concepto de las prestaciones y derechos detallados en este documento, " +
                    $"con motivo de la terminación de mi contrato de trabajo el {liquidacion.FechaSalida:dd/MM/yyyy}" +
                    (liquidacion.FechaPago.HasValue ? $", pagada el {liquidacion.FechaPago:dd/MM/yyyy}" : "") + ".")
                    .LineHeight(1.4f);

                col.Item().PaddingTop(40).Row(fila =>
                {
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(0.5f);
                        c.Item().AlignCenter().Text("Recibido conforme (trabajador)").FontColor(Colors.Grey.Darken1);
                    });
                    fila.ConstantItem(60);
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(0.5f);
                        c.Item().AlignCenter().Text("Por la empresa").FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            pagina.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                t.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}. Cálculo de referencia según el Código de Trabajo; compárelo con la calculadora del Ministerio de Trabajo.");
            });
        })).GeneratePdf();

    private static string M(decimal monto) => monto.ToString("N2", Cultura);
}
