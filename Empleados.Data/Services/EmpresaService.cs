using Empleados.Data.Data;
using Empleados.Data.DTOs;
using Empleados.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Empleados.Data.Services;

public class EmpresaService
{
    private readonly EmpleadosDbContext _context;
    private readonly IContextoUsuario _contexto;

    public EmpresaService(EmpleadosDbContext context, IContextoUsuario contexto)
    {
        _context = context;
        _contexto = contexto;
    }

    public Task<List<Empresa>> ListarAsync() =>
        _context.Empresas.Include(e => e.Usuarios).OrderBy(e => e.RazonSocial).ToListAsync();

    // Empresas activas a las que tiene acceso el usuario actual (para el selector)
    public Task<List<Empresa>> MisEmpresasAsync() =>
        _context.Empresas
            .Where(e => e.Activa && e.Usuarios.Any(u => u.UsuarioId == _contexto.UsuarioId))
            .OrderBy(e => e.RazonSocial)
            .ToListAsync();

    public Task<Empresa?> ObtenerAsync(int id) =>
        _context.Empresas.Include(e => e.Usuarios).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Resultado<Empresa>> CrearAsync(EmpresaDto dto)
    {
        var error = await ValidarAsync(dto, null);
        if (error != null)
            return Resultado<Empresa>.Fallo(error);

        var empresa = new Empresa();
        Aplicar(empresa, dto);
        // Quien la crea tiene acceso desde el inicio
        if (_contexto.UsuarioId != null)
            empresa.Usuarios.Add(new UsuarioEmpresa { UsuarioId = _contexto.UsuarioId });
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();
        return Resultado<Empresa>.Con(empresa, "Empresa creada correctamente");
    }

    public async Task<Resultado> ActualizarAsync(int id, EmpresaDto dto)
    {
        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);
        if (empresa == null)
            return Resultado.NoEncontrado("Empresa no encontrada");

        var error = await ValidarAsync(dto, id);
        if (error != null)
            return error;

        Aplicar(empresa, dto);
        await _context.SaveChangesAsync();
        return Resultado.Exito("Empresa actualizada correctamente");
    }

    // Reemplaza la lista de usuarios con acceso a la empresa
    public async Task<Resultado> AsignarUsuariosAsync(int id, IEnumerable<string> usuarioIds)
    {
        var empresa = await _context.Empresas.Include(e => e.Usuarios).FirstOrDefaultAsync(e => e.Id == id);
        if (empresa == null)
            return Resultado.NoEncontrado("Empresa no encontrada");

        var nuevos = usuarioIds.Distinct().ToHashSet();
        var existentes = await _context.Users.Where(u => nuevos.Contains(u.Id)).Select(u => u.Id).ToListAsync();
        if (existentes.Count != nuevos.Count)
            return Resultado.Invalido("Algún usuario no existe");

        foreach (var acceso in empresa.Usuarios.Where(u => !nuevos.Contains(u.UsuarioId)).ToList())
            empresa.Usuarios.Remove(acceso);
        foreach (var usuarioId in nuevos.Where(n => empresa.Usuarios.All(u => u.UsuarioId != n)))
            empresa.Usuarios.Add(new UsuarioEmpresa { UsuarioId = usuarioId, EmpresaId = id });

        await _context.SaveChangesAsync();
        return Resultado.Exito("Accesos actualizados");
    }

    private async Task<Resultado?> ValidarAsync(EmpresaDto dto, int? id)
    {
        var rnc = SoloDigitos(dto.Rnc);
        if (rnc.Length != 9 && rnc.Length != 11)
            return Resultado.Invalido("El RNC debe tener 9 dígitos (u 11 si es una cédula)", nameof(dto.Rnc));
        if (await _context.Empresas.AnyAsync(e => e.Rnc == rnc && e.Id != id))
            return Resultado.Invalido("Ya existe una empresa con ese RNC", nameof(dto.Rnc));
        return null;
    }

    private static void Aplicar(Empresa empresa, EmpresaDto dto)
    {
        empresa.Rnc = SoloDigitos(dto.Rnc);
        empresa.RazonSocial = dto.RazonSocial.Trim();
        empresa.NombreComercial = string.IsNullOrWhiteSpace(dto.NombreComercial) ? null : dto.NombreComercial.Trim();
        empresa.Tamano = dto.Tamano;
        empresa.TasaRiesgoSrl = dto.TasaRiesgoSrl;
        empresa.FrecuenciaPagoPredeterminada = dto.FrecuenciaPagoPredeterminada;
        empresa.MesCierreFiscal = dto.MesCierreFiscal;
        empresa.Direccion = dto.Direccion;
        empresa.Telefono = dto.Telefono;
        empresa.CorreoElectronico = dto.CorreoElectronico;
        empresa.Activa = dto.Activa;
    }

    internal static string SoloDigitos(string? valor) => new((valor ?? "").Where(char.IsDigit).ToArray());
}
