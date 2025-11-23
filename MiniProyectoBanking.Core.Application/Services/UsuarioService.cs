using MiniProyectoBanking.Core.Application.Interfaces.Services;
using MiniProyectoBanking.Core.Application.ViewModels.Usuarios;
using AutoMapper;
using MiniProyectoBanking.Core.Application.Dtos.Account;

namespace MiniProyectoBanking.Core.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IProductoService _productoService;

        public UsuarioService(IMapper mapper, IAccountService accountService)
        {
            _accountService = accountService;
            _mapper = mapper;
        }

        public async Task<AuthenticationResponse> LoginAsyncs(LoginViewModel vm)
        {
            AuthenticationRequest Loginrequest = _mapper.Map<AuthenticationRequest>(vm);
            AuthenticationResponse userResponse = await _accountService.AuthenticateAsync(Loginrequest);

            return userResponse;
        }

        public async Task<RegisterResponse> RegisterAsyncs(SaveUsuarioViewModel vm, string origin)
        {
            RegisterRequest RegisterRequest = _mapper.Map<RegisterRequest>(vm);
            return await _accountService.RegisterBasicUserAsync(RegisterRequest, origin);
        }

        public async Task<RegisterResponse> EditAsync(EditUsuarioViewModel vm, string origin)
        {
            RegisterResponse response = new()
            {
                HasError = false
            };

            var userDto = await _accountService.GetByIdAsync(vm.Id);
            if (userDto == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            // Mapear UserDto a RegisterRequest si es necesario
            var registerRequest = new RegisterRequest
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                Nombre = userDto.Nombre,
                Apellido = userDto.Apellido,
                Cedula = userDto.Cedula,
                Rol = Enum.Parse<MiniProyectoBanking.Core.Application.Enums.Roles>(userDto.Tipo.ToString())
            };

            return await _accountService.EditUserAsync(registerRequest, origin);
        }

        public async Task<EditUsuarioViewModel> GetByIdAsync(string userId)
        {
            var userDto = await _accountService.GetByIdAsync(userId);
            if (userDto == null)
            {
                return null;
            }

            var editViewModel = _mapper.Map<EditUsuarioViewModel>(userDto);
            return editViewModel;
        }
        public async Task<ConfirmEmailResponse> ConfirmUserEmailAsync(EditUsuarioViewModel vm)
        {
            return await _accountService.ConfirmUserEmailAsync(vm);
        }

        /*
                public async Task<UsuarioViewModel> GetByNombreUsuario(string nombreUsuario)
                {
                    var usuario = await _usuarioRepository.GetByNombreUsuario(nombreUsuario);
                    if (usuario == null) return null;

                    return new UsuarioViewModel
                    {
                        Id = usuario.Id,
                        Nombre = usuario.Nombre,
                        Apellido = usuario.Apellido,
                        Correo = usuario.Correo,
                        Cedula = usuario.Cedula,
                        NombreUsuario = usuario.NombreUsuario,
                        Estado= usuario.Estado,
                        Tipo= usuario.Tipo,
                    };
                }
        */
        public async Task<int> GetTotalUsuariosActivos()
        {
            return await _accountService.CountUsuariosByEstado(true);
        }

        public async Task<int> GetTotalUsuariosInactivos()
        {
            return await _accountService.CountUsuariosByEstado(false);
        }

        public async Task SingoutAsyncs()
        {
            await _accountService.SignOutAsync();
        }

    }

}