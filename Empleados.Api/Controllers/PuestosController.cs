using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PuestosController : ControllerBase
{
    private readonly PuestoService _puestos;

    public PuestosController(PuestoService puestos)
    {
        _puestos = puestos;
    }

    // GET: api/Puestos?departamentoId=1
    [HttpGet]
    public async Task<IActionResult> GetPuestos([FromQuery] int? departamentoId)
    {
        var puestos = await _puestos.ListarAsync(departamentoId);
        return Ok(puestos.Select(PuestoDto.Desde).ToList());
    }

    // GET: api/Puestos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPuesto(int id)
    {
        var puesto = await _puestos.ObtenerAsync(id);
        if (puesto == null)
            return NotFound(new { mensaje = "Puesto no encontrado" });

        return Ok(PuestoDto.Desde(puesto));
    }

    // POST: api/Puestos
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CrearPuesto([FromBody] CrearPuestoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _puestos.CrearAsync(dto);
        if (!resultado.Ok)
            return this.Error(resultado);

        return CreatedAtAction(nameof(GetPuesto), new { id = resultado.Valor!.Id }, PuestoDto.Desde(resultado.Valor));
    }

    // PUT: api/Puestos/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarPuesto(int id, [FromBody] CrearPuestoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _puestos.ActualizarAsync(id, dto);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // DELETE: api/Puestos/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarPuesto(int id)
    {
        var resultado = await _puestos.EliminarAsync(id);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }
}
