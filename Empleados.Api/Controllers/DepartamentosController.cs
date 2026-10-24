using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartamentosController : ControllerBase
{
    private readonly DepartamentoService _departamentos;

    public DepartamentosController(DepartamentoService departamentos)
    {
        _departamentos = departamentos;
    }

    // GET: api/Departamentos
    [HttpGet]
    public async Task<IActionResult> GetDepartamentos()
    {
        return Ok(await _departamentos.ListarAsync());
    }

    // GET: api/Departamentos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDepartamento(int id)
    {
        var departamento = await _departamentos.ObtenerAsync(id);
        if (departamento == null)
            return NotFound(new { mensaje = "Departamento no encontrado" });
        return Ok(departamento);
    }

    // POST: api/Departamentos
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CrearDepartamento([FromBody] CrearDepartamentoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _departamentos.CrearAsync(dto);
        if (!resultado.Ok)
            return this.Error(resultado);

        return CreatedAtAction(nameof(GetDepartamento), new { id = resultado.Valor!.Id }, resultado.Valor);
    }

    // PUT: api/Departamentos/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarDepartamento(int id, [FromBody] CrearDepartamentoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _departamentos.ActualizarAsync(id, dto);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }

    // DELETE: api/Departamentos/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDepartamento(int id)
    {
        var resultado = await _departamentos.EliminarAsync(id);
        return resultado.Ok ? Ok(new { mensaje = resultado.Mensaje }) : this.Error(resultado);
    }
}
