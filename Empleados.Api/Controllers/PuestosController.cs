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
public class PuestosController : ControllerBase
{
    private readonly EmpleadosDbContext _context;

    public PuestosController(EmpleadosDbContext context)
    {
        _context = context;
    }

    // GET: api/Puestos?departamentoId=1
    [HttpGet]
    public async Task<IActionResult> GetPuestos([FromQuery] int? departamentoId)
    {
        var query = _context.Puestos
            .Include(p => p.Departamento)
            .AsQueryable();

        if (departamentoId.HasValue)
            query = query.Where(p => p.DepartamentoId == departamentoId.Value);

        var result = await query
            .Select(p => new PuestoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                DepartamentoId = p.DepartamentoId,
                NombreDepartamento = p.Departamento != null ? p.Departamento.Nombre : ""
            })
            .ToListAsync();

        return Ok(result);
    }

    // GET: api/Puestos/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPuesto(int id)
    {
        var puesto = await _context.Puestos
            .Include(p => p.Departamento)
            .Where(p => p.Id == id)
            .Select(p => new PuestoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                DepartamentoId = p.DepartamentoId,
                NombreDepartamento = p.Departamento != null ? p.Departamento.Nombre : ""
            })
            .FirstOrDefaultAsync();

        if (puesto == null)
            return NotFound(new { mensaje = "Puesto no encontrado" });

        return Ok(puesto);
    }

    // POST: api/Puestos
    [HttpPost]
    public async Task<IActionResult> CrearPuesto([FromBody] CrearPuestoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var departamento = await _context.Departamentos.FindAsync(dto.DepartamentoId);
        if (departamento == null)
            return BadRequest(new { mensaje = "El departamento no existe" });

        var existe = await _context.Puestos.AnyAsync(p => p.Nombre == dto.Nombre && p.DepartamentoId == dto.DepartamentoId);
        if (existe)
            return BadRequest(new { mensaje = "Ya existe un puesto con ese nombre en el departamento" });

        var puesto = new Puesto
        {
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            DepartamentoId = dto.DepartamentoId
        };

        _context.Puestos.Add(puesto);
        await _context.SaveChangesAsync();

        var resultDto = new PuestoDto
        {
            Id = puesto.Id,
            Nombre = puesto.Nombre,
            Descripcion = puesto.Descripcion,
            DepartamentoId = puesto.DepartamentoId,
            NombreDepartamento = departamento.Nombre
        };

        return CreatedAtAction(nameof(GetPuesto), new { id = puesto.Id }, resultDto);
    }

    // PUT: api/Puestos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarPuesto(int id, [FromBody] CrearPuestoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var puesto = await _context.Puestos.FindAsync(id);
        if (puesto == null)
            return NotFound(new { mensaje = "Puesto no encontrado" });

        var departamento = await _context.Departamentos.FindAsync(dto.DepartamentoId);
        if (departamento == null)
            return BadRequest(new { mensaje = "El departamento no existe" });

        var existe = await _context.Puestos.AnyAsync(p => p.Nombre == dto.Nombre && p.DepartamentoId == dto.DepartamentoId && p.Id != id);
        if (existe)
            return BadRequest(new { mensaje = "Ya existe otro puesto con ese nombre en el departamento" });

        puesto.Nombre = dto.Nombre;
        puesto.Descripcion = dto.Descripcion;
        puesto.DepartamentoId = dto.DepartamentoId;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Puesto actualizado correctamente" });
    }

    // DELETE: api/Puestos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarPuesto(int id)
    {
        var puesto = await _context.Puestos
            .Include(p => p.Empleados)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (puesto == null)
            return NotFound(new { mensaje = "Puesto no encontrado" });

        if (puesto.Empleados.Any())
            return BadRequest(new { mensaje = "No se puede eliminar un puesto que tiene empleados asociados" });

        _context.Puestos.Remove(puesto);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Puesto eliminado correctamente" });
    }
}
