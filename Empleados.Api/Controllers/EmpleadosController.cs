using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly EmpleadoService _empleados;

    public EmpleadosController(EmpleadoService empleados)
    {
        _empleados = empleados;
    }

    // GET: api/Empleados?page=1&pageSize=5&nombre=Ana&apellido=Garcia&fechaContratacion=2018-03-01
    [HttpGet]
    public async Task<IActionResult> GetEmpleados(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nombre = null,
        [FromQuery] string? apellido = null,
        [FromQuery] DateTime? fechaContratacion = null)
    {
        var resultado = await _empleados.ListarAsync(nombre, apellido, fechaContratacion, page, pageSize);

        return Ok(new
        {
            items = resultado.Items.Select(EmpleadoDto.Desde).ToList(),
            totalCount = resultado.TotalCount,
            page = resultado.Page,
            pageSize = resultado.PageSize,
            totalPages = resultado.TotalPages
        });
    }

    // GET: api/Empleados/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmpleado(int id)
    {
        var empleado = await _empleados.ObtenerAsync(id);
        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        return Ok(EmpleadoDto.Desde(empleado));
    }

    // POST: api/Empleados
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CrearEmpleado([FromBody] CrearEmpleadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _empleados.CrearAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (!resultado.Ok)
            return this.Error(resultado);

        var empleado = await _empleados.ObtenerAsync(resultado.Valor!.Id);
        return CreatedAtAction(nameof(GetEmpleado), new { id = empleado!.Id }, EmpleadoDto.Desde(empleado));
    }

    // PUT: api/Empleados/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarEmpleado(int id, [FromBody] ActualizarEmpleadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _empleados.ActualizarAsync(id, dto);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // PATCH: api/Empleados/5/desactivar
    [Authorize(Roles = "Admin,RRHH")]
    [HttpPatch("{id}/desactivar")]
    public async Task<IActionResult> DesactivarEmpleado(int id)
    {
        var resultado = await _empleados.DesactivarAsync(id);
        if (!resultado.Ok)
            return this.Error(resultado);

        return Ok(new { mensaje = resultado.Mensaje, estado = resultado.Valor!.Estado.ToString() });
    }

    // PATCH: api/Empleados/5/despedir
    [Authorize(Roles = "Admin,RRHH")]
    [HttpPatch("{id}/despedir")]
    public async Task<IActionResult> DespedirEmpleado(int id)
    {
        var resultado = await _empleados.DespedirAsync(id);
        if (!resultado.Ok)
            return this.Error(resultado);

        return Ok(new { mensaje = resultado.Mensaje, estado = resultado.Valor!.Estado.ToString(), fechaTerminacion = resultado.Valor.FechaTerminacion });
    }

    // POST: api/Empleados/1/foto
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/foto")]
    public async Task<IActionResult> SubirFoto(int id, IFormFile? foto)
    {
        using var contenido = foto?.OpenReadStream();
        var resultado = await _empleados.SubirFotoAsync(id, contenido, foto?.FileName, foto?.Length ?? 0);
        if (!resultado.Ok)
            return this.Error(resultado);

        return Ok(new { mensaje = resultado.Mensaje, ruta = resultado.Valor });
    }
}
