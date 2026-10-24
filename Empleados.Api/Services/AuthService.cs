using Empleados.Data.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Empleados.Api.Services;

public class AuthService
{
    private readonly EmpleadosDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(EmpleadosDbContext context, UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<string?> LoginAsync(string correo, string contrasena)
    {
        // Buscar usuario usando Identity
        var usuario = await _userManager.FindByEmailAsync(correo);
        if (usuario == null)
            return null;

        // Verificar contraseña usando Identity
        if (!await _userManager.CheckPasswordAsync(usuario, contrasena))
            return null;

        // La API es solo para Admin y RRHH: los usuarios sin rol o del portal del empleado no reciben token
        var roles = await _userManager.GetRolesAsync(usuario);
        var rol = roles.Contains("Admin") ? "Admin" : roles.Contains("RRHH") ? "RRHH" : null;
        if (rol == null)
            return null;

        // Generar token JWT
        return GenerarToken(usuario, rol);
    }

    private string GenerarToken(IdentityUser usuario, string rol)
    {
        // Jwt:Key se valida al arrancar en Program.cs
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.UserName ?? usuario.Email ?? string.Empty),
            new Claim(ClaimTypes.Email, usuario.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, rol)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "EmpleadosApi",
            audience: _configuration["Jwt:Audience"] ?? "EmpleadosApiClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}