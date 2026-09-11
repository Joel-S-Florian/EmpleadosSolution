namespace Empleados.Data.Entities;

public enum TipoNotificacion
{
    Info,
    Warning,
    Success
}

public class Notificacion
{
    public int Id { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Info;
    public bool Leida { get; set; } = false;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public string? UsuarioDestinoId { get; set; }
}
