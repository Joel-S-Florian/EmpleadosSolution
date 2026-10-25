namespace Empleados.Data.Entities;

public class Puesto
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    // Rango salarial mensual del puesto (opcional); el salario fijo del empleado debe quedar dentro
    public decimal? SalarioMinimo { get; set; }
    public decimal? SalarioMaximo { get; set; }

    public int DepartamentoId { get; set; }
    public virtual Departamento? Departamento { get; set; }

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}
