using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend_Cooganicas.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {   
        // Chamamos nosso contexto da base de dados
       CoorganicasContext _contexto = new CoorganicasContext();
        public static int UsuarioLogado { get; private set;}

        // Definimos uma variavel para percorrer nossos métodos com as configurações obtidas no appsetting.json
        private IConfiguration _config;

        // Definimos um metodo construtor para poder acessar estas configs ^
        
        public LoginController(IConfiguration config){
            _config = config;
        }   


        // Chamamos nosso método para validar o usuário na aplicação, verificando se ele existe em nosso banco de dados
        private Usuario ValidaUsuario(Usuario login) {
            var usuario = _contexto.Usuario.Include(x => x.TipoUsuario).FirstOrDefault(
                u => u.Email == login.Email && u.Senha == login.Senha
            );

            UsuarioLogado = usuario.UsuarioId;

            
            // if(usuario == null) {
            //     return null;
            //  }

            return usuario;
        }

        // Geramos o Token
        private string GerarToken(Usuario userInfo) {
            
            // Definimos a criptografia do nosso Token
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Definimos nossas Claims (dados da sessão)
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.NameId, userInfo.Nome),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role,userInfo.TipoUsuario.Tipo),                            

            };

            // Configuramos nosso Token e seu tempo de vida
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires : DateTime.Now.AddMinutes(120),
                signingCredentials : credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Usa essa anotação para ignorar a autenticação nesse método
        [HttpPost]
        [AllowAnonymous]
        public IActionResult Login([FromBody]Usuario login) {
            
            IActionResult response = Unauthorized();

            var user = ValidaUsuario(login);

            if(user != null) {
                var tokenString = GerarToken(user);
                response = Ok(new {token = tokenString});
            }

            return response;
        }

    }

}