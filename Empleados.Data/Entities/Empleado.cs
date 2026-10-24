using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.Entities;

public enum EstadoEmpleado
{
    Activo,
    Inactivo,
    Despedido
}

public enum TipoDocumento
{
    [Display(Name = "Cédula")] Cedula,
    Pasaporte
}

public enum Sexo
{
    Masculino,
    Femenino
}

public enum TipoContrato
{
    [Display(Name = "Por tiempo indefinido")] Indefinido,
    [Display(Name = "Por tiempo definido")] TiempoDefinido,
    [Display(Name = "Para obra o servicio determinado")] ObraServicio
}

public enum TipoSalario
{
    [Display(Name = "Fijo mensual")] FijoMensual,
    [Display(Name = "Por hora")] PorHora
}

public enum TipoCuenta
{
    Ahorro,
    Corriente
}

public class Empleado
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    // Mensual si TipoSalario = FijoMensual; tarifa por hora si TipoSalario = PorHora
    public decimal Salario { get; set; }
    public DateTime FechaContratacion { get; set; }
    public DateTime? FechaTerminacion { get; set; }
    public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.Activo;
    public string? FotoRuta { get; set; }

    // Datos para nómina, TSS y DGII
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Cedula;
    public string? NumeroDocumento { get; set; }
    public string? Nss { get; set; }
    public Sexo? Sexo { get; set; }
    public string? Afp { get; set; }
    public string? Ars { get; set; }
    public int DependientesSfs { get; set; }
    public TipoContrato TipoContrato { get; set; } = TipoContrato.Indefinido;
    public DateTime? FechaFinContrato { get; set; }
    public FrecuenciaPago FrecuenciaPago { get; set; } = FrecuenciaPago.Quincenal;
    public TipoSalario TipoSalario { get; set; } = TipoSalario.FijoMensual;
    public int? BancoId { get; set; }
    public virtual Banco? Banco { get; set; }
    public string? NumeroCuenta { get; set; }
    public TipoCuenta? TipoCuenta { get; set; }

    // Usuario del portal del empleado (rol Empleado)
    public string? UsuarioId { get; set; }

    public int DepartamentoId { get; set; }
    public virtual Departamento? Departamento { get; set; }

    public int? PuestoId { get; set; }
    public virtual Puesto? PuestoNav { get; set; }
}
