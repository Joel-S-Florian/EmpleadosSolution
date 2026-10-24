using Empleados.Data.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Empleados.Mvc.Controllers;

public static class ResultadoExtensions
{
    // Muestra el error de negocio junto al campo que lo causó (o arriba del formulario si no hay campo)
    public static void AgregarError(this ModelStateDictionary modelState, Resultado resultado) =>
        modelState.AddModelError(resultado.Campo ?? string.Empty, resultado.Mensaje ?? "Operación no válida");
}
