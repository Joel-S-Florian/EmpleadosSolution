using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class EmpleadoService
{
    public const long TamanoMaximoFoto = 5 * 1024 * 1024;
    private static readonly string[] ExtensionesFoto = { ".jpg", ".jpeg", ".png", ".gif" };

    private readonly EmpleadosDbContext _context;
    private readonly NotificacionService _notificaciones;
    private readonly AlmacenFotos _fotos;

    public EmpleadoService(EmpleadosDbContext context, NotificacionService notificaciones, AlmacenFotos fotos)
    {
        _context = context;
        _notificaciones = notificaciones;
        _fotos = fotos;
    }

    public async Task<Paginado<Empleado>> ListarAsync(
        string? nombre, string? apellido, DateTime? fechaContratacion, int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.PuestoNav)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(e => e.Nombre.Contains(nombre));
        if (!string.IsNullOrWhiteSpace(apellido))
            query = query.Where(e => e.Apellido.Contains(apellido));
        if (fechaContratacion.HasValue)
            query = query.Where(e => e.FechaContratacion.Date == fechaContratacion.Value.Date);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new Paginado<Empleado>(items, totalCount, page, pageSize);
    }

    public Task<Empleado?> ObtenerAsync(int id) =>
        _context.Empleados
            .Include(e => e.Departamento)
            .Include(e => e.PuestoNav)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Resultado<Empleado>> CrearAsync(CrearEmpleadoDto dto, string? usuarioActualId)
    {
        var error = await ValidarAsync(dto, null);
        if (error != null)
            return Resultado<Empleado>.Fallo(error);

        var empleado = new Empleado { Estado = EstadoEmpleado.Activo };
        await AplicarAsync(empleado, dto);
        _context.Empleados.Add(empleado);
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarNuevoEmpleadoAsync(empleado, usuarioActualId);
        return Resultado<Empleado>.Con(empleado, "Empleado creado correctamente");
    }

    public async Task<Resultado> ActualizarAsync(int id, ActualizarEmpleadoDto dto)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return Resultado.NoEncontrado("Empleado no encontrado");
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado.Invalido("No se puede modificar un empleado despedido");

        var error = await ValidarAsync(dto, id);
        if (error != null)
            return error;

        await AplicarAsync(empleado, dto);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Empleado actualizado correctamente");
    }

    public async Task<Resultado<Empleado>> DesactivarAsync(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return Resultado<Empleado>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<Empleado>.Fallo(Resultado.Invalido("No se puede desactivar un empleado despedido"));

        empleado.Estado = EstadoEmpleado.Inactivo;
        await _context.SaveChangesAsync();
        return Resultado<Empleado>.Con(empleado, "Empleado desactivado correctamente");
    }

    public async Task<Resultado<Empleado>> DespedirAsync(int id)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return Resultado<Empleado>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<Empleado>.Fallo(Resultado.Invalido("El empleado ya está despedido"));

        empleado.Estado = EstadoEmpleado.Despedido;
        empleado.FechaTerminacion = DateTime.Now;
        await _context.SaveChangesAsync();
        return Resultado<Empleado>.Con(empleado, "Empleado despedido correctamente");
    }

    public async Task<Resultado<string>> SubirFotoAsync(int id, Stream? contenido, string? nombreArchivo, long tamano)
    {
        var empleado = await _context.Empleados.FindAsync(id);
        if (empleado == null)
            return Resultado<string>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<string>.Fallo(Resultado.Invalido("No se puede modificar un empleado despedido"));
        if (contenido == null || tamano == 0)
            return Resultado<string>.Fallo(Resultado.Invalido("Debe seleccionar una imagen"));

        var extension = Path.GetExtension(nombreArchivo ?? "").ToLowerInvariant();
        if (!ExtensionesFoto.Contains(extension))
            return Resultado<string>.Fallo(Resultado.Invalido("Solo se permiten archivos de imagen (jpg, jpeg, png, gif)"));
        if (tamano > TamanoMaximoFoto)
            return Resultado<string>.Fallo(Resultado.Invalido("La imagen no puede superar los 5MB"));

        empleado.FotoRuta = await _fotos.GuardarAsync(id, contenido, extension);
        await _context.SaveChangesAsync();
        return Resultado<string>.Con(empleado.FotoRuta, "Foto subida correctamente");
    }

    private async Task<Resultado?> ValidarAsync(CrearEmpleadoDto dto, int? id)
    {
        if (!await _context.Departamentos.AnyAsync(d => d.Id == dto.DepartamentoId))
            return Resultado.Invalido("El departamento no existe", nameof(dto.DepartamentoId));

        if (await _context.Empleados.AnyAsync(e => e.CorreoElectronico == dto.CorreoElectronico && e.Id != id))
            return Resultado.Invalido(id == null
                ? "Ya existe un empleado con ese correo electrónico"
                : "Ya existe otro empleado con ese correo electrónico", nameof(dto.CorreoElectronico));

        if (dto.PuestoId.HasValue)
        {
            var puesto = await _context.Puestos.FindAsync(dto.PuestoId.Value);
            if (puesto == null)
                return Resultado.Invalido("El puesto no existe", nameof(dto.PuestoId));
            if (puesto.DepartamentoId != dto.DepartamentoId)
                return Resultado.Invalido("El puesto no pertenece al departamento seleccionado", nameof(dto.PuestoId));
        }

        return null;
    }

    private async Task AplicarAsync(Empleado empleado, CrearEmpleadoDto dto)
    {
        empleado.Nombre = dto.Nombre;
        empleado.Apellido = dto.Apellido;
        empleado.FechaNacimiento = dto.FechaNacimiento;
        empleado.Direccion = dto.Direccion;
        empleado.Telefono = dto.Telefono;
        empleado.CorreoElectronico = dto.CorreoElectronico;
        empleado.Salario = dto.Salario;
        empleado.FechaContratacion = dto.FechaContratacion;
        empleado.DepartamentoId = dto.DepartamentoId;
        empleado.PuestoId = dto.PuestoId;
        empleado.Puesto = dto.PuestoId.HasValue
            ? (await _context.Puestos.FindAsync(dto.PuestoId.Value))!.Nombre
            : dto.Puesto;
    }
}
