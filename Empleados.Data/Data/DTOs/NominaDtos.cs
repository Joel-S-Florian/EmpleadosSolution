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

public class IncidenciaDto
{
    [Required(ErrorMessage = "Seleccione un empleado")]
    public int EmpleadoId { get; set; }

    public TipoIncidencia Tipo { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime FechaFin { get; set; } = DateTime.Today;

    // Vacío: se cuentan los días laborables entre las fechas
    [Range(0.5, 366, ErrorMessage = "Los días deben estar entre 0.5 y 366")]
    public decimal? Dias { get; set; }

    // Solo retardos
    [Range(0, 24, ErrorMessage = "Las horas deben estar entre 0 y 24")]
    public decimal Horas { get; set; }

    // Vacío: según el tipo (faltas, retardos y permisos sin goce se descuentan)
    public bool? Descontar { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}

public class VacacionesColectivasDto
{
    public DateTime FechaInicio { get; set; } = DateTime.Today;
    public DateTime FechaFin { get; set; } = DateTime.Today;

    // Vacío: todos los empleados activos de la empresa
    public int? DepartamentoId { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}

public class LiquidacionDto
{
    [Required(ErrorMessage = "Seleccione un empleado")]
    public int EmpleadoId { get; set; }

    public Empleados.Nomina.CausaTerminacion Causa { get; set; }

    public DateTime FechaSalida { get; set; } = DateTime.Today;

    // El empleador avisó con el tiempo de ley (no se paga preaviso)
    public bool PreavisoOtorgado { get; set; }

    // Vacío: promedio del último año según las nóminas registradas
    [Range(0.01, 99999999.99, ErrorMessage = "El salario promedio debe ser mayor que 0")]
    public decimal? SalarioPromedio { get; set; }

    // Vacío: los días disponibles según el saldo de vacaciones
    [Range(0, 366, ErrorMessage = "Los días de vacaciones deben estar entre 0 y 366")]
    public decimal? DiasVacacionesPendientes { get; set; }

    // Mutuo acuerdo u otros montos pactados
    [Range(0, 99999999.99)]
    public decimal MontoAcordado { get; set; }

    [StringLength(500)]
    public string? Comentario { get; set; }
}
