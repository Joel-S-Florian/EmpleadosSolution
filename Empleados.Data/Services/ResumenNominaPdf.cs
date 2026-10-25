using Empleados.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Empleados.Data.Services;

// Resumen de la nómina: una fila por empleado (sin el detalle de cada recibo), totales y firmas
public static class ResumenNominaPdf
{
    private static readonly CultureInfo Cultura = new("es-DO");

    static ResumenNominaPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(Empresa empresa, NominaPeriodo nomina) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            var recibos = nomina.Recibos.OrderBy(r => r.Departamento).ThenBy(r => r.NombreEmpleado).ToList();
            var definitivo = nomina.Estado is EstadoNomina.Pagada or EstadoNomina.Cerrada;
            pagina.Size(PageSizes.Letter.Landscape());
            pagina.Margin(28);
            pagina.DefaultTextStyle(t => t.FontSize(8.5f));

            pagina.Header().Column(col =>
            {
                col.Item().Row(fila =>
                {
                    fila.RelativeItem().Column(c =>
                    {
                        c.Item().Text(empresa.RazonSocial).Bold().FontSize(13);
                        c.Item().Text($"RNC {empresa.Rnc}").FontColor(Colors.Grey.Darken1);
                    });
                    fila.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text($"Resumen de nómina · {nomina.Descripcion}").Bold().FontSize(12);
                        c.Item().AlignRight().Text($"Del {nomina.FechaInicio:dd/MM/yyyy} al {nomina.FechaFin:dd/MM/yyyy} · {recibos.Count} empleado(s) · {nomina.Estado}").FontColor(Colors.Grey.Darken1);
                        if (nomina.FechaPago.HasValue)
                            c.Item().AlignRight().Text($"Pagada el {nomina.FechaPago:dd/MM/yyyy}");
                    });
                });
                if (!definitivo)
                    col.Item().PaddingTop(5).Background(Colors.Orange.Lighten4).Padding(3).AlignCenter()
                        .Text($"BORRADOR ({nomina.Estado}): los montos pueden cambiar").Bold().FontColor(Colors.Orange.Darken3);
                col.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
            });

            pagina.Content().PaddingTop(8).Column(col =>
            {
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(22);
                        c.RelativeColumn(3);
                        c.RelativeColumn(1.6f);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(2.6f);
                    });
                    tabla.Header(h =>
                    {
                        foreach (var (titulo, derecha) in new[] { ("#", false), ("Empleado", false), ("Documento", false), ("Departamento", false),
                                     ("Ingresos", true), ("Ded. de ley", true), ("Otras ded.", true), ("Neto", true), ("Forma de pago", false) })
                        {
                            var celda = h.Cell().Background(Colors.Grey.Lighten3).Padding(3);
                            (derecha ? celda.AlignRight() : celda).Text(titulo).Bold();
                        }
                    });

                    var numero = 0;
                    foreach (var r in recibos)
                    {
                        IContainer Celda() => tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);
                        Celda().Text((++numero).ToString()).FontColor(Colors.Grey.Darken1);
                        Celda().Text(r.NombreEmpleado);
                        Celda().Text(r.Documento ?? "—");
                        Celda().Text(r.Departamento ?? "—");
                        Celda().AlignRight().Text(M(r.TotalIngresos));
                        Celda().AlignRight().Text(M(r.TotalDeduccionesLey));
                        Celda().AlignRight().Text(M(r.TotalDeduccionesInternas));
                        Celda().AlignRight().Text(M(r.Neto)).SemiBold();
                        Celda().Text(r.Banco == null ? "Cheque / efectivo" : $"{r.Banco} {r.NumeroCuenta}");
                    }

                    tabla.Cell().ColumnSpan(4).Padding(3).Text("Totales").Bold();
                    tabla.Cell().Padding(3).AlignRight().Text(M(recibos.Sum(r => r.TotalIngresos))).Bold();
                    tabla.Cell().Padding(3).AlignRight().Text(M(recibos.Sum(r => r.TotalDeduccionesLey))).Bold();
                    tabla.Cell().Padding(3).AlignRight().Text(M(recibos.Sum(r => r.TotalDeduccionesInternas))).Bold();
                    tabla.Cell().Padding(3).AlignRight().Text(M(recibos.Sum(r => r.Neto))).Bold();
                    tabla.Cell().Padding(3);
                });

                col.Item().PaddingTop(10).Row(fila =>
                {
                    void Dato(string etiqueta, decimal valor) => fila.RelativeItem().Background(Colors.Grey.Lighten4).Padding(6).Column(c =>
                    {
                        c.Item().Text(etiqueta).FontColor(Colors.Grey.Darken1);
                        c.Item().Text($"RD$ {M(valor)}").Bold().FontSize(10);
                    });
                    Dato("Neto a pagar", nomina.TotalNeto);
                    fila.ConstantItem(8);
                    Dato("Por banco", recibos.Where(r => r.Banco != null).Sum(r => r.Neto));
                    fila.ConstantItem(8);
                    Dato("Cheque o efectivo", recibos.Where(r => r.Banco == null).Sum(r => r.Neto));
                    fila.ConstantItem(8);
                    Dato("Aportes del empleador", nomina.TotalAportesPatronales);
                    fila.ConstantItem(8);
                    Dato("Costo total", nomina.TotalIngresos + nomina.TotalAportesPatronales);
                });

                col.Item().PaddingTop(36).Row(fila =>
                {
                    void Firma(string texto) => fila.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(0.5f);
                        c.Item().AlignCenter().Text(texto).FontColor(Colors.Grey.Darken1);
                    });
                    Firma($"Preparada por {nomina.CreadaPor}");
                    fila.ConstantItem(40);
                    Firma(nomina.AprobadaPor != null ? $"Aprobada por {nomina.AprobadaPor}" : "Aprobada por");
                    fila.ConstantItem(40);
                    Firma("Recibido (contabilidad)");
                });
            });

            pagina.Footer().AlignCenter().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                t.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        })).GeneratePdf();

    private static string M(decimal monto) => monto.ToString("N2", Cultura);
}
