using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Empleados.Data.Services;

public class EmpleadoService
{
    public const long TamanoMaximoFoto = 5 * 1024 * 1024;
    private static readonly string[] ExtensionesFoto = { ".jpg", ".jpeg", ".png", ".gif" };

    private readonly EmpleadosDbContext _context;
    private readonly NotificacionService _notificaciones;
    private readonly AlmacenFotos _fotos;
    private readonly ParametrosLegalesService _parametros;
    private readonly IContextoUsuario _contexto;

    public EmpleadoService(
        EmpleadosDbContext context,
        NotificacionService notificaciones,
        AlmacenFotos fotos,
        ParametrosLegalesService parametros,
        IContextoUsuario contexto)
    {
        _context = context;
        _notificaciones = notificaciones;
        _fotos = fotos;
        _parametros = parametros;
        _contexto = contexto;
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
            .Include(e => e.Banco)
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
            .Include(e => e.Banco)
            .FirstOrDefaultAsync(e => e.Id == id);

    public Task<List<MovimientoEmpleado>> HistorialAsync(int empleadoId) =>
        _context.MovimientosEmpleados
            .Where(m => m.EmpleadoId == empleadoId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

    // Para las listas de selección: todos, del ingreso más reciente al más antiguo
    public Task<List<Empleado>> ParaSeleccionAsync(bool incluirDespedidos = false) =>
        _context.Empleados
            .Where(e => incluirDespedidos || e.Estado != EstadoEmpleado.Despedido)
            .OrderByDescending(e => e.FechaContratacion).ThenByDescending(e => e.Id)
            .ToListAsync();

    public Task<List<Banco>> BancosAsync() =>
        _context.Bancos.OrderBy(b => b.Nombre).ToListAsync();

    public async Task<Resultado<Empleado>> CrearAsync(CrearEmpleadoDto dto)
    {
        var (error, departamento) = await ValidarAsync(dto, null);
        if (error != null)
            return Resultado<Empleado>.Fallo(error);

        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == departamento!.EmpresaId);
        var empleado = new Empleado
        {
            EmpresaId = departamento!.EmpresaId,
            Estado = EstadoEmpleado.Activo,
            FrecuenciaPago = dto.FrecuenciaPago ?? empresa?.FrecuenciaPagoPredeterminada ?? FrecuenciaPago.Quincenal
        };
        Aplicar(empleado, dto);
        _context.Empleados.Add(empleado);
        Registrar(empleado, TipoMovimiento.Alta, null, Dinero(empleado.Salario));
        await _context.SaveChangesAsync();

        await _notificaciones.NotificarNuevoEmpleadoAsync(empleado, _contexto.UsuarioId);
        return Resultado<Empleado>.Con(empleado, "Empleado creado correctamente");
    }

    public async Task<Resultado> ActualizarAsync(int id, ActualizarEmpleadoDto dto)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null)
            return Resultado.NoEncontrado("Empleado no encontrado");
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado.Invalido("No se puede modificar un empleado despedido");

        var (error, _) = await ValidarAsync(dto, id);
        if (error != null)
            return error;

        var antes = (empleado.Salario, empleado.PuestoId, empleado.DepartamentoId, empleado.FrecuenciaPago);
        Aplicar(empleado, dto);
        empleado.FrecuenciaPago = dto.FrecuenciaPago ?? empleado.FrecuenciaPago;

        if (antes.Salario != empleado.Salario)
            Registrar(empleado, TipoMovimiento.CambioSalario, Dinero(antes.Salario), Dinero(empleado.Salario));
        if (antes.PuestoId != empleado.PuestoId)
            Registrar(empleado, TipoMovimiento.CambioPuesto, await NombrePuestoAsync(antes.PuestoId), await NombrePuestoAsync(empleado.PuestoId));
        if (antes.DepartamentoId != empleado.DepartamentoId)
            Registrar(empleado, TipoMovimiento.CambioDepartamento, await NombreDepartamentoAsync(antes.DepartamentoId), await NombreDepartamentoAsync(empleado.DepartamentoId));
        if (antes.FrecuenciaPago != empleado.FrecuenciaPago)
            Registrar(empleado, TipoMovimiento.CambioFrecuenciaPago, antes.FrecuenciaPago.ToString(), empleado.FrecuenciaPago.ToString());

