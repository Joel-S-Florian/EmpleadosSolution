using Empleados.Nomina;
using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

public enum EstadoLiquidacion
{
    Borrador,
    Pagada
}

// Prestaciones laborales al terminar el contrato. Al pagarla, el empleado queda despedido y sus préstamos saldados.
public class Liquidacion
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }

    public CausaTerminacion Causa { get; set; }
    public DateTime FechaIngreso { get; set; }
    public DateTime FechaSalida { get; set; }
    public bool PreavisoOtorgado { get; set; }
    public decimal SalarioPromedio { get; set; }
    public decimal SalarioDiario { get; set; }
    public string TiempoServicio { get; set; } = string.Empty;
    public decimal MontoAcordado { get; set; }

    public decimal TotalIngresos { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal Neto { get; set; }
    // Lista de LineaLiquidacion
    public string LineasJson { get; set; } = "[]";
    public string? ExplicacionJson { get; set; }
    public string? AdvertenciasJson { get; set; }
    [StringLength(500)]
    public string? Comentario { get; set; }

    public EstadoLiquidacion Estado { get; set; }
    public string? CreadaPor { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? PagadaPor { get; set; }
    public DateTime? FechaPago { get; set; }
}

// Ingreso (prestación) o deducción (saldo de préstamo) de una liquidación
public record LineaLiquidacion(bool EsIngreso, string Codigo, string Concepto, decimal Dias, decimal Monto, int? PrestamoId = null);
