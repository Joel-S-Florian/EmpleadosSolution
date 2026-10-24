using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Empleados.Mvc.Models;

public static class EnumTexto
{
    // Nombre para mostrar: el [Display(Name)] del valor, o su nombre si no tiene
    public static string Texto(this Enum valor) =>
        valor.GetType().GetField(valor.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
}
