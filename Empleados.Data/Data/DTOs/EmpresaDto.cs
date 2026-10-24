using Empleados.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Empleados.Data.DTOs;

public class EmpresaDto
{
    // RNC de 9 dígitos, o cédula de 11 si es persona física
    [Required(ErrorMessage = "El RNC es obligatorio")]
    [RegularExpression(@"^[\d-]{9,13}$", ErrorMessage = "El RNC debe tener 9 dígitos (u 11 si es una cédula)")]
    public string Rnc { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria")]
    [StringLength(200)]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NombreComercial { get; set; }

    public TamanoEmpresa Tamano { get; set; } = TamanoEmpresa.Grande;

    [Range(0, 10, ErrorMessage = "La tasa SRL debe estar entre 0 y 10 %")]
    public decimal TasaRiesgoSrl { get; set; } = 1.20m;

    public FrecuenciaPago FrecuenciaPagoPredeterminada { get; set; } = FrecuenciaPago.Quincenal;

    [Range(1, 12, ErrorMessage = "El mes de cierre fiscal debe estar entre 1 y 12")]
    public int MesCierreFiscal { get; set; } = 12;

    [StringLength(255)]
    public string? Direccion { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
    [StringLength(255)]
    public string? CorreoElectronico { get; set; }

    public bool Activa { get; set; } = true;

    public static EmpresaDto Desde(Empresa e) => new()
    {
        Rnc = e.Rnc,
        RazonSocial = e.RazonSocial,
        NombreComercial = e.NombreComercial,
        Tamano = e.Tamano,
        TasaRiesgoSrl = e.TasaRiesgoSrl,
        FrecuenciaPagoPredeterminada = e.FrecuenciaPagoPredeterminada,
        MesCierreFiscal = e.MesCierreFiscal,
        Direccion = e.Direccion,
        Telefono = e.Telefono,
        CorreoElectronico = e.CorreoElectronico,
        Activa = e.Activa
    };
}
