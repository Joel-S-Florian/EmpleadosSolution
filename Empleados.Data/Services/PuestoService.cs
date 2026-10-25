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
            DepartamentoId = dto.DepartamentoId,
            SalarioMinimo = dto.SalarioMinimo,
            SalarioMaximo = dto.SalarioMaximo
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
        puesto.SalarioMinimo = dto.SalarioMinimo;
        puesto.SalarioMaximo = dto.SalarioMaximo;
        await _context.SaveChangesAsync();

        // El rango nuevo no cambia salarios: solo avisa quién queda fuera
        var fuera = await _context.Empleados.CountAsync(e => e.PuestoId == id && e.Estado != EstadoEmpleado.Despedido &&
            e.TipoSalario == TipoSalario.FijoMensual &&
            ((dto.SalarioMinimo != null && e.Salario < dto.SalarioMinimo) || (dto.SalarioMaximo != null && e.Salario > dto.SalarioMaximo)));
        return Resultado.Exito(fuera == 0
            ? "Puesto actualizado correctamente"
            : $"Puesto actualizado. {fuera} empleado(s) de este puesto tienen un salario fuera del nuevo rango");
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
        if (dto.SalarioMinimo <= 0 || dto.SalarioMaximo <= 0)
            return Resultado.Invalido("Los salarios del rango deben ser mayores que 0", nameof(dto.SalarioMinimo));
        if (dto.SalarioMinimo > dto.SalarioMaximo)
            return Resultado.Invalido("El salario mínimo no puede ser mayor que el máximo", nameof(dto.SalarioMaximo));

        if (await _context.Puestos.AnyAsync(p => p.Nombre == dto.Nombre && p.DepartamentoId == dto.DepartamentoId && p.Id != id))
            return Resultado.Invalido(id == null
                ? "Ya existe un puesto con ese nombre en el departamento"
                : "Ya existe otro puesto con ese nombre en el departamento", nameof(dto.Nombre));

        return null;
    }
}
