using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;

namespace Empleados.Data.Services;

public enum TipoReporte
{
    [System.ComponentModel.DataAnnotations.Display(Name = "Nómina y costo por departamento")] CostoPorDepartamento,
    [System.ComponentModel.DataAnnotations.Display(Name = "Acumulados anuales por empleado")] AcumuladosAnuales,
    [System.ComponentModel.DataAnnotations.Display(Name = "Ausentismo")] Ausentismo,
    [System.ComponentModel.DataAnnotations.Display(Name = "Rotación de personal")] Rotacion,
    [System.ComponentModel.DataAnnotations.Display(Name = "Vacaciones pendientes")] VacacionesPendientes
}

// Columna numérica (se suma en el total y se alinea a la derecha, con su formato) o de texto
public record ColumnaReporte(string Titulo, bool Numerica = false, string Formato = "N2");

public record TablaReporte(string Titulo, string Subtitulo, List<ColumnaReporte> Columnas, List<object?[]> Filas)
{
    // Suma de las columnas numéricas; vacío si no hay ninguna
    public object?[]? Totales => Columnas.Any(c => c.Numerica) && Filas.Count > 0
        ? Columnas.Select((c, i) => i == 0 ? "Total" : c.Numerica ? Filas.Sum(f => Convert.ToDecimal(f[i] ?? 0m)) : (object?)null).ToArray()
        : null;
}

