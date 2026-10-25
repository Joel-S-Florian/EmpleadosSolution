using Empleados.Data.Data;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace Empleados.Data.Services;

public enum TipoReporte
{
    // Personal
    [Display(Name = "Plantilla de empleados")] PlantillaEmpleados,
    [Display(Name = "Empleados por departamento")] EmpleadosPorDepartamento,
    [Display(Name = "Cumpleaños")] Cumpleanos,
    [Display(Name = "Contratos por vencer")] ContratosPorVencer,
    // Nómina
    [Display(Name = "Nómina y costo por departamento")] CostoPorDepartamento,
    [Display(Name = "Detalle de una nómina")] DetalleNomina,
    [Display(Name = "Comparativo mensual")] ComparativoMensual,
    [Display(Name = "Acumulados anuales por empleado")] AcumuladosAnuales,
    [Display(Name = "Préstamos y saldos")] Prestamos,
    // Asistencia
    [Display(Name = "Ausentismo")] Ausentismo,
    [Display(Name = "Vacaciones pendientes")] VacacionesPendientes,
    [Display(Name = "Rotación de personal")] Rotacion
}

// Qué filtros usa cada reporte y dónde se muestra
public record InfoReporte(string Categoria, string Icono, string Descripcion, bool Anio = false, bool Mes = false, bool Departamento = true, bool Estado = false, bool Nomina = false);

public record FiltroReporte(int Anio, int? Mes = null, int? DepartamentoId = null, EstadoEmpleado? Estado = null, int? NominaId = null);

// Columna numérica (se suma en el total y se alinea a la derecha, con su formato) o de texto
public record ColumnaReporte(string Titulo, bool Numerica = false, string Formato = "N2");

public record TablaReporte(string Titulo, string Subtitulo, List<ColumnaReporte> Columnas, List<object?[]> Filas)
{
    // Suma de las columnas numéricas; vacío si no hay ninguna
    public object?[]? Totales => Columnas.Any(c => c.Numerica) && Filas.Count > 0
        ? Columnas.Select((c, i) => i == 0 ? "Total" : c.Numerica ? Filas.Sum(f => Convert.ToDecimal(f[i] ?? 0m)) : (object?)null).ToArray()
        : null;
}

// Reportes de personal, nómina y asistencia con exportación a Excel, CSV y PDF.
// Los de nómina usan solo las nóminas aprobadas (las editables todavía pueden cambiar).
public class GeneradorReportes
{
    public const string Personal = "Personal";
    public const string Nominas = "Nómina";
    public const string Asistencia = "Asistencia";

    public static readonly IReadOnlyDictionary<TipoReporte, InfoReporte> Catalogo = new Dictionary<TipoReporte, InfoReporte>
    {
        [TipoReporte.PlantillaEmpleados] = new(Personal, "bi-people", "Listado de empleados con puesto, salario, antigüedad y datos de pago", Estado: true),
        [TipoReporte.EmpleadosPorDepartamento] = new(Personal, "bi-diagram-3", "Cantidad de empleados y nómina mensual por departamento", Departamento: false),
        [TipoReporte.Cumpleanos] = new(Personal, "bi-cake2", "Cumpleaños del mes", Mes: true),
        [TipoReporte.ContratosPorVencer] = new(Personal, "bi-hourglass-split", "Contratos por tiempo definido que terminan en los próximos 90 días"),
        [TipoReporte.CostoPorDepartamento] = new(Nominas, "bi-building", "Ingresos, deducciones, neto, aportes del empleador y costo total", Anio: true, Mes: true),
        [TipoReporte.DetalleNomina] = new(Nominas, "bi-receipt", "Cada recibo de una nómina con sus totales", Nomina: true),
        [TipoReporte.ComparativoMensual] = new(Nominas, "bi-graph-up", "Totales de cada mes del año para comparar", Anio: true),
        [TipoReporte.AcumuladosAnuales] = new(Nominas, "bi-calendar3", "Salario, retenciones y Navidad del año por empleado", Anio: true),
        [TipoReporte.Prestamos] = new(Nominas, "bi-piggy-bank", "Préstamos con su cuota, lo pagado y el saldo"),
        [TipoReporte.Ausentismo] = new(Asistencia, "bi-calendar-x", "Faltas, retardos, permisos, licencias y vacaciones aprobadas", Anio: true, Mes: true),
        [TipoReporte.VacacionesPendientes] = new(Asistencia, "bi-sun", "Saldo de vacaciones del año de servicio en curso"),
        [TipoReporte.Rotacion] = new(Asistencia, "bi-arrow-left-right", "Altas, bajas y rotación de cada mes", Anio: true)
    };

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    private readonly EmpleadosDbContext _context;
    private readonly IncidenciaService _incidencias;

