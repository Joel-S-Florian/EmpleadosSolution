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
public class EmpleadosController : ControllerBase
{
    private readonly EmpleadosDbContext _context;

    public EmpleadosController(EmpleadosDbContext context)
    {
        _context = context;
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
        // Validar parámetros
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Construir la consulta
        var query = _context.Empleados
            .Include(e => e.Departamento)
            .AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(e => e.Nombre.Contains(nombre));

        if (!string.IsNullOrWhiteSpace(apellido))
            query = query.Where(e => e.Apellido.Contains(apellido));

        if (fechaContratacion.HasValue)
            query = query.Where(e => e.FechaContratacion.Date == fechaContratacion.Value.Date);

        // Obtener total de registros (para la paginación)
        var totalCount = await query.CountAsync();

        // Aplicar paginación y proyección a DTO
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmpleadoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                FechaNacimiento = e.FechaNacimiento,
                Direccion = e.Direccion,
                Telefono = e.Telefono,
                CorreoElectronico = e.CorreoElectronico,
                Puesto = e.Puesto,
                Salario = e.Salario,
                FechaContratacion = e.FechaContratacion,
                FechaTerminacion = e.FechaTerminacion,
                Estado = e.Estado.ToString(),
                FotoRuta = e.FotoRuta,
                NombreDepartamento = e.Departamento != null ? e.Departamento.Nombre : ""
            })
            .ToListAsync();

        // Retornar objeto con metadatos de paginación
        var resultado = new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return Ok(resultado);
    }

    // GET: api/Empleados/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmpleado(int id)
    {
        var empleado = await _context.Empleados
            .Include(e => e.Departamento)
            .Where(e => e.Id == id)
            .Select(e => new EmpleadoDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                FechaNacimiento = e.FechaNacimiento,
                Direccion = e.Direccion,
                Telefono = e.Telefono,
                CorreoElectronico = e.CorreoElectronico,
                Puesto = e.Puesto,
                Salario = e.Salario,
                FechaContratacion = e.FechaContratacion,
                FechaTerminacion = e.FechaTerminacion,
                Estado = e.Estado.ToString(),
                FotoRuta = e.FotoRuta,
                NombreDepartamento = e.Departamento != null ? e.Departamento.Nombre : ""
            })
            .FirstOrDefaultAsync();

        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        return Ok(empleado);
    }

    // POST: api/Empleados
    [HttpPost]
    public async Task<IActionResult> CrearEmpleado([FromBody] CrearEmpleadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verificar que el departamento existe
        var departamento = await _context.Departamentos.FindAsync(dto.DepartamentoId);
        if (departamento == null)
            return BadRequest(new { mensaje = "El departamento no existe" });

        // Verificar que el email no esté duplicado
        var emailExiste = await _context.Empleados
            .AnyAsync(e => e.CorreoElectronico == dto.CorreoElectronico);
        if (emailExiste)
            return BadRequest(new { mensaje = "Ya existe un empleado con ese correo electrónico" });

        var empleado = new Empleado
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            FechaNacimiento = dto.FechaNacimiento,
            Direccion = dto.Direccion,
            Telefono = dto.Telefono,
            CorreoElectronico = dto.CorreoElectronico,
            Puesto = dto.Puesto,
            Salario = dto.Salario,
            FechaContratacion = dto.FechaContratacion,
            DepartamentoId = dto.DepartamentoId,
            Estado = EstadoEmpleado.Activo
        };

        _context.Empleados.Add(empleado);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmpleado), new { id = empleado.Id }, empleado);
    }

    // PUT: api/Empleados/5
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarEmpleado(int id, [FromBody] ActualizarEmpleadoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        // Verificar que el departamento existe
        var departamento = await _context.Departamentos.FindAsync(dto.DepartamentoId);
        if (departamento == null)
            return BadRequest(new { mensaje = "El departamento no existe" });

        // Verificar que el email no esté duplicado (excepto el mismo empleado)
        var emailExiste = await _context.Empleados
            .AnyAsync(e => e.CorreoElectronico == dto.CorreoElectronico && e.Id != id);
        if (emailExiste)
            return BadRequest(new { mensaje = "Ya existe otro empleado con ese correo electrónico" });

        empleado.Nombre = dto.Nombre;
        empleado.Apellido = dto.Apellido;
        empleado.FechaNacimiento = dto.FechaNacimiento;
        empleado.Direccion = dto.Direccion;
        empleado.Telefono = dto.Telefono;
        empleado.CorreoElectronico = dto.CorreoElectronico;
        empleado.Puesto = dto.Puesto;
        empleado.Salario = dto.Salario;
        empleado.FechaContratacion = dto.FechaContratacion;
        empleado.DepartamentoId = dto.DepartamentoId;

        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado actualizado correctamente" });
    }

    // PATCH: api/Empleados/5/desactivar
    [HttpPatch("{id}/desactivar")]
    public async Task<IActionResult> DesactivarEmpleado(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        if (empleado.Estado == EstadoEmpleado.Despedido)
            return BadRequest(new { mensaje = "No se puede desactivar un empleado despedido" });

        empleado.Estado = EstadoEmpleado.Inactivo;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado desactivado correctamente", estado = empleado.Estado.ToString() });
    }

    // PATCH: api/Empleados/5/despedir
    [HttpPatch("{id}/despedir")]
    public async Task<IActionResult> DespedirEmpleado(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        if (empleado.Estado == EstadoEmpleado.Despedido)
            return BadRequest(new { mensaje = "El empleado ya está despedido" });

        empleado.Estado = EstadoEmpleado.Despedido;
        empleado.FechaTerminacion = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Empleado despedido correctamente", estado = empleado.Estado.ToString(), fechaTerminacion = empleado.FechaTerminacion });
    }

    // POST: api/Empleados/1/foto
    [HttpPost("{id}/foto")]
    public async Task<IActionResult> SubirFoto(int id, IFormFile foto)
    {
        // Validar que el empleado existe
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return NotFound(new { mensaje = "Empleado no encontrado" });

        // Validar el archivo
        if (foto == null || foto.Length == 0)
            return BadRequest(new { mensaje = "Debe seleccionar una imagen" });

        // Validar tipo de archivo (solo imágenes)
        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
            return BadRequest(new { mensaje = "Solo se permiten archivos de imagen (jpg, jpeg, png, gif)" });

        // Validar tamaño (máximo 5MB)
        if (foto.Length > 5 * 1024 * 1024)
            return BadRequest(new { mensaje = "La imagen no puede superar los 5MB" });

        // Crear directorio si no existe
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "empleados");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generar nombre único para el archivo
        var nombreArchivo = $"{id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var rutaArchivo = Path.Combine(uploadsFolder, nombreArchivo);

        // Guardar el archivo
        using (var stream = new FileStream(rutaArchivo, FileMode.Create))
        {
            await foto.CopyToAsync(stream);
        }

        // Guardar la ruta relativa en la base de datos
        var rutaRelativa = $"/uploads/empleados/{nombreArchivo}";
        empleado.FotoRuta = rutaRelativa;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Foto subida correctamente", ruta = rutaRelativa });
    }
}