        await _context.SaveChangesAsync();
        return Resultado.Exito("Empleado actualizado correctamente");
    }

    public async Task<Resultado<Empleado>> DesactivarAsync(int id)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null)
            return Resultado<Empleado>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<Empleado>.Fallo(Resultado.Invalido("No se puede desactivar un empleado despedido"));

        Registrar(empleado, TipoMovimiento.Desactivacion, empleado.Estado.ToString(), EstadoEmpleado.Inactivo.ToString());
        empleado.Estado = EstadoEmpleado.Inactivo;
        await _context.SaveChangesAsync();
        return Resultado<Empleado>.Con(empleado, "Empleado desactivado correctamente");
    }

    public async Task<Resultado<Empleado>> DespedirAsync(int id)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
        if (empleado == null)
            return Resultado<Empleado>.Fallo(Resultado.NoEncontrado("Empleado no encontrado"));
        if (empleado.Estado == EstadoEmpleado.Despedido)
            return Resultado<Empleado>.Fallo(Resultado.Invalido("El empleado ya está despedido"));

        Registrar(empleado, TipoMovimiento.Despido, empleado.Estado.ToString(), EstadoEmpleado.Despedido.ToString());
        empleado.Estado = EstadoEmpleado.Despedido;
        empleado.FechaTerminacion = DateTime.Now;
        await _context.SaveChangesAsync();
        return Resultado<Empleado>.Con(empleado, "Empleado despedido correctamente");
    }

    public async Task<Resultado<string>> SubirFotoAsync(int id, Stream? contenido, string? nombreArchivo, long tamano)
    {
        var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id == id);
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

    // Departamento y puesto se buscan con el filtro de empresa: uno de otra empresa "no existe"
    private async Task<(Resultado? Error, Departamento? Departamento)> ValidarAsync(CrearEmpleadoDto dto, int? id)
    {
        var departamento = await _context.Departamentos.FirstOrDefaultAsync(d => d.Id == dto.DepartamentoId);
        if (departamento == null)
            return (Resultado.Invalido("El departamento no existe", nameof(dto.DepartamentoId)), null);
        var empresaId = departamento.EmpresaId;

        if (await _context.Empleados.AnyAsync(e => e.EmpresaId == empresaId && e.CorreoElectronico == dto.CorreoElectronico && e.Id != id))
            return (Resultado.Invalido(id == null
                ? "Ya existe un empleado con ese correo electrónico"
                : "Ya existe otro empleado con ese correo electrónico", nameof(dto.CorreoElectronico)), null);

        if (!string.IsNullOrWhiteSpace(dto.NumeroDocumento))
        {
            var documento = NormalizarDocumento(dto.TipoDocumento, dto.NumeroDocumento);
            if (dto.TipoDocumento == TipoDocumento.Cedula && documento.Length != 11)
                return (Resultado.Invalido("La cédula debe tener 11 dígitos", nameof(dto.NumeroDocumento)), null);
            if (dto.TipoDocumento == TipoDocumento.Pasaporte && documento.Length < 5)
                return (Resultado.Invalido("El pasaporte debe tener al menos 5 caracteres", nameof(dto.NumeroDocumento)), null);
            if (await _context.Empleados.AnyAsync(e => e.EmpresaId == empresaId && e.NumeroDocumento == documento && e.Id != id))
                return (Resultado.Invalido("Ya existe un empleado con ese documento", nameof(dto.NumeroDocumento)), null);
        }

        if (dto.PuestoId.HasValue)
        {
            var puesto = await _context.Puestos.FirstOrDefaultAsync(p => p.Id == dto.PuestoId.Value);
            if (puesto == null)
                return (Resultado.Invalido("El puesto no existe", nameof(dto.PuestoId)), null);
            if (puesto.DepartamentoId != dto.DepartamentoId)
                return (Resultado.Invalido("El puesto no pertenece al departamento seleccionado", nameof(dto.PuestoId)), null);
        }

        if (dto.TipoContrato == TipoContrato.TiempoDefinido && dto.FechaFinContrato == null)
            return (Resultado.Invalido("Un contrato por tiempo definido necesita fecha de fin", nameof(dto.FechaFinContrato)), null);
        if (dto.FechaFinContrato.HasValue && dto.FechaFinContrato.Value.Date <= dto.FechaContratacion.Date)
            return (Resultado.Invalido("La fecha de fin del contrato debe ser posterior a la contratación", nameof(dto.FechaFinContrato)), null);

        if (dto.BancoId.HasValue != !string.IsNullOrWhiteSpace(dto.NumeroCuenta))
            return (Resultado.Invalido("Indique el banco y el número de cuenta juntos", nameof(dto.NumeroCuenta)), null);
        if (dto.BancoId.HasValue && !await _context.Bancos.AnyAsync(b => b.Id == dto.BancoId.Value))
            return (Resultado.Invalido("El banco no existe", nameof(dto.BancoId)), null);

        // shortcut: solo se valida el salario fijo mensual; la tarifa por hora se valida al calcular la nómina
        if (dto.TipoSalario == TipoSalario.FijoMensual)
        {
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == empresaId);
            var minimo = empresa == null ? null : await _parametros.SalarioMinimoAsync(empresa.Tamano, DateTime.Today);
            if (minimo.HasValue && dto.Salario < minimo.Value)
                return (Resultado.Invalido(
                    $"El salario es menor que el mínimo vigente para esta empresa (RD${Dinero(minimo.Value)})",
                    nameof(dto.Salario)), null);
        }

        return (null, departamento);
    }

    private static void Aplicar(Empleado empleado, CrearEmpleadoDto dto)
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
        empleado.TipoDocumento = dto.TipoDocumento;
        empleado.NumeroDocumento = string.IsNullOrWhiteSpace(dto.NumeroDocumento) ? null : NormalizarDocumento(dto.TipoDocumento, dto.NumeroDocumento);
        empleado.Nss = Limpiar(dto.Nss);
        empleado.Sexo = dto.Sexo;
        empleado.Afp = Limpiar(dto.Afp);
        empleado.Ars = Limpiar(dto.Ars);
        empleado.DependientesSfs = dto.DependientesSfs;
        empleado.TipoContrato = dto.TipoContrato;
        empleado.FechaFinContrato = dto.FechaFinContrato;
        empleado.TipoSalario = dto.TipoSalario;
        empleado.BancoId = dto.BancoId;
        empleado.NumeroCuenta = Limpiar(dto.NumeroCuenta);
        empleado.TipoCuenta = dto.BancoId.HasValue ? dto.TipoCuenta : null;
    }

    private void Registrar(Empleado empleado, TipoMovimiento tipo, string? anterior, string? nuevo) =>
        _context.MovimientosEmpleados.Add(new MovimientoEmpleado
        {
            EmpresaId = empleado.EmpresaId,
            Empleado = empleado,
            Fecha = DateTime.Now,
            Tipo = tipo,
            ValorAnterior = anterior,
            ValorNuevo = nuevo,
            UsuarioId = _contexto.UsuarioId,
            UsuarioNombre = _contexto.UsuarioNombre
        });

    private async Task<string?> NombrePuestoAsync(int? id) =>
        id == null ? null : await _context.Puestos.Where(p => p.Id == id).Select(p => p.Nombre).FirstOrDefaultAsync();

    private Task<string?> NombreDepartamentoAsync(int id) =>
        _context.Departamentos.Where(d => d.Id == id).Select(d => (string?)d.Nombre).FirstOrDefaultAsync();

    // Cédula: solo dígitos (se aceptan guiones al escribirla). Pasaporte: sin espacios, en mayúsculas.
    // shortcut: no se valida el dígito verificador de la cédula (hay cédulas antiguas válidas que no lo cumplen)
    internal static string NormalizarDocumento(TipoDocumento tipo, string valor) =>
        tipo == TipoDocumento.Cedula
            ? EmpresaService.SoloDigitos(valor)
            : valor.Replace(" ", "").Trim().ToUpperInvariant();

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string Dinero(decimal monto) => monto.ToString("N2", CultureInfo.InvariantCulture);
}
