namespace Empleados.Data.Entities;

public enum EstadoEnvio
{
    Enviado,
    Fallido
}

// Registro de cada correo enviado (recibos de pago, acceso al portal)
public class EnvioCorreo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int? ReciboId { get; set; }
    public int? EmpleadoId { get; set; }
    public string Destino { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public EstadoEnvio Estado { get; set; }
    public string? Error { get; set; }
    public DateTime Fecha { get; set; }
}
