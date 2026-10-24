using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class DepartamentoService
{
    private readonly EmpleadosDbContext _context;
    private readonly IContextoUsuario _contexto;

    public DepartamentoService(EmpleadosDbContext context, IContextoUsuario contexto)
    {
        _context = context;
        _contexto = contexto;
    }

    public Task<List<Departamento>> ListarAsync(bool conEmpleados = false)
    {
        var query = _context.Departamentos.AsQueryable();
        if (conEmpleados)
            query = query.Include(d => d.Empleados);
        return query.OrderBy(d => d.Nombre).ToListAsync();
    }

    public Task<Departamento?> ObtenerAsync(int id) =>
        _context.Departamentos.FirstOrDefaultAsync(d => d.Id == id);

    // El departamento nuevo pertenece a la empresa seleccionada por el usuario
    public async Task<Resultado<Departamento>> CrearAsync(CrearDepartamentoDto dto)
    {
        if (_contexto.EmpresaId is not int empresaId)
            return Resultado<Departamento>.Fallo(Resultado.Invalido("Seleccione una empresa antes de crear departamentos"));
        if (await _context.Departamentos.AnyAsync(d => d.EmpresaId == empresaId && d.Nombre == dto.Nombre))
            return Resultado<Departamento>.Fallo(Resultado.Invalido("Ya existe un departamento con ese nombre", nameof(dto.Nombre)));

        var departamento = new Departamento { EmpresaId = empresaId, Nombre = dto.Nombre, Descripcion = dto.Descripcion };
        _context.Departamentos.Add(departamento);
        await _context.SaveChangesAsync();
        return Resultado<Departamento>.Con(departamento, "Departamento creado correctamente");
    }

    public async Task<Resultado> ActualizarAsync(int id, CrearDepartamentoDto dto)
    {
        var departamento = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == id);
        if (departamento == null)
            return Resultado.NoEncontrado("Departamento no encontrado");

        if (await _context.Departamentos.AnyAsync(d => d.EmpresaId == departamento.EmpresaId && d.Nombre == dto.Nombre && d.Id != id))
            return Resultado.Invalido("Ya existe otro departamento con ese nombre", nameof(dto.Nombre));

        departamento.Nombre = dto.Nombre;
        departamento.Descripcion = dto.Descripcion;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Departamento actualizado correctamente");
    }

    public async Task<Resultado> EliminarAsync(int id)
    {
        var departamento = await _context.Departamentos
            .Include(d => d.Empleados)
            .Include(d => d.Puestos)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (departamento == null)
            return Resultado.NoEncontrado("Departamento no encontrado");

        if (departamento.Empleados.Any())
            return Resultado.Invalido("No se puede eliminar un departamento que tiene empleados asociados");
        if (departamento.Puestos.Any())
            return Resultado.Invalido("No se puede eliminar un departamento que tiene puestos asociados");

        _context.Departamentos.Remove(departamento);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Departamento eliminado correctamente");
    }
}
