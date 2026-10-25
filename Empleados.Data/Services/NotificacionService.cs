using Empleados.Data.Data;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Empleados.Data.Services;

public class NotificacionService
{
    private readonly EmpleadosDbContext _context;
    private readonly ILogger<NotificacionService>? _logger;

    public NotificacionService(EmpleadosDbContext context, ILogger<NotificacionService>? logger = null)
    {
        _context = context;
        _logger = logger;
    }

    // Notificaciones del usuario más las globales (sin destino)
    private IQueryable<Notificacion> Visibles(string? usuarioId)
    {
        var query = _context.Notificaciones.AsQueryable();
        if (!string.IsNullOrEmpty(usuarioId))
            query = query.Where(n => n.UsuarioDestinoId == usuarioId || n.UsuarioDestinoId == null);
        return query;
    }

    public async Task<List<Notificacion>> ListarAsync(string? usuarioId, int? maximo = null)
    {
        var query = Visibles(usuarioId).OrderByDescending(n => n.FechaCreacion).AsQueryable();
        if (maximo.HasValue)
            query = query.Take(maximo.Value);
        return await query.ToListAsync();
    }

    public Task<int> ContarNoLeidasAsync(string? usuarioId) =>
        Visibles(usuarioId).CountAsync(n => !n.Leida);

    // Solo las del usuario: las globales las marca cada uno al abrirlas
    public async Task<int> MarcarTodasLeidasAsync(string? usuarioId)
    {
        if (string.IsNullOrEmpty(usuarioId))
            return 0;
        var pendientes = await _context.Notificaciones.Where(n => n.UsuarioDestinoId == usuarioId && !n.Leida).ToListAsync();
        pendientes.ForEach(n => n.Leida = true);
        await _context.SaveChangesAsync();
        return pendientes.Count;
    }

    public async Task<Resultado> MarcarLeidaAsync(int id, string? usuarioId)
    {
        var notificacion = await _context.Notificaciones.FindAsync(id);
        if (notificacion == null)
            return Resultado.NoEncontrado("Notificación no encontrada");

        if (!string.IsNullOrEmpty(notificacion.UsuarioDestinoId) && notificacion.UsuarioDestinoId != usuarioId)
            return Resultado.Prohibido("La notificación pertenece a otro usuario");

        notificacion.Leida = true;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Notificación marcada como leída");
    }

    // Usuarios de RRHH con acceso a la empresa; los del portal del empleado no reciben avisos internos
    private async Task<List<string?>> DestinosAsync(int empresaId)
    {
        var delPortal = _context.Empleados.IgnoreQueryFilters().Where(e => e.UsuarioId != null).Select(e => e.UsuarioId);
        return await _context.UsuariosEmpresas
            .Where(u => u.EmpresaId == empresaId && !delPortal.Contains(u.UsuarioId))
            .Select(u => (string?)u.UsuarioId)
            .ToListAsync();
    }

    public async Task NotificarEmpresaAsync(int empresaId, string mensaje, TipoNotificacion tipo)
    {
        foreach (var destino in await DestinosAsync(empresaId))
            _context.Notificaciones.Add(new Notificacion { Mensaje = mensaje, Tipo = tipo, FechaCreacion = DateTime.Now, UsuarioDestinoId = destino });
        await _context.SaveChangesAsync();
    }

    // Avisa a los usuarios con acceso a la empresa del empleado; si no hay ninguno, al usuario actual
    public async Task NotificarNuevoEmpleadoAsync(Empleado empleado, string? usuarioActualId)
    {
        try
        {
            var destinos = await DestinosAsync(empleado.EmpresaId);
            if (destinos.Count == 0)
                destinos.Add(usuarioActualId);

            foreach (var destino in destinos)
            {
                _context.Notificaciones.Add(new Notificacion
                {
                    Mensaje = $"Nuevo empleado registrado: {empleado.Nombre} {empleado.Apellido}",
                    Tipo = TipoNotificacion.Success,
                    Leida = false,
                    FechaCreacion = DateTime.Now,
                    UsuarioDestinoId = destino
                });
            }
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // No bloquear la creación del empleado si falla la notificación
            _logger?.LogError(ex, "No se pudo crear la notificación del empleado {EmpleadoId}", empleado.Id);
        }
    }
}