// Reportes de nómina y personal sobre las nóminas aprobadas (las editables pueden cambiar todavía)
public class ReportesNominaService
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private readonly EmpleadosDbContext _context;
    private readonly IncidenciaService _incidencias;

    public ReportesNominaService(EmpleadosDbContext context, IncidenciaService incidencias)
    {
        _context = context;
        _incidencias = incidencias;
    }

    public Task<TablaReporte> GenerarAsync(TipoReporte tipo, int anio, int? mes) => tipo switch
    {
        TipoReporte.CostoPorDepartamento => CostoPorDepartamentoAsync(anio, mes),
        TipoReporte.AcumuladosAnuales => AcumuladosAnualesAsync(anio),
        TipoReporte.Ausentismo => AusentismoAsync(anio, mes),
        TipoReporte.Rotacion => RotacionAsync(anio),
        _ => VacacionesPendientesAsync()
    };

    private Task<List<ReciboPago>> RecibosAprobadosAsync(int anio, int? mes, bool soloRegulares) =>
        _context.RecibosPago.Include(r => r.Lineas).Include(r => r.Nomina)
            .Where(r => r.Nomina!.Anio == anio && (mes == null || r.Nomina.Mes == mes) &&
                        r.Nomina.Estado != EstadoNomina.Borrador && r.Nomina.Estado != EstadoNomina.Calculada &&
                        (!soloRegulares || r.Nomina.Tipo == TipoNomina.Regular))
            .AsSplitQuery()
            .ToListAsync();

    private static string Periodo(int anio, int? mes) =>
        mes == null ? $"Año {anio}" : new DateTime(anio, mes.Value, 1).ToString("MMMM yyyy", Cultura);

    public async Task<TablaReporte> CostoPorDepartamentoAsync(int anio, int? mes)
    {
        var recibos = await RecibosAprobadosAsync(anio, mes, soloRegulares: false);
        var filas = recibos.GroupBy(r => r.Departamento ?? "Sin departamento").OrderBy(g => g.Key)
            .Select(g => new object?[]
            {
                g.Key,
                (decimal)g.Select(r => r.EmpleadoId).Distinct().Count(),
                g.Sum(r => r.TotalIngresos),
                g.Sum(r => r.TotalDeduccionesLey),
                g.Sum(r => r.TotalDeduccionesInternas),
                g.Sum(r => r.Neto),
                g.Sum(r => r.TotalAportesPatronales),
                g.Sum(r => r.TotalIngresos + r.TotalAportesPatronales)
            }).ToList();
        return new TablaReporte("Nómina y costo por departamento", $"{Periodo(anio, mes)} · nóminas aprobadas, incluye pagos especiales",
            new() { new("Departamento"), new("Empleados", true, "0"), new("Ingresos", true), new("Deducciones de ley", true), new("Otras deducciones", true),
                    new("Neto pagado", true), new("Aportes del empleador", true), new("Costo total", true) },
            filas);
    }

    public async Task<TablaReporte> AcumuladosAnualesAsync(int anio)
    {
        var recibos = await RecibosAprobadosAsync(anio, null, soloRegulares: false);
        decimal Suma(IEnumerable<ReciboPago> rs, params string[] codigos) => rs.SelectMany(r => r.Lineas).Where(l => codigos.Contains(l.Codigo)).Sum(l => l.Monto);
        var filas = recibos.GroupBy(r => new { r.EmpleadoId, r.NombreEmpleado, r.Documento }).OrderBy(g => g.Key.NombreEmpleado)
            .Select(g =>
            {
                var regulares = g.Where(r => r.Nomina!.Tipo == TipoNomina.Regular).ToList();
                return new object?[]
                {
                    g.Key.NombreEmpleado,
                    g.Key.Documento,
                    regulares.Sum(r => r.BaseTss),
                    regulares.Sum(r => r.TotalIngresos),
                    Suma(regulares, Codigos.Afp),
                    Suma(regulares, Codigos.Sfs, Codigos.SfsDependientes),
                    Suma(regulares, Codigos.Isr),
                    g.Where(r => r.Nomina!.Tipo == TipoNomina.SalarioNavidad).Sum(r => r.Neto),
                    g.Sum(r => r.Neto),
                    g.Sum(r => r.TotalAportesPatronales)
                };
            }).ToList();
        return new TablaReporte("Acumulados anuales por empleado", $"Año {anio} · nóminas aprobadas (para la declaración y la Navidad)",
            new() { new("Empleado"), new("Documento"), new("Salario cotizable", true), new("Ingresos", true), new("AFP", true), new("SFS", true),
                    new("ISR retenido", true), new("Salario de Navidad", true), new("Neto pagado", true), new("Aportes del empleador", true) },
            filas);
    }

    public async Task<TablaReporte> AusentismoAsync(int anio, int? mes)
    {
        var desde = new DateTime(anio, mes ?? 1, 1);
        var hasta = mes == null ? new DateTime(anio, 12, 31) : desde.AddMonths(1).AddDays(-1);
        var incidencias = await _incidencias.ListarAsync(estado: EstadoIncidencia.Aprobada, desde: desde, hasta: hasta);
        decimal Dias(IEnumerable<Incidencia> i, params TipoIncidencia[] tipos) => i.Where(x => tipos.Contains(x.Tipo)).Sum(x => x.Dias);
        var filas = incidencias.GroupBy(i => $"{i.Empleado?.Nombre} {i.Empleado?.Apellido}").OrderBy(g => g.Key)
            .Select(g => new object?[]
            {
                g.Key,
                Dias(g, TipoIncidencia.Falta),
                g.Where(i => i.Tipo == TipoIncidencia.Retardo).Sum(i => i.Horas),
                Dias(g, TipoIncidencia.PermisoConGoce, TipoIncidencia.PermisoSinGoce),
                Dias(g, TipoIncidencia.LicenciaMedica, TipoIncidencia.LicenciaMaternidad, TipoIncidencia.LicenciaPaternidad, TipoIncidencia.LicenciaLuto, TipoIncidencia.LicenciaMatrimonio),
                Dias(g, TipoIncidencia.Vacaciones),
                g.Where(i => i.Descontar && i.Tipo != TipoIncidencia.Retardo).Sum(i => i.Dias)
            }).ToList();
        return new TablaReporte("Ausentismo", $"{Periodo(anio, mes)} · incidencias aprobadas que empiezan o terminan en el período",
            new() { new("Empleado"), new("Faltas (días)", true, "0.##"), new("Retardos (horas)", true, "0.##"), new("Permisos (días)", true, "0.##"),
                    new("Licencias (días)", true, "0.##"), new("Vacaciones (días)", true, "0.##"), new("Días descontados", true, "0.##") },
            filas);
    }

    public async Task<TablaReporte> RotacionAsync(int anio)
    {
        var empleados = await _context.Empleados.Select(e => new { e.FechaContratacion, e.FechaTerminacion, e.Estado }).ToListAsync();
        var filas = Enumerable.Range(1, 12).Select(mes =>
        {
            var inicio = new DateTime(anio, mes, 1);
            var fin = inicio.AddMonths(1).AddDays(-1);
            bool Activo(DateTime dia, DateTime ingreso, DateTime? salida) => ingreso.Date <= dia && (salida == null || salida.Value.Date >= dia);
            var alInicio = empleados.Count(e => Activo(inicio, e.FechaContratacion, e.FechaTerminacion));
            var alFinal = empleados.Count(e => Activo(fin, e.FechaContratacion, e.FechaTerminacion));
            var altas = empleados.Count(e => e.FechaContratacion.Date >= inicio && e.FechaContratacion.Date <= fin);
            var bajas = empleados.Count(e => e.FechaTerminacion?.Date >= inicio && e.FechaTerminacion?.Date <= fin);
            var promedio = (alInicio + alFinal) / 2m;
            return new object?[]
            {
                inicio.ToString("MMMM", Cultura), (decimal)alInicio, (decimal)altas, (decimal)bajas, (decimal)alFinal,
                promedio == 0 ? "—" : $"{Math.Round(bajas / promedio * 100, 1).ToString("0.#", Cultura)} %"
            };
        }).ToList();
        // shortcut: las desactivaciones sin fecha de terminación no cuentan como bajas
        return new TablaReporte("Rotación de personal", $"Año {anio} · rotación = bajas / promedio de empleados del mes",
            new() { new("Mes"), new("Al inicio", Formato: "0"), new("Altas", true, "0"), new("Bajas", true, "0"), new("Al final", Formato: "0"), new("Rotación") },
            filas);
    }

    public async Task<TablaReporte> VacacionesPendientesAsync()
    {
        var hoy = DateTime.Today;
        var empleados = await _context.Empleados.Where(e => e.Estado == EstadoEmpleado.Activo).OrderBy(e => e.Nombre).ThenBy(e => e.Apellido).ToListAsync();
        var filas = new List<object?[]>();
        foreach (var e in empleados)
        {
            var saldo = await _incidencias.SaldoVacacionesAsync(e.Id, hoy);
            if (saldo == null || saldo.Derecho.AniosServicio == 0)
                continue;
            filas.Add(new object?[]
            {
                $"{e.Nombre} {e.Apellido}", saldo.Derecho.AniosServicio.ToString(), (decimal)saldo.Derecho.Dias, saldo.Tomados, saldo.PorAprobar, saldo.Disponibles,
                saldo.Derecho.Hasta.ToString("dd/MM/yyyy")
            });
        }
        return new TablaReporte("Vacaciones pendientes", $"Al {hoy:dd/MM/yyyy} · empleados activos con al menos un año de servicio",
            new() { new("Empleado"), new("Año de servicio"), new("Derecho", true, "0.##"), new("Tomados", true, "0.##"), new("Por aprobar", true, "0.##"),
                    new("Disponibles", true, "0.##"), new("Vence el") },
            filas.OrderByDescending(f => (decimal)f[5]!).ToList());
    }

    public static string Celda(object? valor, ColumnaReporte columna) =>
        valor is decimal d ? d.ToString(columna.Formato, Cultura) : valor?.ToString() ?? "";

    public static byte[] Csv(TablaReporte tabla)
    {
        static string Campo(string valor) => valor.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
        static string Valor(object? v) => v is decimal d ? d.ToString("0.##", CultureInfo.InvariantCulture) : Campo(v?.ToString() ?? "");
        var texto = new StringBuilder();
        texto.AppendJoin(',', tabla.Columnas.Select(c => Campo(c.Titulo))).Append("\r\n");
        foreach (var fila in tabla.Filas)
            texto.AppendJoin(',', fila.Select(Valor)).Append("\r\n");
        if (tabla.Totales is { } totales)
            texto.AppendJoin(',', totales.Select(Valor)).Append("\r\n");
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texto.ToString())).ToArray();
    }

    public static byte[] Pdf(TablaReporte tabla, Empresa empresa)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(tabla.Columnas.Count > 6 ? PageSizes.Letter.Landscape() : PageSizes.Letter);
            pagina.Margin(30);
            pagina.DefaultTextStyle(t => t.FontSize(8.5f));
            pagina.Header().Column(c =>
            {
                c.Item().Text(empresa.RazonSocial).Bold().FontSize(12);
                c.Item().Text(tabla.Titulo).SemiBold().FontSize(11);
                c.Item().Text(tabla.Subtitulo).FontColor(Colors.Grey.Darken1);
                c.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
            });
            pagina.Content().PaddingTop(8).Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    foreach (var (col, i) in tabla.Columnas.Select((c, i) => (c, i)))
                        if (i == 0) c.RelativeColumn(2.2f); else c.RelativeColumn();
                });
                t.Header(h =>
                {
                    foreach (var col in tabla.Columnas)
                    {
                        var celda = h.Cell().Background(Colors.Grey.Lighten3).Padding(3);
                        (col.Numerica ? celda.AlignRight() : celda).Text(col.Titulo).Bold();
                    }
                });
                void Fila(object?[] fila, bool negrita)
                {
                    for (var i = 0; i < fila.Length; i++)
                    {
                        var celda = t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);
                        var texto = (fila[i] is decimal ? celda.AlignRight() : celda).Text(Celda(fila[i], tabla.Columnas[i]));
                        if (negrita)
                            texto.Bold();
                    }
                }
                foreach (var fila in tabla.Filas)
                    Fila(fila, false);
                if (tabla.Totales is { } totales)
                    Fila(totales, true);
            });
            pagina.Footer().AlignCenter().Text(x =>
            {
                x.DefaultTextStyle(s => s.FontSize(7).FontColor(Colors.Grey.Darken1));
                x.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm} · Página ");
                x.CurrentPageNumber();
                x.Span(" de ");
                x.TotalPages();
            });
        })).GeneratePdf();
    }
}
