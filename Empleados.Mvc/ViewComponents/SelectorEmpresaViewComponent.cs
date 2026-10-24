using Empleados.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace Empleados.Mvc.ViewComponents;

// Empresa actual en la barra superior; con más de una empresa permite cambiarla
public class SelectorEmpresaViewComponent : ViewComponent
{
    private readonly EmpresaService _empresas;
    private readonly IContextoUsuario _contexto;

    public SelectorEmpresaViewComponent(EmpresaService empresas, IContextoUsuario contexto)
    {
        _empresas = empresas;
        _contexto = contexto;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var empresas = await _empresas.MisEmpresasAsync();
        ViewBag.EmpresaActualId = _contexto.EmpresaId;
        return View(empresas);
    }
}
