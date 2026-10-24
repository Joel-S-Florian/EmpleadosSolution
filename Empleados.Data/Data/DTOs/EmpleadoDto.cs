namespace Empleados.Data.DTOs;

public class EmpleadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;
    public decimal Salario { get; set; }
    public DateTime FechaContratacion { get; set; }
    public DateTime? FechaTerminacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? FotoRuta { get; set; }
    public string NombreDepartamento { get; set; } = string.Empty;
    public int? PuestoId { get; set; }
    public string? NombrePuesto { get; set; }
    public int EmpresaId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public string? NumeroDocumento { get; set; }
    public string? Nss { get; set; }
    public string? Sexo { get; set; }
    public string? Afp { get; set; }
    public string? Ars { get; set; }
    public int DependientesSfs { get; set; }
    public string TipoContrato { get; set; } = string.Empty;
    public DateTime? FechaFinContrato { get; set; }
    public string FrecuenciaPago { get; set; } = string.Empty;
    public string TipoSalario { get; set; } = string.Empty;
    public int? BancoId { get; set; }
    public string? NombreBanco { get; set; }
    public string? NumeroCuenta { get; set; }
    public string? TipoCuenta { get; set; }

    // Requiere Departamento, PuestoNav y Banco cargados
    public static EmpleadoDto Desde(Entities.Empleado e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Apellido = e.Apellido,
        FechaNacimiento = e.FechaNacimiento,
        Direccion = e.Direccion,
        Telefono = e.Telefono,
        CorreoElectronico = e.CorreoElectronico,
        Salario = e.Salario,
        FechaContratacion = e.FechaContratacion,
        FechaTerminacion = e.FechaTerminacion,
        Estado = e.Estado.ToString(),
        FotoRuta = e.FotoRuta,
        NombreDepartamento = e.Departamento?.Nombre ?? "",
        PuestoId = e.PuestoId,
        NombrePuesto = e.PuestoNav?.Nombre,
        EmpresaId = e.EmpresaId,
        TipoDocumento = e.TipoDocumento.ToString(),
        NumeroDocumento = e.NumeroDocumento,
        Nss = e.Nss,
        Sexo = e.Sexo?.ToString(),
        Afp = e.Afp,
        Ars = e.Ars,
        DependientesSfs = e.DependientesSfs,
        TipoContrato = e.TipoContrato.ToString(),
        FechaFinContrato = e.FechaFinContrato,
        FrecuenciaPago = e.FrecuenciaPago.ToString(),
        TipoSalario = e.TipoSalario.ToString(),
        BancoId = e.BancoId,
        NombreBanco = e.Banco?.Nombre,
        NumeroCuenta = e.NumeroCuenta,
        TipoCuenta = e.TipoCuenta?.ToString()
    };
}