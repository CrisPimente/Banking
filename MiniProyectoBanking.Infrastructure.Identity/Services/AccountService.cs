using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniProyectoBanking.Core.Application.Dtos.Account;
using MiniProyectoBanking.Core.Application.Enums;
using MiniProyectoBanking.Core.Application.Interfaces.Services;
using MiniProyectoBanking.Core.Application.ViewModels.Usuarios;
using MiniProyectoBanking.Infrastructure.Identity.Entities;
using AutoMapper;
using MiniProyectoBanking.Core.Application.ViewModels.Productos;
using System.Diagnostics.Contracts;

namespace MiniProyectoBanking.Infrastructure.Identity.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IProductoService _productoService;
        private readonly IMapper _mapper;

        public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IProductoService productoService, IMapper mapper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _productoService = productoService;
            _mapper = mapper;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request)
        {
            AuthenticationResponse response = new()
            {
                HasError = false
            };

            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                response.HasError = true;
                response.Error = $"No existe una cuenta con el usuario: {request.Username}";
                return response;
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, request.Password, false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = $"Credenciales incorrectas para: {request.Username}";
                return response;
            }
            if (!user.EmailConfirmed)
            {
                response.HasError = true;
                response.Error = $"La cuenta: {request.Username} no esta confirmada, por favor contactar con administracion";
                return response;
            }

            response.Id = user.Id;
            response.Email = user.Email;
            response.UserName = user.UserName;

            var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);

            response.Roles = rolesList.ToList();
            response.IsVerified = user.EmailConfirmed;

            return response;
        }

        public async Task<RegisterResponse> RegisterBasicUserAsync(RegisterRequest request, string origin)
        {
            RegisterResponse response = new()
            {
                HasError = false
            };

            var UserSameName = await _userManager.FindByNameAsync(request.UserName);
            if (UserSameName != null)
            {
                response.HasError = true;
                response.Error = $"El nombre de usuario '{request.UserName}' ya existe";
                return response;
            }

            var UserSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (UserSameEmail != null)
            {
                response.HasError = true;
                response.Error = $"El email '{request.Email}' ya existe";
                return response;
            }

            var user = new ApplicationUser()
            {
                Email = request.Email,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                UserName = request.UserName,
                Cedula = request.Cedula,
                EmailConfirmed = request.Rol == Roles.Admin
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                if (request.Rol == Roles.Admin)
                {
                    await _userManager.AddToRoleAsync(user, Roles.Admin.ToString());
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, Roles.Cliente.ToString());

                    // Crear cuenta de ahorro si se especificó un monto
                    if (request.Monto.HasValue)
                    {
                        string numeroCuenta;
                        do
                        {
                            numeroCuenta = new Random().Next(100000000, 999999999).ToString();
                        } while (await _productoService.ExisteNumeroCuenta(numeroCuenta));

                        var cuentaViewModel = new SaveProductoViewModel
                        {
                            NumeroCuenta = numeroCuenta,
                            TipoCuenta = "Cuenta de ahorro",
                            EsPrincipal = true,
                            Monto = request.Monto.Value,
                            Limite = null,
                            Deuda = null,
                            ClienteId = user.Id
                        };

                        await _productoService.Add(cuentaViewModel);
                    }
                }
            }
            else
            {
                response.HasError = true;
                response.Error = $"An error has occurred in the register";
                return response;
            }

            return response;
        }

        public async Task<RegisterResponse> EditUserAsync(RegisterRequest request, string origin)
        {
            RegisterResponse response = new()
            {
                HasError = false
            };

            // Buscar el usuario existente por nombre de usuario
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            // Verificar si el nuevo email ya está registrado por otro usuario
            var userWithSameEmail = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != user.Id);
            if (userWithSameEmail != null)
            {
                response.HasError = true;
                response.Error = $"The email '{request.Email}' is already registered.";
                return response;
            }

            // Actualizar los datos del usuario
            user.Email = request.Email;
            user.Nombre = request.Nombre;
            user.Apellido = request.Apellido;
            user.Cedula = request.Cedula;

            // Actualizar la contraseña si se proporcionó una nueva
            if (!string.IsNullOrEmpty(request.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResetResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                if (!passwordResetResult.Succeeded)
                {
                    response.HasError = true;
                    response.Error = "An error occurred while updating the password.";
                    return response;
                }
            }

            // Guardar los cambios en el usuario
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                response.HasError = true;
                response.Error = "An error occurred while updating the user.";
                return response;
            }

            // Actualizar la cuenta de ahorro si se especificó un monto adicional
            if (request.MontoAdicional.HasValue)
            {
                var productos = await _productoService.GetAllByClienteId(user.Id);
                var productoPrincipal = productos.FirstOrDefault(p => p.EsPrincipal);

                if (productoPrincipal != null)
                {
                    productoPrincipal.Monto += request.MontoAdicional.Value;
                    var saveProductoViewModel = _mapper.Map<SaveProductoViewModel>(productoPrincipal);
                    await _productoService.Update(saveProductoViewModel, saveProductoViewModel.Id);
                }
            }

            return response;
        }


        public async Task<int> CountUsuariosByEstado(bool estado)
        {
            var users = _userManager.Users;

            int count = await users.CountAsync(u => u.EmailConfirmed == estado);

            return count;
        }

        public async Task<List<UserDto>> GetAllUsuarios()
        {
            var users = await _userManager.Users
                .Select(user => new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    Cedula = user.Cedula,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed
                })
                .ToListAsync();

            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.Id);
                var roles = await _userManager.GetRolesAsync(appUser);

                user.Tipo = roles.Contains("Admin") ? "Admin" : "Cliente";
            }

            return users;
        }


        public async Task<UserDto> GetByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Cedula = user.Cedula,
                EmailConfirmed = user.EmailConfirmed,
                Tipo = await GetUserTypeAsync(user.Id)
                // No mapear roles u otros detalles que no sean necesarios en UserDto
            };

            return userDto;
        }
        public async Task<string> GetUserTypeAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            if (await _userManager.IsInRoleAsync(user, Roles.Admin.ToString()))
            {
                return "Admin";
            }
            else if (await _userManager.IsInRoleAsync(user, Roles.Cliente.ToString()))
            {
                return "Cliente";
            }
            else
            {
                // Definir un comportamiento por defecto o manejar otros roles según sea necesario
                return "Otro";
            }
        }

        public async Task<ConfirmEmailResponse> ConfirmUserEmailAsync(EditUsuarioViewModel vm)
        {
            ConfirmEmailResponse response = new ConfirmEmailResponse
            {
                HasError = false
            };

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            user.EmailConfirmed = !user.EmailConfirmed;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = "Error confirming email.";
                return response;
            }

            return response;
        }


        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
