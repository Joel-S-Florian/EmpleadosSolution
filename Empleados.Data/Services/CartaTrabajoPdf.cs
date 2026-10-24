using Empleados.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace Empleados.Data.Services;

// Nombre para mostrar de un enum: su [Display(Name)] o el nombre del valor
public static class Textos
{
    public static string De(Enum valor) =>
        valor.GetType().GetField(valor.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
}

// Carta de trabajo (certificación laboral) que el empleado descarga desde el portal
public static class CartaTrabajoPdf
{
    private static readonly CultureInfo Cultura = new("es-DO");

    static CartaTrabajoPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generar(Empresa empresa, Empleado empleado, DateTime fecha) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.Letter);
            pagina.Margin(60);
            pagina.DefaultTextStyle(t => t.FontSize(11).LineHeight(1.5f));

            pagina.Header().Column(c =>
            {
                c.Item().Text(empresa.RazonSocial).Bold().FontSize(15);
                c.Item().Text($"RNC {empresa.Rnc}").FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(empresa.Direccion))
                    c.Item().Text(empresa.Direccion).FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
            });

            pagina.Content().PaddingTop(30).Column(c =>
            {
                c.Spacing(14);
                c.Item().AlignRight().Text(fecha.ToString("d 'de' MMMM 'de' yyyy", Cultura));
                c.Item().Text("A QUIEN PUEDA INTERESAR").Bold();
                var actual = empleado.Estado != EstadoEmpleado.Despedido;
                var salario = empleado.TipoSalario == TipoSalario.PorHora
                    ? $"una tarifa de RD$ {empleado.Salario.ToString("N2", Cultura)} por hora"
                    : $"un salario mensual de RD$ {empleado.Salario.ToString("N2", Cultura)}";
                c.Item().Text(
                    $"Por medio de la presente certificamos que {empleado.Nombre} {empleado.Apellido}, " +
                    $"portador(a) {(empleado.TipoDocumento == TipoDocumento.Cedula ? "de la cédula" : "del pasaporte")} {empleado.NumeroDocumento ?? "—"}, " +
                    (actual
                        ? $"labora en esta empresa desde el {empleado.FechaContratacion.ToString("d 'de' MMMM 'de' yyyy", Cultura)}"
                        : $"laboró en esta empresa desde el {empleado.FechaContratacion.ToString("d 'de' MMMM 'de' yyyy", Cultura)} hasta el {empleado.FechaTerminacion?.ToString("d 'de' MMMM 'de' yyyy", Cultura)}") +
                    (empleado.PuestoNav != null ? $", {(actual ? "desempeñando" : "desempeñó")} el cargo de {empleado.PuestoNav.Nombre}" : "") +
                    (empleado.Departamento != null ? $" en el departamento de {empleado.Departamento.Nombre}" : "") +
                    $", con {salario}.");
                c.Item().Text("Esta certificación se expide a solicitud de la parte interesada, para los fines que estime convenientes.");
                c.Item().PaddingTop(60).Width(220).Column(f =>
                {
                    f.Item().LineHorizontal(0.5f);
                    f.Item().Text("Recursos Humanos").AlignCenter();
                    f.Item().Text(empresa.RazonSocial).FontSize(9).FontColor(Colors.Grey.Darken1).AlignCenter();
                });
            });

            pagina.Footer().AlignCenter().Text($"Emitida el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
        })).GeneratePdf();
}
