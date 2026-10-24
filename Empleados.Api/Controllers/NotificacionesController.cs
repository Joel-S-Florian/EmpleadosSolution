using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificacionesController : ControllerBase
{
    private readonly NotificacionService _notificaciones;

    public NotificacionesController(NotificacionService notificaciones)
    {
        _notificaciones = notificaciones;
    }

    private string? UsuarioActualId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // GET: api/Notificaciones/mis-notificaciones
    [HttpGet("mis-notificaciones")]
    public async Task<IActionResult> GetMisNotificaciones()
    {
        var notificaciones = await _notificaciones.ListarAsync(UsuarioActualId);
        return Ok(notificaciones.Select(NotificacionDto.Desde).ToList());
    }

    // POST: api/Notificaciones/marcar-leida/{id}
    [HttpPost("marcar-leida/{id}")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        var resultado = await _notificaciones.MarcarLeidaAsync(id, UsuarioActualId);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // GET: api/Notificaciones/no-leidas-count - para badge
    [HttpGet("no-leidas-count")]
    public async Task<IActionResult> GetNoLeidasCount()
    {
        return Ok(new { count = await _notificaciones.ContarNoLeidasAsync(UsuarioActualId) });
    }
}