    public GeneradorReportes(EmpleadosDbContext context, IncidenciaService incidencias)
    {
        _context = context;
        _incidencias = incidencias;
    }

    public async Task<TablaReporte> GenerarAsync(TipoReporte tipo, FiltroReporte filtro)
    {
        var tabla = tipo switch
        {
            TipoReporte.PlantillaEmpleados => await PlantillaAsync(filtro),
            TipoReporte.EmpleadosPorDepartamento => await EmpleadosPorDepartamentoAsync(),
            TipoReporte.Cumpleanos => await CumpleanosAsync(filtro),
            TipoReporte.ContratosPorVencer => await ContratosPorVencerAsync(filtro),
            TipoReporte.CostoPorDepartamento => await CostoPorDepartamentoAsync(filtro),
            TipoReporte.DetalleNomina => await DetalleNominaAsync(filtro),
            TipoReporte.ComparativoMensual => await ComparativoMensualAsync(filtro),
            TipoReporte.AcumuladosAnuales => await AcumuladosAnualesAsync(filtro),
            TipoReporte.Prestamos => await PrestamosAsync(filtro),
            TipoReporte.Ausentismo => await AusentismoAsync(filtro),
            TipoReporte.Rotacion => await RotacionAsync(filtro),
            _ => await VacacionesPendientesAsync(filtro)
        };
        var departamento = filtro.DepartamentoId == null || !Catalogo[tipo].Departamento ? null
            : await _context.Departamentos.Where(d => d.Id == filtro.DepartamentoId).Select(d => d.Nombre).FirstOrDefaultAsync();
        return departamento == null ? tabla : tabla with { Subtitulo = $"{tabla.Subtitulo} · departamento {departamento}" };
    }

    private static string Periodo(int anio, int? mes) =>
        mes == null ? $"Año {anio}" : new DateTime(anio, mes.Value, 1).ToString("MMMM yyyy", Cultura);

    private static string Antiguedad(DateTime ingreso, DateTime hasta)
    {
        var (anios, meses, _) = Prestaciones.Tiempo(ingreso, hasta);
        return anios > 0 ? $"{anios} a {meses} m" : $"{meses} m";
    }

    private IQueryable<Empleado> Empleados(FiltroReporte f) =>
        _context.Empleados.Where(e => f.DepartamentoId == null || e.DepartamentoId == f.DepartamentoId);

    // Personal

    public async Task<TablaReporte> PlantillaAsync(FiltroReporte f)
    {
        var hoy = DateTime.Today;
        var empleados = await Empleados(f).Include(e => e.Departamento).Include(e => e.PuestoNav).Include(e => e.Banco)
            .Where(e => f.Estado == null ? e.Estado != EstadoEmpleado.Despedido : e.Estado == f.Estado)
            .OrderBy(e => e.Departamento!.Nombre).ThenBy(e => e.Apellido).ThenBy(e => e.Nombre)
            .ToListAsync();
        var filas = empleados.Select(e => new object?[]
        {
            $"{e.Apellido}, {e.Nombre}", e.NumeroDocumento ?? "—", e.Departamento?.Nombre, e.PuestoNav?.Nombre ?? "—",
            e.FechaContratacion.ToString("dd/MM/yyyy"), Antiguedad(e.FechaContratacion, e.FechaTerminacion ?? hoy),
            Textos.De(e.FrecuenciaPago), e.Salario, e.Banco == null ? "Cheque / efectivo" : e.Banco.Nombre, Textos.De(e.Estado)
        }).ToList();
        return new TablaReporte("Plantilla de empleados",
            f.Estado == null ? "Activos e inactivos" : $"Estado: {Textos.De(f.Estado.Value)}",
            new() { new("Empleado"), new("Documento"), new("Departamento"), new("Puesto"), new("Ingreso"), new("Antigüedad"),
                    new("Frecuencia"), new("Salario", true), new("Forma de pago"), new("Estado") },
            filas);
    }

