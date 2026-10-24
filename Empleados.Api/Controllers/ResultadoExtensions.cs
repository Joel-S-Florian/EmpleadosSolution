using Empleados.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

public static class ResultadoExtensions
{
    // Traduce un error de negocio a su respuesta HTTP
    public static IActionResult Error(this ControllerBase controller, Resultado resultado) => resultado.Tipo switch
    {
        TipoResultado.NoEncontrado => controller.NotFound(new { mensaje = resultado.Mensaje }),
        TipoResultado.Prohibido => controller.Forbid(),
        _ => controller.BadRequest(new { mensaje = resultado.Mensaje })
    };
}
