using Empleados.Data.Entities;
using Empleados.Nomina;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Empleados.Data.Services;

// Recibo de pago en PDF: una página por empleado. Sin los aportes del empleador (no son del empleado).
public static class ReciboPdf
{
    private static readonly CultureInfo Cultura = new("es-DO");

    static ReciboPdf()
    {
        // Licencia Community: gratuita para empresas con ingresos anuales menores de 1 millón de dólares
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(Empresa empresa, NominaPeriodo nomina, IEnumerable<ReciboPago> recibos) =>
        Document.Create(documento =>
        {
            foreach (var recibo in recibos)
                documento.Page(pagina => Pagina(pagina, empresa, nomina, recibo));
        }).GeneratePdf();

    private static void Pagina(PageDescriptor pagina, Empresa empresa, NominaPeriodo nomina, ReciboPago recibo)
    {
        var definitivo = nomina.Estado is EstadoNomina.Pagada or EstadoNomina.Cerrada;
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
                    c.Item().AlignRight().Text("Recibo de pago").Bold().FontSize(13);
                    c.Item().AlignRight().Text(nomina.Descripcion);
                    c.Item().AlignRight().Text($"Del {nomina.FechaInicio:dd/MM/yyyy} al {nomina.FechaFin:dd/MM/yyyy}").FontColor(Colors.Grey.Darken1);
                    if (nomina.FechaPago.HasValue)
                        c.Item().AlignRight().Text($"Pagado el {nomina.FechaPago:dd/MM/yyyy}");
                });
            });
            if (!definitivo)
                col.Item().PaddingTop(6).Background(Colors.Orange.Lighten4).Padding(4).AlignCenter()
                    .Text($"BORRADOR ({nomina.Estado}): no es un comprobante de pago").Bold().FontColor(Colors.Orange.Darken3);
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
                Dato("Empleado", recibo.NombreEmpleado);
                Dato("Documento", recibo.Documento);
                Dato("Departamento", recibo.Departamento);
                Dato("NSS", recibo.Nss);
                Dato("Puesto", recibo.Puesto);
                Dato("Salario", recibo.TipoSalario == TipoSalario.PorHora ? $"RD$ {M(recibo.Salario)} por hora" : $"RD$ {M(recibo.Salario)} mensual");
                Dato("Cuenta", recibo.Banco == null ? "Sin cuenta" : $"{recibo.Banco} {recibo.NumeroCuenta}");
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
                    foreach (var (titulo, derecha) in new[] { ("Concepto", false), ("Cantidad", true), ("Ingresos", true), ("Deducciones", true) })
                    {
                        var celda = h.Cell().Background(Colors.Grey.Lighten3).Padding(4);
                        (derecha ? celda.AlignRight() : celda).Text(titulo).Bold();
                    }
                });

                foreach (var linea in recibo.Lineas.Where(l => l.Tipo != nameof(TipoLinea.AportePatronal)).OrderBy(l => l.Orden))
                {
                    var ingreso = linea.Tipo == nameof(TipoLinea.Ingreso);
                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(linea.Concepto);
                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(linea.Cantidad?.ToString("0.##", Cultura) ?? "");
                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(ingreso ? M(linea.Monto) : "");
                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(ingreso ? "" : M(linea.Monto));
                }

                tabla.Cell().Padding(4).Text("Totales").Bold();
                tabla.Cell().Padding(4);
                tabla.Cell().Padding(4).AlignRight().Text(M(recibo.TotalIngresos)).Bold();
                tabla.Cell().Padding(4).AlignRight().Text(M(recibo.TotalDeduccionesLey + recibo.TotalDeduccionesInternas)).Bold();
            });

            col.Item().AlignRight().Background(Colors.Grey.Lighten4).Padding(8)
                .Text($"Neto a pagar: RD$ {M(recibo.Neto)}").Bold().FontSize(14);

            col.Item().PaddingTop(40).Row(fila =>
            {
                fila.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(0.5f);
                    c.Item().AlignCenter().Text("Recibido conforme (empleado)").FontColor(Colors.Grey.Darken1);
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
            t.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Página ");
            t.CurrentPageNumber();
            t.Span(" de ");
            t.TotalPages();
        });
    }

    private static string M(decimal monto) => monto.ToString("N2", Cultura);
}
