namespace Empleados.Data.DTOs;

public class ReporteDepartamentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public int TotalEmpleados { get; set; }
    public int Activos { get; set; }
    public decimal NominaMensual { get; set; }
}
