using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DepartamentosController : ControllerBase
{
    private readonly EmpleadosDbContext _context;

    public DepartamentosController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: api/Departamentos
    [HttpGet]
    public async Task<IActionResult> GetDepartamentos()
    {
        var departamentos = await _context.Departamentos.ToListAsync();
        return Ok(departamentos);
    }

    // GET: api/Departamentos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDepartamento(int id)
    {
        var departamento = await _context.Departamentos.FindAsync(id);
        if (departamento == null)
            return NotFound(new { mensaje = "Departamento no encontrado" });
        return Ok(departamento);
    }

    // POST: api/Departamentos
    [HttpPost]
    public async Task<IActionResult> CrearDepartamento([FromBody] CrearDepartamentoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verificar que no exista un departamento con el mismo nombre
        var existe = await _context.Departamentos
            .AnyAsync(d => d.Nombre == dto.Nombre);
        if (existe)
            return BadRequest(new { mensaje = "Ya existe un departamento con ese nombre" });

        var departamento = new Departamento
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion
        };

        _context.Departamentos.Add(departamento);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDepartamento), new { id = departamento.Id }, departamento);
    }

    // PUT: api/Departamentos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarDepartamento(int id, [FromBody] CrearDepartamentoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var departamento = await _context.Departamentos.FindAsync(id);
        if (departamento == null)
            return NotFound(new { mensaje = "Departamento no encontrado" });

        // Verificar que no exista otro departamento con el mismo nombre
        var existe = await _context.Departamentos
            .AnyAsync(d => d.Nombre == dto.Nombre && d.Id != id);
        if (existe)
            return BadRequest(new { mensaje = "Ya existe otro departamento con ese nombre" });

        departamento.Nombre = dto.Nombre;
        departamento.Descripcion = dto.Descripcion;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Departamento actualizado correctamente" });
    }

    // DELETE: api/Departamentos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarDepartamento(int id)
    {
        var departamento = await _context.Departamentos
            .Include(d => d.Empleados)
            .Include(d => d.Puestos)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (departamento == null)
            return NotFound(new { mensaje = "Departamento no encontrado" });

        // Regla de negocio: No permitir eliminar departamento con empleados o puestos asociados
        if (departamento.Empleados.Any())
            return BadRequest(new { mensaje = "No se puede eliminar un departamento que tiene empleados asociados" });
        if (departamento.Puestos.Any())
            return BadRequest(new { mensaje = "No se puede eliminar un departamento que tiene puestos asociados" });

        _context.Departamentos.Remove(departamento);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Departamento eliminado correctamente" });
    }
}