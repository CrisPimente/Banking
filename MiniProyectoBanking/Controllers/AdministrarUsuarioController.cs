using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MiniProyectoBanking.Core.Application.Interfaces.Services;
using MiniProyectoBanking.Core.Application.ViewModels.Usuarios;
using MiniProyectoBanking.Middlewares;
using MiniProyectoBanking.Core.Application.Dtos.Account;
using Microsoft.AspNetCore.Authorization;

namespace MiniProyectoBanking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdministrarUsuarioController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IProductoService _productoService;
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly ValidateUserSession _validateUserSession;

        public AdministrarUsuarioController(IUsuarioService usuarioService, IProductoService productoService, IMapper mapper, ValidateUserSession validateUserSession, IAccountService accountService)
        {
            _usuarioService = usuarioService;
            _productoService = productoService;
            _mapper = mapper;
            _validateUserSession = validateUserSession;
            _accountService = accountService;
        }
        public async Task<IActionResult> Index()
        {
            var usuarios = await _accountService.GetAllUsuarios();
            var usuarioViewModels = _mapper.Map<List<UsuarioViewModel>>(usuarios);

            return View(usuarioViewModels);
        }

        public IActionResult SaveUsuario()
        {
            return View(new SaveUsuarioViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> SaveUsuario(SaveUsuarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            var origin = Request.Headers["origin"];
            RegisterResponse response = await _usuarioService.RegisterAsync(vm, origin);
            if (response.HasError)
            {
                vm.HasError = response.HasError;
                vm.Error = response.Error;

                return View(vm);
            }
            return RedirectToRoute(new { controller = "AdministrarUsuario", action = "Index" });
        }

        public async Task<IActionResult> EditUsuario(string id)
        {
            var editViewModel = await _usuarioService.GetByIdAsync(id);
            if (editViewModel == null)
            {
                return NotFound();
            }

            return View(editViewModel);
        }


        [HttpPost]
        public async Task<IActionResult> EditUsuario(EditUsuarioViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var origin = Request.Headers["origin"];
            var registerRequest = _mapper.Map<RegisterRequest>(vm);
            registerRequest.MontoAdicional = vm.MontoAdicional;

            var response = await _accountService.EditUserAsync(registerRequest, origin);

            if (response.HasError)
            {
                vm.HasError = response.HasError;
                vm.Error = response.Error;
                return View(vm);
            }

            return RedirectToAction("Index");
        }
        
        public async Task<IActionResult> Activacion(string id)
        {
            var editViewModel = await _usuarioService.GetByIdAsync(id);
            if (editViewModel == null)
            {
                return NotFound();
            }

            return View(editViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Activacion(EditUsuarioViewModel vm)
        {
            var response = await _usuarioService.ConfirmUserEmailAsync(vm);

            if (response.HasError)
            {
                ViewBag.ErrorMessage = response.Error;
                return View();
            }

            return RedirectToAction("Index");
        }
    }
}
