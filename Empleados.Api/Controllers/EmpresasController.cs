using Empleados.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly EmpresaService _empresas;
    private readonly IContextoUsuario _contexto;

    public EmpresasController(EmpresaService empresas, IContextoUsuario contexto)
    {
        _empresas = empresas;
        _contexto = contexto;
    }

    // GET: api/Empresas - empresas del usuario; el Id se envía en la cabecera X-Empresa-Id
    [HttpGet]
    public async Task<IActionResult> GetMisEmpresas()
    {
        var empresas = await _empresas.MisEmpresasAsync();
        return Ok(new
        {
            empresaActualId = _contexto.EmpresaId,
            cabecera = ContextoUsuarioHttp.CabeceraEmpresa,
            empresas = empresas.Select(e => new { e.Id, e.Rnc, e.RazonSocial, e.NombreComercial })
        });
    }
}
