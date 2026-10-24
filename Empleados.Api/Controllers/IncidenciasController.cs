using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

// Faltas, retardos, permisos, licencias y vacaciones de la empresa actual
[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class IncidenciasController : ControllerBase
{
    private readonly IncidenciaService _incidencias;

    public IncidenciasController(IncidenciaService incidencias)
    {
        _incidencias = incidencias;
    }

    // GET: api/Incidencias?empleadoId=5&estado=Pendiente&desde=2026-10-01&hasta=2026-10-31
    [HttpGet]
    public async Task<IActionResult> GetIncidencias([FromQuery] int? empleadoId, [FromQuery] EstadoIncidencia? estado, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var incidencias = await _incidencias.ListarAsync(empleadoId, estado, desde, hasta);
        return Ok(incidencias.Select(i => new
        {
            i.Id, i.EmpleadoId, Empleado = $"{i.Empleado?.Nombre} {i.Empleado?.Apellido}", i.Tipo, i.FechaInicio, i.FechaFin,
            i.Dias, i.Horas, i.Descontar, i.Estado, i.Comentario, i.MotivoRechazo, i.CreadaPor, i.RevisadaPor, i.FechaRevision
        }));
    }

    // GET: api/Incidencias/vacaciones/5?fecha=2026-10-24
    [HttpGet("vacaciones/{empleadoId}")]
    public async Task<IActionResult> GetSaldoVacaciones(int empleadoId, [FromQuery] DateTime? fecha)
    {
        var saldo = await _incidencias.SaldoVacacionesAsync(empleadoId, fecha);
        if (saldo == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });
        return Ok(new
        {
            saldo.Derecho.AniosServicio, saldo.Derecho.Desde, saldo.Derecho.Hasta, Derecho = saldo.Derecho.Dias,
            saldo.Tomados, saldo.PorAprobar, saldo.Disponibles
        });
    }

    // POST: api/Incidencias
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] IncidenciaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var resultado = await _incidencias.CrearAsync(dto);
        return resultado.Ok ? Ok(new { resultado.Valor!.Id, resultado.Valor.Dias, mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // POST: api/Incidencias/colectivas
    [HttpPost("colectivas")]
    public async Task<IActionResult> Colectivas([FromBody] VacacionesColectivasDto dto) =>
        Respuesta(await _incidencias.VacacionesColectivasAsync(dto));

    [HttpPost("{id}/aprobar")]
    public async Task<IActionResult> Aprobar(int id) => Respuesta(await _incidencias.AprobarAsync(id));

    // POST: api/Incidencias/5/rechazar?motivo=...
    [HttpPost("{id}/rechazar")]
    public async Task<IActionResult> Rechazar(int id, [FromQuery] string? motivo) => Respuesta(await _incidencias.RechazarAsync(id, motivo));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id) => Respuesta(await _incidencias.EliminarAsync(id));

    private IActionResult Respuesta(Resultado resultado) =>
        resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
}
