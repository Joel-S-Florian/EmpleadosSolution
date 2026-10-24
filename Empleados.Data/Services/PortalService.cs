using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

// Portal del empleado: solo sus datos, sus recibos pagados, su saldo de vacaciones y sus solicitudes
public class PortalService
{
    // Lo que un empleado puede pedir; faltas y retardos los registra RRHH
    public static readonly TipoIncidencia[] Solicitables =
    {
        TipoIncidencia.Vacaciones, TipoIncidencia.PermisoConGoce, TipoIncidencia.PermisoSinGoce, TipoIncidencia.LicenciaMedica,
        TipoIncidencia.LicenciaMaternidad, TipoIncidencia.LicenciaPaternidad, TipoIncidencia.LicenciaLuto, TipoIncidencia.LicenciaMatrimonio
    };

    private readonly EmpleadosDbContext _context;
    private readonly IncidenciaService _incidencias;
    private readonly NotificacionService _notificaciones;
    private readonly IContextoUsuario _contexto;

    public PortalService(EmpleadosDbContext context, IncidenciaService incidencias, NotificacionService notificaciones, IContextoUsuario contexto)
    {
        _context = context;
        _incidencias = incidencias;
        _notificaciones = notificaciones;
        _contexto = contexto;
    }

    public Task<Empleado?> EmpleadoAsync()
    {
        var usuarioId = _contexto.UsuarioId;
        return usuarioId == null
            ? Task.FromResult<Empleado?>(null)
            : _context.Empleados.Include(e => e.Departamento).Include(e => e.PuestoNav).Include(e => e.Banco)
                .FirstOrDefaultAsync(e => e.UsuarioId == usuarioId);
    }

    public Task<List<ReciboPago>> RecibosAsync(int empleadoId) =>
        _context.RecibosPago.Include(r => r.Nomina)
            .Where(r => r.EmpleadoId == empleadoId && (r.Nomina!.Estado == EstadoNomina.Pagada || r.Nomina.Estado == EstadoNomina.Cerrada))
            .OrderByDescending(r => r.Nomina!.FechaFin)
            .ToListAsync();

    public Task<ReciboPago?> ReciboAsync(int empleadoId, int reciboId) =>
        _context.RecibosPago.Include(r => r.Nomina).Include(r => r.Lineas)
            .FirstOrDefaultAsync(r => r.Id == reciboId && r.EmpleadoId == empleadoId &&
                                      (r.Nomina!.Estado == EstadoNomina.Pagada || r.Nomina.Estado == EstadoNomina.Cerrada));

    public async Task<Resultado> SolicitarAsync(Empleado empleado, IncidenciaDto dto)
    {
        if (!Solicitables.Contains(dto.Tipo))
            return Resultado.Invalido("Ese tipo de incidencia lo registra Recursos Humanos", nameof(dto.Tipo));

        dto.EmpleadoId = empleado.Id;
        dto.Descontar = null; // lo decide la regla del tipo; RRHH puede ajustarlo
        var resultado = await _incidencias.CrearAsync(dto);
        if (!resultado.Ok)
            return resultado;

        var i = resultado.Valor!;
        await _notificaciones.NotificarEmpresaAsync(empleado.EmpresaId,
            $"{empleado.Nombre} {empleado.Apellido} solicitó {Textos.De(i.Tipo).ToLowerInvariant()} del {i.FechaInicio:dd/MM/yyyy} al {i.FechaFin:dd/MM/yyyy} ({i.Dias:0.##} días)",
            TipoNotificacion.Info);
        return Resultado.Exito("Solicitud enviada; Recursos Humanos la revisará");
    }
}

// Avisos del día para RRHH: cumpleaños, fin del período de prueba y vacaciones por vencer
public class RecordatoriosService
{
    public const int MesesPeriodoPrueba = 3;
    public const int DiasAvisoVacaciones = 30;

    private readonly EmpleadosDbContext _context;
    private readonly IncidenciaService _incidencias;
    private readonly NotificacionService _notificaciones;

    public RecordatoriosService(EmpleadosDbContext context, IncidenciaService incidencias, NotificacionService notificaciones)
    {
        _context = context;
        _incidencias = incidencias;
        _notificaciones = notificaciones;
    }

    public async Task<int> GenerarAsync(DateTime hoy)
    {
        var dia = hoy.Date;
        var creados = 0;
        var empleados = await _context.Empleados.IgnoreQueryFilters().Where(e => e.Estado == EstadoEmpleado.Activo).ToListAsync();
        foreach (var e in empleados)
        {
            var nombre = $"{e.Nombre} {e.Apellido}";
            if (e.FechaNacimiento.Month == dia.Month && e.FechaNacimiento.Day == dia.Day)
                creados += await Avisar(e.EmpresaId, $"Hoy cumple años {nombre}", TipoNotificacion.Success);
            if (e.FechaContratacion.Date.AddMonths(MesesPeriodoPrueba) == dia)
                creados += await Avisar(e.EmpresaId, $"Hoy termina el período de prueba de {nombre} ({MesesPeriodoPrueba} meses)", TipoNotificacion.Warning);

            var saldo = await _incidencias.SaldoVacacionesAsync(e.Id, dia);
            if (saldo != null && saldo.Disponibles > 0 && saldo.Derecho.Hasta.AddDays(-DiasAvisoVacaciones) == dia)
                creados += await Avisar(e.EmpresaId,
                    $"A {nombre} le quedan {saldo.Disponibles:0.##} días de vacaciones y su año de servicio termina el {saldo.Derecho.Hasta:dd/MM/yyyy}",
                    TipoNotificacion.Warning);
        }
        return creados;
    }

    // Una sola vez por día, aunque el proceso se ejecute varias veces
    private async Task<int> Avisar(int empresaId, string mensaje, TipoNotificacion tipo)
    {
        if (await _context.Notificaciones.AnyAsync(n => n.Mensaje == mensaje && n.FechaCreacion >= DateTime.Today))
            return 0;
        await _notificaciones.NotificarEmpresaAsync(empresaId, mensaje, tipo);
        return 1;
    }
}
