using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class PrestamoService
{
    private readonly EmpleadosDbContext _context;

    public PrestamoService(EmpleadosDbContext context)
    {
        _context = context;
    }

    public Task<List<Prestamo>> ListarAsync(int? empleadoId = null) =>
        _context.Prestamos
            .Include(p => p.Empleado)
            .Where(p => empleadoId == null || p.EmpleadoId == empleadoId)
            .OrderByDescending(p => p.Activo).ThenByDescending(p => p.FechaCreacion)
            .ToListAsync();

    public async Task<Resultado<Prestamo>> CrearAsync(PrestamoDto dto)
    {
        // Con el filtro de empresa, un empleado de otra empresa "no existe"
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoId);
        if (empleado == null)
            return Resultado<Prestamo>.Fallo(Resultado.Invalido("El empleado no existe", nameof(dto.EmpleadoId)));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<Prestamo>.Fallo(Resultado.Invalido("No se pueden registrar préstamos a un empleado despedido", nameof(dto.EmpleadoId)));
        if (dto.MontoCuota > dto.MontoTotal)
            return Resultado<Prestamo>.Fallo(Resultado.Invalido("La cuota no puede ser mayor que el monto total", nameof(dto.MontoCuota)));

        var prestamo = new Prestamo
        {
            EmpresaId = empleado.EmpresaId,
            EmpleadoId = empleado.Id,
            Concepto = dto.Concepto.Trim(),
            MontoTotal = dto.MontoTotal,
            MontoCuota = dto.MontoCuota,
            Saldo = dto.MontoTotal,
            Prioridad = dto.Prioridad,
            FechaInicio = dto.FechaInicio.Date,
            SoloFinDeMes = dto.SoloFinDeMes,
            FechaCreacion = DateTime.Now
        };
        _context.Prestamos.Add(prestamo);
        await _context.SaveChangesAsync();
        return Resultado<Prestamo>.Con(prestamo, "Préstamo registrado");
    }

    // Deja de descontarse; el saldo pendiente queda registrado
    public async Task<Resultado> CancelarAsync(int id)
    {
        var prestamo = await _context.Prestamos.FirstOrDefaultAsync(p => p.Id == id);
        if (prestamo == null)
            return Resultado.NoEncontrado("Préstamo no encontrado");
        if (!prestamo.Activo)
            return Resultado.Invalido("El préstamo ya no está activo");

        prestamo.Activo = false;
        await _context.SaveChangesAsync();
        return Resultado.Exito("Préstamo cancelado");
    }
}
