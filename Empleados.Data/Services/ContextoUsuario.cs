using Empleados.Data.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Empleados.Data.Services;

// Quién hace la operación y en qué empresa. El DbContext lo usa para filtrar los datos por empresa.
public interface IContextoUsuario
{
    // Sin petición HTTP (arranque, migraciones, tareas de fondo): no se filtra por empresa
    bool EsSistema { get; }
    string? UsuarioId { get; }
    string? UsuarioNombre { get; }
    // Empresa seleccionada y permitida al usuario; null = ninguna (no ve datos)
    int? EmpresaId { get; }
}

public class ContextoUsuarioHttp : IContextoUsuario
{
    public const string CabeceraEmpresa = "X-Empresa-Id";
    public const string CookieEmpresa = "EmpresaId";

    private readonly IHttpContextAccessor _http;
    private readonly DbContextOptions<EmpleadosDbContext> _opciones;
    private List<int>? _permitidas;
    private int? _empresaId;

    public ContextoUsuarioHttp(IHttpContextAccessor http, DbContextOptions<EmpleadosDbContext> opciones)
    {
        _http = http;
        _opciones = opciones;
    }

    public bool EsSistema => _http.HttpContext == null;
    public string? UsuarioId => _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UsuarioNombre => _http.HttpContext?.User.Identity?.Name;

    public int? EmpresaId
    {
        get
        {
            if (_permitidas == null)
                Resolver();
            return _empresaId;
        }
    }

    public IReadOnlyList<int> EmpresasPermitidas
    {
        get
        {
            if (_permitidas == null)
                Resolver();
            return _permitidas!;
        }
    }

    private void Resolver()
    {
        _permitidas = new List<int>();
        var usuarioId = UsuarioId;
        if (usuarioId == null)
            return;

        // Contexto sin filtro: se consulta antes de saber la empresa
        using (var db = new EmpleadosDbContext(_opciones))
        {
            _permitidas = db.UsuariosEmpresas
                .Where(u => u.UsuarioId == usuarioId && u.Empresa!.Activa)
                .Select(u => u.EmpresaId)
                .OrderBy(id => id)
                .ToList();
        }

        var request = _http.HttpContext!.Request;
        // La cabecera (API) es explícita: si pide una empresa no permitida, no se usa otra
        if (request.Headers.TryGetValue(CabeceraEmpresa, out var cabecera))
        {
            _empresaId = int.TryParse(cabecera, out var id) && _permitidas.Contains(id) ? id : null;
            return;
        }

        // La cookie (MVC) es la última elección del usuario; si ya no es válida, se usa la primera permitida
        if (int.TryParse(request.Cookies[CookieEmpresa], out var elegida) && _permitidas.Contains(elegida))
            _empresaId = elegida;
        else
            _empresaId = _permitidas.Count > 0 ? _permitidas[0] : null;
    }
}
