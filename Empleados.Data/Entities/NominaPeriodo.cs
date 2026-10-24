using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

// Borrador -> Calculada -> Aprobada -> Pagada -> Cerrada. Desde Pagada no se modifica nada.
public enum EstadoNomina
{
    Borrador,
    Calculada,
    Aprobada,
    Pagada,
    Cerrada
}

// Nómina de una empresa para un período (quincena o mes)
public class NominaPeriodo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    // 1 o 2 si es quincenal; 0 si es mensual
    public int Quincena { get; set; }
    public FrecuenciaPago Frecuencia { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoNomina Estado { get; set; } = EstadoNomina.Borrador;

    public decimal TotalIngresos { get; set; }
    public decimal TotalDeducciones { get; set; }
    public decimal TotalNeto { get; set; }
    public decimal TotalAportesPatronales { get; set; }

    public DateTime FechaCreacion { get; set; }
    public string? CreadaPor { get; set; }
    public DateTime? FechaCalculo { get; set; }
    public DateTime? FechaAprobacion { get; set; }
    public string? AprobadaPor { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? PagadaPor { get; set; }
    public DateTime? FechaCierre { get; set; }

    public virtual ICollection<ReciboPago> Recibos { get; set; } = new List<ReciboPago>();

    public string Descripcion => Frecuencia == FrecuenciaPago.Mensual
        ? $"Mensual {Mes:00}/{Anio}"
        : $"{Quincena}.ª quincena {Mes:00}/{Anio}";

    public bool Editable => Estado is EstadoNomina.Borrador or EstadoNomina.Calculada;
}

// Recibo de un empleado en una nómina. Guarda una copia de los datos del empleado al calcular.
public class ReciboPago
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int NominaId { get; set; }
    public virtual NominaPeriodo? Nomina { get; set; }
    public int EmpleadoId { get; set; }
    public virtual Empleado? Empleado { get; set; }

    // Copia al momento del cálculo
    public string NombreEmpleado { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string? Nss { get; set; }
    public string? Departamento { get; set; }
    public string? Puesto { get; set; }
    public decimal Salario { get; set; }
    public TipoSalario TipoSalario { get; set; }
    public string? Banco { get; set; }
    public string? NumeroCuenta { get; set; }

    // Novedades del período (se escriben antes de calcular)
    public decimal HorasTrabajadas { get; set; }
    public decimal DiasAusenciaSinPago { get; set; }
    // Días que descuentan las incidencias aprobadas (se recalcula; no se edita)
    public decimal DiasIncidencias { get; set; }
    public decimal HorasExtra { get; set; }
    public decimal HorasExtraExceso { get; set; }
    public decimal HorasNocturnas { get; set; }
    public decimal HorasFeriado { get; set; }
    public virtual ICollection<NovedadNomina> Novedades { get; set; } = new List<NovedadNomina>();

    // Resultado del cálculo
    public decimal TotalIngresos { get; set; }
    public decimal TotalDeduccionesLey { get; set; }
    public decimal TotalDeduccionesInternas { get; set; }
    public decimal Neto { get; set; }
    public decimal TotalAportesPatronales { get; set; }
    public decimal BaseTss { get; set; }
    public decimal BaseIsrMensual { get; set; }
    // AcumuladoMes del motor al cerrar este recibo (lo usa la 2.ª quincena), como JSON
    public string? AcumuladoJson { get; set; }
    public string? ExplicacionJson { get; set; }
    public string? AdvertenciasJson { get; set; }
    // Impiden aprobar la nómina (por ejemplo, falta la cédula)
    public string? ErroresJson { get; set; }
    public virtual ICollection<LineaReciboPago> Lineas { get; set; } = new List<LineaReciboPago>();
}

public class LineaReciboPago
{
    public int Id { get; set; }
    public int ReciboId { get; set; }
    public int Orden { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal? Cantidad { get; set; }
    public bool CotizaTss { get; set; }
    public bool GravaIsr { get; set; }
    public int? PrestamoId { get; set; }
}

public enum TipoNovedad
{
    Ingreso,
    [Display(Name = "Deducción")] Deduccion
}

// Ingreso o deducción de un solo período (bono, comisión, ajuste, descuento puntual)
public class NovedadNomina
{
    public int Id { get; set; }
    public int ReciboId { get; set; }
    public TipoNovedad Tipo { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public bool CotizaTss { get; set; } = true;
    public bool GravaIsr { get; set; } = true;
    public int Prioridad { get; set; }
}

// Préstamo o descuento en cuotas: cada nómina pagada descuenta la cuota (o lo que alcance) del saldo
public class Prestamo
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public int EmpleadoId { get; set; }
    public virtual Empleado? Empleado { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal Saldo { get; set; }
    public int Prioridad { get; set; }
    public DateTime FechaInicio { get; set; }
    // Solo en el último pago del mes (2.ª quincena o mensual)
    public bool SoloFinDeMes { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
}
