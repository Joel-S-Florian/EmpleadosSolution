using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class PuestoService
{
    private readonly EmpleadosDbContext _context;

    public PuestoService(EmpleadosDbContext context)
    {
        _context = context;
    }

    public Task<List<Puesto>> ListarAsync(int? departamentoId = null)
    {
        var query = _context.Puestos.Include(p => p.Departamento).AsQueryable();
        if (departamentoId.HasValue)
            query = query.Where(p => p.DepartamentoId == departamentoId.Value);
        return query.OrderBy(p => p.Nombre).ToListAsync();
    }

    public Task<Puesto?> ObtenerAsync(int id) =>
        _context.Puestos.Include(p => p.Departamento).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Resultado<Puesto>> CrearAsync(CrearPuestoDto dto)
    {
        var error = await ValidarAsync(dto, null);
        if (error != null)
            return Resultado<Puesto>.Fallo(error);

        var puesto = new Puesto
        {
            EmpresaId = await EmpresaDelDepartamentoAsync(dto.DepartamentoId),
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            DepartamentoId = dto.DepartamentoId
        };
        _context.Puestos.Add(puesto);
        await _context.SaveChangesAsync();
        await _context.Entry(puesto).Reference(p => p.Departamento).LoadAsync();
        return Resultado<Puesto>.Con(puesto, "Puesto creado correctamente");
    }

    public async Task<Resultado> ActualizarAsync(int id, CrearPuestoDto dto)
    {
        var puesto = await _context.Puestos.FirstOrDefaultAsync(p => p.Id == id);
        if (puesto == null)
            return Resultado.NoEncontrado("Puesto no encontrado");

        var error = await ValidarAsync(dto, id);
        if (error != null)
            return error;

        puesto.EmpresaId = await EmpresaDelDepartamentoAsync(dto.DepartamentoId);
        puesto.Nombre = dto.Nombre;
        puesto.Descripcion = dto.Descripcion;
        puesto.DepartamentoId = dto.DepartamentoId;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Puesto actualizado correctamente");
    }

    public async Task<Resultado> EliminarAsync(int id)
    {
        var puesto = await _context.Puestos.Include(p => p.Empleados).FirstOrDefaultAsync(p => p.Id == id);
        if (puesto == null)
            return Resultado.NoEncontrado("Puesto no encontrado");

        if (puesto.Empleados.Any())
            return Resultado.Invalido("No se puede eliminar un puesto que tiene empleados asociados");

        _context.Puestos.Remove(puesto);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Puesto eliminado correctamente");
    }

    private Task<int> EmpresaDelDepartamentoAsync(int departamentoId) =>
        _context.Departamentos.Where(d => d.Id == departamentoId).Select(d => d.EmpresaId).FirstAsync();

    // El departamento se busca con el filtro de empresa: uno de otra empresa "no existe"
    private async Task<Resultado?> ValidarAsync(CrearPuestoDto dto, int? id)
    {
        if (!await _context.Departamentos.AnyAsync(d => d.Id == dto.DepartamentoId))
            return Resultado.Invalido("El departamento no existe", nameof(dto.DepartamentoId));

        if (await _context.Puestos.AnyAsync(p => p.Nombre == dto.Nombre && p.DepartamentoId == dto.DepartamentoId && p.Id != id))
            return Resultado.Invalido(id == null
                ? "Ya existe un puesto con ese nombre en el departamento"
                : "Ya existe otro puesto con ese nombre en el departamento", nameof(dto.Nombre));

        return null;
    }
}
