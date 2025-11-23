using MiniProyectoBanking.Core.Application.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniProyectoBanking.Core.Application.Dtos.Account
{
    public class RegisterRequest
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Cedula { get; set; }
        public Roles Rol { get; set; }
        public decimal? Monto { get; set; }

        public decimal? MontoAdicional { get; set; }

        public static implicit operator RegisterRequest(RegisterRequest v)
        {
            throw new NotImplementedException();
        }
    }
}
