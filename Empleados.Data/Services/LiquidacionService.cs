using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Nomina;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Empleados.Data.Services;

// Liquidación de prestaciones laborales: se calcula en borrador y, al pagarla, el empleado sale de la empresa
public class LiquidacionService
{
    private const string CodigoPrestamo = "PRESTAMO";

    private readonly EmpleadosDbContext _context;
    private readonly ParametrosLegalesService _parametros;
    private readonly IncidenciaService _incidencias;
    private readonly IContextoUsuario _contexto;

    public LiquidacionService(EmpleadosDbContext context, ParametrosLegalesService parametros, IncidenciaService incidencias, IContextoUsuario contexto)
    {
        _context = context;
        _parametros = parametros;
        _incidencias = incidencias;
        _contexto = contexto;
    }

    public Task<List<Liquidacion>> ListarAsync() =>
        _context.Liquidaciones.Include(l => l.Empleado).OrderByDescending(l => l.FechaCreacion).ToListAsync();

    public Task<Liquidacion?> ObtenerAsync(int id) =>
        _context.Liquidaciones.Include(l => l.Empleado).FirstOrDefaultAsync(l => l.Id == id);

    public async Task<Resultado<Liquidacion>> CrearAsync(LiquidacionDto dto)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoId);
        if (empleado == null)
            return Fallo("El empleado no existe", nameof(dto.EmpleadoId));
        if (await _context.Liquidaciones.AnyAsync(l => l.EmpleadoId == empleado.Id))
            return Fallo("El empleado ya tiene una liquidación; elimine el borrador para recalcularla", nameof(dto.EmpleadoId));
        var salida = dto.FechaSalida.Date;
        if (salida < empleado.FechaContratacion.Date)
            return Fallo("La fecha de salida es anterior al ingreso", nameof(dto.FechaSalida));
        var divisor = await _parametros.ValorAsync(ClavesParametro.DivisorSalarioDiario, salida);
        var jornada = await _parametros.ValorAsync(ClavesParametro.HorasJornadaDiaria, salida);
        if (divisor is not > 0 || jornada is not > 0)
            return Fallo("Faltan parámetros legales vigentes a la fecha de salida (divisor del salario diario o jornada)", nameof(dto.FechaSalida));

        var advertencias = new List<string>();
        var porHora = empleado.TipoSalario == TipoSalario.PorHora;
        var mensualActual = porHora ? empleado.Salario * jornada.Value * divisor.Value : empleado.Salario;
        var registrado = await CicloNominaService.SalarioOrdinarioRegistradoAsync(_context, empleado.EmpresaId,
            new DateTime(salida.Year - 1, 1, 1), salida, empleado.Id);

        var (promedio, detallePromedio) = dto.SalarioPromedio is decimal manual
            ? (manual, "Salario promedio indicado a mano")
            : Promedio(empleado.FechaContratacion, salida, mensualActual, registrado);

        var vacaciones = dto.DiasVacacionesPendientes ?? Math.Max(0, (await _incidencias.SaldoVacacionesAsync(empleado.Id, salida))?.Disponibles ?? 0);

        // Navidad proporcional, salvo que el Salario de Navidad del año ya se haya pagado
        decimal ordinarioAnio = 0;
        if (await _context.Nominas.AnyAsync(n => n.EmpresaId == empleado.EmpresaId && n.Tipo == TipoNomina.SalarioNavidad && n.Anio == salida.Year &&
                                                 (n.Estado == EstadoNomina.Pagada || n.Estado == EstadoNomina.Cerrada) &&
                                                 n.Recibos.Any(r => r.EmpleadoId == empleado.Id)))
            advertencias.Add($"El Salario de Navidad {salida.Year} ya se pagó: no se incluye");
        else
            ordinarioAnio = PagosEspeciales.SalarioNavidad(salida.Year, empleado.FechaContratacion, empleado.Salario, porHora,
                registrado.Where(r => r.Key.Anio == salida.Year).ToDictionary(r => r.Key.Mes, r => r.Value), salida).TotalDevengado;

        var resultado = Prestaciones.Calcular(new EntradaPrestaciones(dto.Causa, empleado.FechaContratacion, salida, promedio,
            dto.PreavisoOtorgado, vacaciones, ordinarioAnio, divisor.Value, dto.MontoAcordado));

        var lineas = resultado.Lineas.Select(l => new LineaLiquidacion(true, l.Codigo, l.Concepto, l.Dias, l.Monto)).ToList();
        var ingresos = lineas.Sum(l => l.Monto);

        // Saldos de préstamos: se descuentan hasta donde alcance
        var disponible = ingresos;
        foreach (var prestamo in await _context.Prestamos.Where(p => p.EmpleadoId == empleado.Id && p.Activo && p.Saldo > 0).OrderBy(p => p.Prioridad).ToListAsync())
        {
            var monto = Math.Min(prestamo.Saldo, disponible);
            if (monto < prestamo.Saldo)
                advertencias.Add($"{prestamo.Concepto}: quedan {(prestamo.Saldo - monto).ToString("N2", CultureInfo.GetCultureInfo("es-DO"))} sin descontar");
            if (monto <= 0)
                continue;
            lineas.Add(new LineaLiquidacion(false, CodigoPrestamo, $"Saldo de {prestamo.Concepto}", 0, monto, prestamo.Id));
            disponible -= monto;
        }

        var explicacion = resultado.Explicacion.ToList();
        explicacion.Insert(1, detallePromedio);
        var liquidacion = new Liquidacion
        {
            EmpresaId = empleado.EmpresaId,
            EmpleadoId = empleado.Id,
            Causa = dto.Causa,
            FechaIngreso = empleado.FechaContratacion.Date,
            FechaSalida = salida,
            PreavisoOtorgado = dto.PreavisoOtorgado,
            SalarioPromedio = promedio,
            SalarioDiario = resultado.SalarioDiario,
            TiempoServicio = resultado.TiempoServicio,
            MontoAcordado = dto.MontoAcordado,
            TotalIngresos = ingresos,
            TotalDeducciones = ingresos - disponible,
            Neto = disponible,
            LineasJson = JsonSerializer.Serialize(lineas, OpcionesJson),
            ExplicacionJson = JsonSerializer.Serialize(explicacion, OpcionesJson),
            AdvertenciasJson = advertencias.Count > 0 ? JsonSerializer.Serialize(advertencias, OpcionesJson) : null,
            Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim(),
            Estado = EstadoLiquidacion.Borrador,
            CreadaPor = _contexto.UsuarioNombre,
            FechaCreacion = DateTime.Now
        };
        _context.Liquidaciones.Add(liquidacion);
        await _context.SaveChangesAsync();
        return Resultado<Liquidacion>.Con(liquidacion, "Liquidación calculada (borrador)");
    }

    // Promedio del salario ordinario de los últimos 12 meses completos antes del mes de salida;
    // los meses sin nómina completa se toman con el salario actual
    private static (decimal Promedio, string Detalle) Promedio(DateTime ingreso, DateTime salida, decimal mensualActual,
        IReadOnlyDictionary<(int EmpleadoId, int Anio, int Mes), decimal> registrado)
    {
        var meses = new List<decimal>();
        var deNomina = 0;
        for (var mes = new DateTime(salida.Year, salida.Month, 1).AddMonths(-12); mes < new DateTime(salida.Year, salida.Month, 1); mes = mes.AddMonths(1))
        {
            if (ingreso.Date > mes)
                continue; // mes incompleto o anterior al ingreso
            var valor = registrado.FirstOrDefault(r => r.Key.Anio == mes.Year && r.Key.Mes == mes.Month);
            if (valor.Key != default)
            {
                meses.Add(valor.Value);
                deNomina++;
            }
            else
                meses.Add(mensualActual);
        }
        if (meses.Count == 0)
            return (mensualActual, "Salario promedio: menos de un mes completo, se usa el salario actual");
        var promedio = Math.Round(meses.Average(), 2, MidpointRounding.AwayFromZero);
        return (promedio, $"Salario promedio de {meses.Count} mes(es) ({deNomina} de nómina, {meses.Count - deNomina} con el salario actual): {promedio.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))}");
    }

    public async Task<Resultado> EliminarAsync(int id)
    {
        var liquidacion = await _context.Liquidaciones.FirstOrDefaultAsync(l => l.Id == id);
        if (liquidacion == null)
            return Resultado.NoEncontrado("Liquidación no encontrada");
        if (liquidacion.Estado != EstadoLiquidacion.Borrador)
            return Resultado.Invalido("Una liquidación pagada no se puede eliminar");

        _context.Liquidaciones.Remove(liquidacion);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Liquidación eliminada");
    }

    // Marca el pago: el empleado queda fuera de la empresa en la fecha de salida y sus préstamos se saldan
    public async Task<Resultado> PagarAsync(int id, DateTime fechaPago)
    {
        var liquidacion = await _context.Liquidaciones.Include(l => l.Empleado).FirstOrDefaultAsync(l => l.Id == id);
        if (liquidacion == null)
            return Resultado.NoEncontrado("Liquidación no encontrada");
        if (liquidacion.Estado != EstadoLiquidacion.Borrador)
            return Resultado.Invalido("La liquidación ya fue pagada");

        var empleado = liquidacion.Empleado!;
        if (empleado.Estado != EstadoEmpleado.Despedido)
        {
            _context.MovimientosEmpleados.Add(new MovimientoEmpleado
            {
                EmpresaId = empleado.EmpresaId,
                EmpleadoId = empleado.Id,
                Fecha = DateTime.Now,
                Tipo = TipoMovimiento.Despido,
                ValorAnterior = empleado.Estado.ToString(),
                ValorNuevo = $"Terminación: {Texto(liquidacion.Causa)}",
                UsuarioId = _contexto.UsuarioId,
                UsuarioNombre = _contexto.UsuarioNombre
            });
            empleado.Estado = EstadoEmpleado.Despedido;
        }
        empleado.FechaTerminacion = liquidacion.FechaSalida;

        var descontado = Lineas(liquidacion.LineasJson).Where(l => l.PrestamoId != null).ToDictionary(l => l.PrestamoId!.Value, l => l.Monto);
        foreach (var prestamo in await _context.Prestamos.Where(p => descontado.Keys.Contains(p.Id)).ToListAsync())
        {
            prestamo.Saldo = Math.Max(0, prestamo.Saldo - descontado[prestamo.Id]);
            if (prestamo.Saldo == 0)
                prestamo.Activo = false;
        }

        liquidacion.Estado = EstadoLiquidacion.Pagada;
        liquidacion.FechaPago = fechaPago.Date;
        liquidacion.PagadaPor = _contexto.UsuarioNombre;
        await _context.SaveChangesAsync();
        return Resultado.Exito($"Liquidación pagada; {empleado.Nombre} {empleado.Apellido} queda fuera de la empresa desde el {liquidacion.FechaSalida:dd/MM/yyyy}");
    }

    public static List<LineaLiquidacion> Lineas(string json) =>
        JsonSerializer.Deserialize<List<LineaLiquidacion>>(json) ?? new List<LineaLiquidacion>();

    public static string Texto(CausaTerminacion causa) => Textos.De(causa);

    private static readonly JsonSerializerOptions OpcionesJson = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    private static Resultado<Liquidacion> Fallo(string mensaje, string campo) =>
        Resultado<Liquidacion>.Fallo(Resultado.Invalido(mensaje, campo));
}
