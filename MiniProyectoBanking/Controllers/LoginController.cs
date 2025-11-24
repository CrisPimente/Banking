using Microsoft.AspNetCore.Mvc;
using MiniProyectoBanking.Core.Application.Interfaces.Services;
using MiniProyectoBanking.Core.Application.ViewModels.Usuarios;
using MiniProyectoBanking.Core.Application.Helpers;
using MiniProyectoBanking.Core.Application.Dtos.Account;
using MiniProyectoBanking.Core.Application.Enums;
using MiniProyectoBanking.Middlewares;

namespace MiniProyectoBanking.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUsuarioService _usuarioService;

        public LoginController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [ServiceFilter(typeof(LoginAuthorize))]
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }

        [ServiceFilter(typeof(LoginAuthorize))]
        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            AuthenticationResponse userVm = await _usuarioService.LoginAsync(vm);
            if (userVm != null && !userVm.HasError)
            {
                // Guardar información del usuario en la sesión
                HttpContext.Session.Set<AuthenticationResponse>("usuario", userVm);
                if (userVm.Roles.Contains(Roles.Admin.ToString()))
                {
                    return RedirectToRoute(new { controller = "HomeAdmin", action = "Index" });
                }
                else
                {
                    return RedirectToRoute(new { controller = "HomeUsuario", action = "Index" });
                }
            }
            else
            {
                vm.HasError = userVm.HasError;
                vm.Error = userVm.Error;
                return View(vm);
            }
        }

        public async Task<IActionResult> LogOut()
        {
            await _usuarioService.SingoutAsyncs();
            HttpContext.Session.Remove("usuario");
            return RedirectToAction("Index", "Login");
        }
    }
}
