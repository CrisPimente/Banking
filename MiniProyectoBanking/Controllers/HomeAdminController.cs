using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniProyectoBanking.Core.Application.Interfaces.Services;
using MiniProyectoBanking.Middlewares;

namespace MiniProyectoBanking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeAdminController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IProductoService _productoService;
        private readonly ITransaccionService _transaccionService;
        private readonly ValidateUserSession _validateUserSession;

        public HomeAdminController(IUsuarioService usuarioService, IProductoService productoService, ITransaccionService transaccionService, ValidateUserSession validateUserSession)
        {
            _usuarioService = usuarioService;
            _productoService = productoService;
            _transaccionService = transaccionService;
            _validateUserSession = validateUserSession;
        }

        public async Task<IActionResult> Index()
        {
            var userType = _validateUserSession.GetUserType(); 

            if (userType == "Cliente")
            {
                TempData["ErrorMensaje"] = "No puedes acceder a esta sección.";
                return RedirectToAction("Index", "HomeUsuario");
            }

            var totalTransacciones = await _transaccionService.GetTotalTransacciones();
            var transaccionesHoy = await _transaccionService.GetTotalTransaccionesHoy();
            var totalUsuariosActivos = await _usuarioService.GetTotalUsuariosActivos();
            var totalUsuariosInactivos = await _usuarioService.GetTotalUsuariosInactivos();
            var totalProductos = await _productoService.GetTotalProductos();

            ViewData["TotalTransacciones"] = totalTransacciones;
            ViewData["TransaccionesHoy"] = transaccionesHoy;
            ViewData["TotalUsuariosActivos"] = totalUsuariosActivos;
            ViewData["TotalUsuariosInactivos"] = totalUsuariosInactivos;
            ViewData["TotalProductos"] = totalProductos;

            return View();
        }
    }
}