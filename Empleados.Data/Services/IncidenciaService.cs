using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;
using CalendarioLaboral = Empleados.Nomina.CalendarioLaboral;
using DerechoVacaciones = Empleados.Nomina.DerechoVacaciones;

namespace Empleados.Data.Services;

public record SaldoVacaciones(DerechoVacaciones Derecho, decimal Tomados, decimal PorAprobar)
{
    public decimal Disponibles => Derecho.Dias - Tomados - PorAprobar;
}

// Faltas, retardos, permisos, licencias y vacaciones: registro, aprobación y saldo de vacaciones
public class IncidenciaService
{
    private readonly EmpleadosDbContext _context;
    private readonly ParametrosLegalesService _parametros;
    private readonly IContextoUsuario _contexto;

    public IncidenciaService(EmpleadosDbContext context, ParametrosLegalesService parametros, IContextoUsuario contexto)
    {
        _context = context;
        _parametros = parametros;
        _contexto = contexto;
    }

    public Task<List<Incidencia>> ListarAsync(int? empleadoId = null, EstadoIncidencia? estado = null, DateTime? desde = null, DateTime? hasta = null) =>
        _context.Incidencias
            .Include(i => i.Empleado)
            .Where(i => (empleadoId == null || i.EmpleadoId == empleadoId) && (estado == null || i.Estado == estado) &&
                        (desde == null || i.FechaFin >= desde) && (hasta == null || i.FechaInicio <= hasta))
            .OrderByDescending(i => i.FechaInicio).ThenBy(i => i.EmpleadoId)
            .ToListAsync();

