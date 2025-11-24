using MiniProyectoBanking.Core.Application.Dtos.Account;
using MiniProyectoBanking.Core.Application.ViewModels.Usuarios;

namespace MiniProyectoBanking.Core.Application.Interfaces.Services
{
    public interface IUsuarioService
    {
        Task<AuthenticationResponse> LoginAsync(LoginViewModel vm);
        Task SingoutAsyncs();

        // Task<UsuarioViewModel> GetByNombreUsuario(string nombreUsuario);
        Task<RegisterResponse> EditAsync(EditUsuarioViewModel vm, string origin);
        Task<EditUsuarioViewModel> GetByIdAsync(string userId);
        Task<ConfirmEmailResponse> ConfirmUserEmailAsync(EditUsuarioViewModel vm);
        Task<RegisterResponse> RegisterAsync(SaveUsuarioViewModel vm, string origin);
        Task<int> GetTotalUsuariosActivos();
        Task<int> GetTotalUsuariosInactivos();
    }
}
