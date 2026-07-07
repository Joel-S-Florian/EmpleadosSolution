using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El correo es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Contrasena { get; set; } = string.Empty;
}
