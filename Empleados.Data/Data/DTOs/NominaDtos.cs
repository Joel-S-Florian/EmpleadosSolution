using Empleados.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class CrearNominaDto
{
    [Range(2020, 2100, ErrorMessage = "Año fuera de rango")]
    public int Anio { get; set; } = DateTime.Today.Year;

    [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12")]
    public int Mes { get; set; } = DateTime.Today.Month;

    public FrecuenciaPago Frecuencia { get; set; } = FrecuenciaPago.Quincenal;

    // Solo para quincenal
    [Range(1, 2, ErrorMessage = "La quincena debe ser 1 o 2")]
    public int Quincena { get; set; } = DateTime.Today.Day <= 15 ? 1 : 2;
}

// Novedades de un recibo: horas, ausencias e ingresos o deducciones de este período
public class NovedadesReciboDto
{
    [Range(0, 1000, ErrorMessage = "Horas fuera de rango")]
    public decimal HorasTrabajadas { get; set; }

    [Range(0, 31, ErrorMessage = "Días fuera de rango")]
    public decimal DiasAusenciaSinPago { get; set; }

    [Range(0, 500, ErrorMessage = "Horas fuera de rango")]
    public decimal HorasExtra { get; set; }

    [Range(0, 500, ErrorMessage = "Horas fuera de rango")]
    public decimal HorasExtraExceso { get; set; }

    [Range(0, 500, ErrorMessage = "Horas fuera de rango")]
    public decimal HorasNocturnas { get; set; }

    [Range(0, 500, ErrorMessage = "Horas fuera de rango")]
    public decimal HorasFeriado { get; set; }

    public List<IngresoSimuladoDto> OtrosIngresos { get; set; } = new();
    public List<DeduccionSimuladaDto> Deducciones { get; set; } = new();
}

public class PrestamoDto
{
    [Required(ErrorMessage = "Seleccione un empleado")]
    public int EmpleadoId { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [StringLength(100)]
    public string Concepto { get; set; } = string.Empty;

    [Range(0.01, 99999999.99, ErrorMessage = "El monto total debe ser mayor que 0")]
    public decimal MontoTotal { get; set; }

    [Range(0.01, 99999999.99, ErrorMessage = "La cuota debe ser mayor que 0")]
    public decimal MontoCuota { get; set; }

    [Range(0, 100)]
    public int Prioridad { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.Today;

    // Descontar solo en el último pago del mes
    public bool SoloFinDeMes { get; set; }
}