    public async Task<TablaReporte> EmpleadosPorDepartamentoAsync()
    {
        var departamentos = await _context.Departamentos.OrderBy(d => d.Nombre).Select(d => new
        {
            d.Nombre,
            Total = d.Empleados.Count(e => e.Estado != EstadoEmpleado.Despedido),
            Activos = d.Empleados.Count(e => e.Estado == EstadoEmpleado.Activo),
            Nomina = d.Empleados.Where(e => e.Estado == EstadoEmpleado.Activo && e.TipoSalario == TipoSalario.FijoMensual).Sum(e => (decimal?)e.Salario) ?? 0
        }).ToListAsync();
        return new TablaReporte("Empleados por departamento", "Salarios fijos mensuales de los empleados activos",
            new() { new("Departamento"), new("Empleados", true, "0"), new("Activos", true, "0"), new("Nómina mensual", true), new("Salario promedio") },
            departamentos.Select(d => new object?[]
            {
                d.Nombre, (decimal)d.Total, (decimal)d.Activos, d.Nomina, d.Activos == 0 ? "—" : (d.Nomina / d.Activos).ToString("N2", Cultura)
            }).ToList());
    }

    public async Task<TablaReporte> CumpleanosAsync(FiltroReporte f)
    {
        var mes = f.Mes ?? DateTime.Today.Month;
        var empleados = await Empleados(f).Include(e => e.Departamento)
            .Where(e => e.Estado == EstadoEmpleado.Activo && e.FechaNacimiento.Month == mes)
            .ToListAsync();
        return new TablaReporte("Cumpleaños", new DateTime(2000, mes, 1).ToString("MMMM", Cultura),
            new() { new("Día", Formato: "0"), new("Empleado"), new("Departamento"), new("Cumple", Formato: "0") },
            empleados.OrderBy(e => e.FechaNacimiento.Day).Select(e => new object?[]
            {
                (decimal)e.FechaNacimiento.Day, $"{e.Nombre} {e.Apellido}", e.Departamento?.Nombre, (decimal)(DateTime.Today.Year - e.FechaNacimiento.Year)
            }).ToList());
    }

    public async Task<TablaReporte> ContratosPorVencerAsync(FiltroReporte f)
    {
        var hoy = DateTime.Today;
        var limite = hoy.AddDays(90);
        var empleados = await Empleados(f).Include(e => e.Departamento).Include(e => e.PuestoNav)
            .Where(e => e.Estado != EstadoEmpleado.Despedido && e.FechaFinContrato != null && e.FechaFinContrato <= limite)
            .OrderBy(e => e.FechaFinContrato)
            .ToListAsync();
        return new TablaReporte("Contratos por vencer", $"Del {hoy:dd/MM/yyyy} al {limite:dd/MM/yyyy} (incluye los ya vencidos)",
            new() { new("Empleado"), new("Departamento"), new("Puesto"), new("Contrato"), new("Vence"), new("Días restantes", Formato: "0") },
            empleados.Select(e => new object?[]
            {
                $"{e.Nombre} {e.Apellido}", e.Departamento?.Nombre, e.PuestoNav?.Nombre ?? "—", Textos.De(e.TipoContrato),
                e.FechaFinContrato!.Value.ToString("dd/MM/yyyy"), (decimal)(e.FechaFinContrato.Value.Date - hoy).Days
            }).ToList());
    }

    // Nómina

