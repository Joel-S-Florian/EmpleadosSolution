namespace Empleados.Data.DTOs;

public class PuestoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int DepartamentoId { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;

    public static PuestoDto Desde(Entities.Puesto p) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        DepartamentoId = p.DepartamentoId,
        NombreDepartamento = p.Departamento?.Nombre ?? ""
    };
}