    public async Task<SaldoVacaciones?> SaldoVacacionesAsync(int empleadoId, DateTime? fecha = null)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == empleadoId);
        return empleado == null ? null : await SaldoAsync(empleado, (fecha ?? DateTime.Today).Date);
    }

    // Derecho del año de servicio en curso menos lo tomado o solicitado dentro de ese año
    private async Task<SaldoVacaciones> SaldoAsync(Empleado empleado, DateTime fecha)
    {
        var derecho = CalendarioLaboral.Vacaciones(empleado.FechaContratacion, fecha,
            (int)(await _parametros.ValorAsync(ClavesParametro.VacacionesHasta5Anios, fecha) ?? 14),
            (int)(await _parametros.ValorAsync(ClavesParametro.VacacionesMas5Anios, fecha) ?? 18));
        var delAnio = await _context.Incidencias
            .Where(i => i.EmpleadoId == empleado.Id && i.Tipo == TipoIncidencia.Vacaciones && i.Estado != EstadoIncidencia.Rechazada &&
                        i.FechaInicio >= derecho.Desde && i.FechaInicio <= derecho.Hasta)
            .Select(i => new { i.Estado, i.Dias })
            .ToListAsync();
        return new SaldoVacaciones(derecho,
            delAnio.Where(i => i.Estado == EstadoIncidencia.Aprobada).Sum(i => i.Dias),
            delAnio.Where(i => i.Estado == EstadoIncidencia.Pendiente).Sum(i => i.Dias));
    }

    public async Task<Resultado<Incidencia>> CrearAsync(IncidenciaDto dto)
    {
        // Con el filtro de empresa, un empleado de otra empresa "no existe"
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoId);
        if (empleado == null)
            return Fallo("El empleado no existe", nameof(dto.EmpleadoId));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Fallo("No se pueden registrar incidencias a un empleado despedido", nameof(dto.EmpleadoId));

        var resultado = await PrepararAsync(empleado, dto);
        if (!resultado.Ok)
            return resultado;
        _context.Incidencias.Add(resultado.Valor!);
        await _context.SaveChangesAsync();
        return Resultado<Incidencia>.Con(resultado.Valor!, "Incidencia registrada; queda por aprobar");
    }

    private async Task<Resultado<Incidencia>> PrepararAsync(Empleado empleado, IncidenciaDto dto)
    {
        var inicio = dto.FechaInicio.Date;
        var fin = dto.Tipo == TipoIncidencia.Retardo ? inicio : dto.FechaFin.Date;
        if (fin < inicio)
            return Fallo("La fecha final no puede ser anterior a la inicial", nameof(dto.FechaFin));
        if (inicio < empleado.FechaContratacion.Date)
            return Fallo("La fecha es anterior al ingreso del empleado", nameof(dto.FechaInicio));
        if (dto.Tipo == TipoIncidencia.Retardo && dto.Horas <= 0)
            return Fallo("Indique las horas de retardo", nameof(dto.Horas));

        decimal dias = 0;
        if (dto.Tipo != TipoIncidencia.Retardo)
        {
            dias = dto.Dias ?? CalendarioLaboral.DiasLaborables(inicio, fin, await FeriadosAsync(inicio, fin));
            if (dias <= 0)
                return Fallo("No hay días laborables entre esas fechas", nameof(dto.FechaFin));
            if (dias > (fin - inicio).Days + 1)
                return Fallo("Los días no pueden ser más que los días entre las fechas", nameof(dto.Dias));
        }

        if (await _context.Incidencias.AnyAsync(i => i.EmpleadoId == empleado.Id && i.Estado != EstadoIncidencia.Rechazada &&
                                                    i.FechaInicio <= fin && i.FechaFin >= inicio))
            return Fallo("El empleado ya tiene una incidencia en esas fechas", nameof(dto.FechaInicio));

        if (dto.Tipo == TipoIncidencia.Vacaciones)
        {
            var saldo = await SaldoAsync(empleado, inicio);
            if (saldo.Derecho.AniosServicio == 0)
                return Fallo("Aún no cumple un año de servicio: no tiene vacaciones ganadas", nameof(dto.FechaInicio));
            if (dias > saldo.Disponibles)
                return Fallo($"Solo le quedan {saldo.Disponibles:0.##} días de vacaciones en su año de servicio n.º {saldo.Derecho.AniosServicio}", nameof(dto.Dias));
        }

        return Resultado<Incidencia>.Con(new Incidencia
        {
            EmpresaId = empleado.EmpresaId,
            EmpleadoId = empleado.Id,
            Tipo = dto.Tipo,
            FechaInicio = inicio,
            FechaFin = fin,
            Dias = dias,
            Horas = dto.Tipo == TipoIncidencia.Retardo ? dto.Horas : 0,
            Descontar = dto.Descontar ?? Incidencia.DescuentaPorDefecto(dto.Tipo),
            Estado = EstadoIncidencia.Pendiente,
            Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? null : dto.Comentario.Trim(),
            CreadaPor = _contexto.UsuarioNombre,
            FechaCreacion = DateTime.Now
        });
    }

    public async Task<Resultado> AprobarAsync(int id)
    {
        var incidencia = await _context.Incidencias.FirstOrDefaultAsync(i => i.Id == id);
        if (incidencia == null)
            return Resultado.NoEncontrado("Incidencia no encontrada");
        if (incidencia.Estado != EstadoIncidencia.Pendiente)
            return Resultado.Invalido("Solo se puede aprobar una incidencia por aprobar");

        incidencia.Estado = EstadoIncidencia.Aprobada;
        Revisada(incidencia);
        await _context.SaveChangesAsync();

        // shortcut: no se aplica sola a una nómina ya aprobada; si hace falta, el ajuste va como novedad en la siguiente
        var nomina = incidencia.Descontar ? await NominaNoEditableAsync(incidencia) : null;
        return Resultado.Exito(nomina == null
            ? "Incidencia aprobada"
            : $"Incidencia aprobada, pero la nómina {nomina} ya no se puede recalcular: registre el descuento como novedad en la próxima");
    }

    public async Task<Resultado> RechazarAsync(int id, string? motivo)
    {
        var incidencia = await _context.Incidencias.FirstOrDefaultAsync(i => i.Id == id);
        if (incidencia == null)
            return Resultado.NoEncontrado("Incidencia no encontrada");
        if (incidencia.Estado != EstadoIncidencia.Pendiente)
            return Resultado.Invalido("Solo se puede rechazar una incidencia por aprobar");

        incidencia.Estado = EstadoIncidencia.Rechazada;
        incidencia.MotivoRechazo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Revisada(incidencia);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Incidencia rechazada");
    }

    public async Task<Resultado> EliminarAsync(int id)
    {
        var incidencia = await _context.Incidencias.FirstOrDefaultAsync(i => i.Id == id);
        if (incidencia == null)
            return Resultado.NoEncontrado("Incidencia no encontrada");
        if (incidencia.Estado == EstadoIncidencia.Aprobada && incidencia.Descontar && await NominaNoEditableAsync(incidencia) is string nomina)
            return Resultado.Invalido($"No se puede eliminar: ya se descontó en la nómina {nomina}");

        _context.Incidencias.Remove(incidencia);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Incidencia eliminada");
    }

    // Art. 186: vacaciones de toda la empresa o de un departamento a la vez; se registran ya aprobadas
    public async Task<Resultado> VacacionesColectivasAsync(VacacionesColectivasDto dto)
    {
        var empleados = await _context.Empleados
            .Where(e => e.Estado == EstadoEmpleado.Activo && (dto.DepartamentoId == null || e.DepartamentoId == dto.DepartamentoId))
            .OrderBy(e => e.Nombre).ThenBy(e => e.Apellido)
            .ToListAsync();
        if (empleados.Count == 0)
            return Resultado.Invalido("No hay empleados activos para esas vacaciones");

        var omitidos = new List<string>();
        foreach (var empleado in empleados)
        {
            var resultado = await PrepararAsync(empleado, new IncidenciaDto
            {
                EmpleadoId = empleado.Id,
                Tipo = TipoIncidencia.Vacaciones,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Comentario = string.IsNullOrWhiteSpace(dto.Comentario) ? "Vacaciones colectivas" : dto.Comentario
            });
            if (!resultado.Ok)
            {
                omitidos.Add($"{empleado.Nombre} {empleado.Apellido} ({resultado.Mensaje})");
                continue;
            }
            resultado.Valor!.Estado = EstadoIncidencia.Aprobada;
            Revisada(resultado.Valor);
            _context.Incidencias.Add(resultado.Valor);
            // Guardar uno a uno para que el siguiente vea las fechas ya ocupadas
            await _context.SaveChangesAsync();
        }

        var registradas = empleados.Count - omitidos.Count;
        if (registradas == 0)
            return Resultado.Invalido("No se registró ninguna: " + string.Join("; ", omitidos));
        return Resultado.Exito(omitidos.Count == 0
            ? $"Vacaciones colectivas registradas para {registradas} empleado(s)"
            : $"Vacaciones registradas para {registradas} empleado(s). Sin registrar: {string.Join("; ", omitidos)}");
    }

    // Días que una incidencia descuenta dentro del período; si cruza de período se reparte por días laborables
    internal static decimal DiasEnPeriodo(Incidencia i, DateTime inicio, DateTime fin, IReadOnlyCollection<DateTime> feriados, decimal horasJornada)
    {
        if (i.Tipo == TipoIncidencia.Retardo)
            return horasJornada > 0 ? Math.Round(i.Horas / horasJornada, 4) : 0;
        if (i.FechaInicio >= inicio && i.FechaFin <= fin)
            return i.Dias;
        var total = CalendarioLaboral.DiasLaborables(i.FechaInicio, i.FechaFin, feriados);
        var dentro = CalendarioLaboral.DiasLaborables(i.FechaInicio > inicio ? i.FechaInicio : inicio, i.FechaFin < fin ? i.FechaFin : fin, feriados);
        return total == 0 ? 0 : Math.Round(i.Dias * dentro / total, 2);
    }

    private Task<List<DateTime>> FeriadosAsync(DateTime desde, DateTime hasta) =>
        _context.Feriados.Where(f => f.Fecha >= desde && f.Fecha <= hasta).Select(f => f.Fecha).ToListAsync();

    // Nómina aprobada, pagada o cerrada que cubre las fechas de la incidencia
    private async Task<string?> NominaNoEditableAsync(Incidencia incidencia)
    {
        var frecuencia = await _context.Empleados.Where(e => e.Id == incidencia.EmpleadoId).Select(e => e.FrecuenciaPago).FirstAsync();
        var nomina = await _context.Nominas.FirstOrDefaultAsync(n =>
            n.EmpresaId == incidencia.EmpresaId && n.Frecuencia == frecuencia &&
            n.Estado != EstadoNomina.Borrador && n.Estado != EstadoNomina.Calculada &&
            n.FechaInicio <= incidencia.FechaFin && n.FechaFin >= incidencia.FechaInicio);
        return nomina?.Descripcion;
    }

    private void Revisada(Incidencia incidencia)
    {
        incidencia.RevisadaPor = _contexto.UsuarioNombre;
        incidencia.FechaRevision = DateTime.Now;
    }

    private static Resultado<Incidencia> Fallo(string mensaje, string campo) =>
        Resultado<Incidencia>.Fallo(Resultado.Invalido(mensaje, campo));
}