    private async Task<List<ReciboPago>> RecibosAprobadosAsync(FiltroReporte f, bool porMes = true)
    {
        var departamento = f.DepartamentoId == null ? null
            : await _context.Departamentos.Where(d => d.Id == f.DepartamentoId).Select(d => d.Nombre).FirstOrDefaultAsync();
        return await _context.RecibosPago.Include(r => r.Lineas).Include(r => r.Nomina)
            .Where(r => r.Nomina!.Anio == f.Anio && (!porMes || f.Mes == null || r.Nomina.Mes == f.Mes) &&
                        r.Nomina.Estado != EstadoNomina.Borrador && r.Nomina.Estado != EstadoNomina.Calculada &&
                        (departamento == null || r.Departamento == departamento))
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<TablaReporte> CostoPorDepartamentoAsync(FiltroReporte f)
    {
        var recibos = await RecibosAprobadosAsync(f);
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
        return new TablaReporte("Nómina y costo por departamento", $"{Periodo(f.Anio, f.Mes)} · nóminas aprobadas, incluye pagos especiales",
            new() { new("Departamento"), new("Empleados", true, "0"), new("Ingresos", true), new("Deducciones de ley", true), new("Otras deducciones", true),
                    new("Neto pagado", true), new("Aportes del empleador", true), new("Costo total", true) },
            filas);
    }

    public async Task<TablaReporte> DetalleNominaAsync(FiltroReporte f)
    {
        var nomina = f.NominaId == null ? null : await _context.Nominas.Include(n => n.Recibos).ThenInclude(r => r.Lineas)
            .AsSplitQuery().FirstOrDefaultAsync(n => n.Id == f.NominaId);
        var departamento = f.DepartamentoId == null ? null
            : await _context.Departamentos.Where(d => d.Id == f.DepartamentoId).Select(d => d.Nombre).FirstOrDefaultAsync();
        var recibos = nomina?.Recibos.Where(r => departamento == null || r.Departamento == departamento)
            .OrderBy(r => r.Departamento).ThenBy(r => r.NombreEmpleado).ToList() ?? new List<ReciboPago>();
        decimal Suma(ReciboPago r, params string[] codigos) => r.Lineas.Where(l => codigos.Contains(l.Codigo)).Sum(l => l.Monto);
        return new TablaReporte(nomina == null ? "Detalle de una nómina" : $"Nómina {nomina.Descripcion}",
            nomina == null ? "Elija una nómina" : $"Del {nomina.FechaInicio:dd/MM/yyyy} al {nomina.FechaFin:dd/MM/yyyy} · {Textos.De(nomina.Estado)}",
            new() { new("Empleado"), new("Departamento"), new("Ingresos", true), new("AFP", true), new("SFS", true), new("ISR", true),
                    new("Otras deducciones", true), new("Neto", true), new("Aportes del empleador", true) },
            recibos.Select(r => new object?[]
            {
                r.NombreEmpleado, r.Departamento, r.TotalIngresos, Suma(r, Codigos.Afp), Suma(r, Codigos.Sfs, Codigos.SfsDependientes),
                Suma(r, Codigos.Isr), r.TotalDeduccionesInternas, r.Neto, r.TotalAportesPatronales
            }).ToList());
    }

    public async Task<TablaReporte> ComparativoMensualAsync(FiltroReporte f)
    {
        var recibos = await RecibosAprobadosAsync(f, porMes: false);
        var filas = Enumerable.Range(1, 12).Select(mes =>
        {
            var delMes = recibos.Where(r => r.Nomina!.Mes == mes).ToList();
            return new object?[]
            {
                new DateTime(f.Anio, mes, 1).ToString("MMMM", Cultura),
                (decimal)delMes.Select(r => r.EmpleadoId).Distinct().Count(),
                delMes.Sum(r => r.TotalIngresos),
                delMes.Sum(r => r.TotalDeduccionesLey + r.TotalDeduccionesInternas),
                delMes.Sum(r => r.Neto),
                delMes.Sum(r => r.TotalAportesPatronales),
                delMes.Sum(r => r.TotalIngresos + r.TotalAportesPatronales)
            };
        }).ToList();
        return new TablaReporte("Comparativo mensual", $"Año {f.Anio} · nóminas aprobadas, incluye pagos especiales",
            new() { new("Mes"), new("Empleados", Formato: "0"), new("Ingresos", true), new("Deducciones", true), new("Neto", true),
                    new("Aportes del empleador", true), new("Costo total", true) },
            filas);
    }

    public async Task<TablaReporte> AcumuladosAnualesAsync(FiltroReporte f)
    {
        var recibos = await RecibosAprobadosAsync(f, porMes: false);
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
        return new TablaReporte("Acumulados anuales por empleado", $"Año {f.Anio} · nóminas aprobadas (para la declaración y la Navidad)",
            new() { new("Empleado"), new("Documento"), new("Salario cotizable", true), new("Ingresos", true), new("AFP", true), new("SFS", true),
                    new("ISR retenido", true), new("Salario de Navidad", true), new("Neto pagado", true), new("Aportes del empleador", true) },
            filas);
    }

    public async Task<TablaReporte> PrestamosAsync(FiltroReporte f)
    {
        var prestamos = await _context.Prestamos.Include(p => p.Empleado)
            .Where(p => f.DepartamentoId == null || p.Empleado!.DepartamentoId == f.DepartamentoId)
            .OrderByDescending(p => p.Activo).ThenBy(p => p.Empleado!.Nombre)
            .ToListAsync();
        return new TablaReporte("Préstamos y saldos", "El saldo baja cuando se paga cada nómina",
            new() { new("Empleado"), new("Concepto"), new("Desde"), new("Monto", true), new("Cuota", true), new("Pagado", true), new("Saldo", true), new("Estado") },
            prestamos.Select(p => new object?[]
            {
                $"{p.Empleado?.Nombre} {p.Empleado?.Apellido}", p.Concepto, p.FechaInicio.ToString("dd/MM/yyyy"), p.MontoTotal, p.MontoCuota,
                p.MontoTotal - p.Saldo, p.Saldo, p.Activo ? "Activo" : p.Saldo == 0 ? "Liquidado" : "Cancelado"
            }).ToList());
    }

    // Asistencia

    public async Task<TablaReporte> AusentismoAsync(FiltroReporte f)
    {
        var desde = new DateTime(f.Anio, f.Mes ?? 1, 1);
        var hasta = f.Mes == null ? new DateTime(f.Anio, 12, 31) : desde.AddMonths(1).AddDays(-1);
        var incidencias = (await _incidencias.ListarAsync(estado: EstadoIncidencia.Aprobada, desde: desde, hasta: hasta))
            .Where(i => f.DepartamentoId == null || i.Empleado?.DepartamentoId == f.DepartamentoId);
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
        return new TablaReporte("Ausentismo", $"{Periodo(f.Anio, f.Mes)} · incidencias aprobadas que tocan el período",
            new() { new("Empleado"), new("Faltas (días)", true, "0.##"), new("Retardos (horas)", true, "0.##"), new("Permisos (días)", true, "0.##"),
                    new("Licencias (días)", true, "0.##"), new("Vacaciones (días)", true, "0.##"), new("Días descontados", true, "0.##") },
            filas);
    }

    public async Task<TablaReporte> RotacionAsync(FiltroReporte f)
    {
        var empleados = await Empleados(f).Select(e => new { e.FechaContratacion, e.FechaTerminacion }).ToListAsync();
        var filas = Enumerable.Range(1, 12).Select(mes =>
        {
            var inicio = new DateTime(f.Anio, mes, 1);
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
        return new TablaReporte("Rotación de personal", $"Año {f.Anio} · rotación = bajas / promedio de empleados del mes",
            new() { new("Mes"), new("Al inicio", Formato: "0"), new("Altas", true, "0"), new("Bajas", true, "0"), new("Al final", Formato: "0"), new("Rotación") },
            filas);
    }

    public async Task<TablaReporte> VacacionesPendientesAsync(FiltroReporte f)
    {
        var hoy = DateTime.Today;
        var empleados = await Empleados(f).Where(e => e.Estado == EstadoEmpleado.Activo).OrderBy(e => e.Nombre).ThenBy(e => e.Apellido).ToListAsync();
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

    // Exportación

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
                    for (var i = 0; i < tabla.Columnas.Count; i++)
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
