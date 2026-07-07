using Empleados.Api.Services;
using Empleados.Data.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST: api/auth/login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var token = await _authService.LoginAsync(dto.Correo, dto.Contrasena);

        if (token == null)
            return Unauthorized(new { mensaje = "Credenciales inválidas" });

        return Ok(new { token });
    }
}
