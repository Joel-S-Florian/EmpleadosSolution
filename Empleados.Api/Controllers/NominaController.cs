using Empleados.Data.DTOs;
using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize(Roles = "Admin,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class NominaController : ControllerBase
{
    private readonly NominaService _nomina;

    public NominaController(NominaService nomina)
    {
        _nomina = nomina;
    }

    // POST: api/Nomina/simular - calcula el recibo de un empleado en un período, sin guardar nada
    [HttpPost("simular")]
    public async Task<IActionResult> Simular([FromBody] SimulacionNominaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resultado = await _nomina.SimularAsync(dto);
        return resultado.Ok ? Ok(resultado.Valor) : this.Error(resultado);
    }
}
