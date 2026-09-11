using Empleados.Data.Entities;

namespace Empleados.Data.DTOs;

public class NotificacionDto
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; }
    public string TipoNombre => Tipo.ToString();
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? UsuarioDestinoId { get; set; }
}
