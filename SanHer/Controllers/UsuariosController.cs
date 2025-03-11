using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SanHer;
using SanHer.Models;

namespace SanHer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly SanHerContext _context;
        private readonly IConfiguration _config;

        public UsuariosController(SanHerContext context, IConfiguration config)
        {
            _context = context;
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }


        public class LoginRequest //modelo auxiliar para el endpoint de Login
        {
            public string Correo { get; set; }
            public string Contrasenia { get; set; }
        }

        [HttpPost("login")]
        public async Task<ActionResult> LoginUsuario([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Contrasenia, usuario.Contrasenia))
            {
                return NotFound("Usuario o contraseña incorrectos.");
            }

            // Generar el token JWT
            var token = GenerarToken(usuario);

            // Devolver el token y los datos del usuario
            return Ok(new
            {
                token,
                nombre = usuario.Nombre,
                correo = usuario.Correo,
                apellido1 = usuario.Apellido1,
                apellido2 = usuario.Apellido2,
                idUsuario = usuario.Id
            });
        }

        private string GenerarToken(Usuario usuario)
        {
            var secretKey = _config["JwtSettings:Secret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, usuario.Correo),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("rol", usuario.Rol),
        new Claim("id", usuario.Id.ToString())
    };

            var token = new JwtSecurityToken(
                _config["JwtSettings:Issuer"],
                _config["JwtSettings:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(60), // 1 hora de expiración
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest();
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }



        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        public class UsuarioRegisterRequest
        {
            public string Nombre { get; set; }
            public string Apellido1 { get; set; }
            public string Apellido2 { get; set; }
            public string Correo { get; set; }
            public string Contrasenia { get; set; }
            public string ConfirmarContrasenia { get; set; }
            public string Rol { get; set; }  // Valor por defecto
        }
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(UsuarioRegisterRequest request)
        {
            var usuarioExists = await _context.Usuarios
            .AnyAsync(u => u.Correo == request.Correo);

            if (usuarioExists)
            {
                return Conflict(new { message = "Este correo ya está asignado a otra cuenta." });
            }
                var usuario = new Usuario
                {
                    Nombre = request.Nombre,
                    Apellido1 = request.Apellido1,
                    Apellido2 = request.Apellido2,
                    Correo = request.Correo,
                    Rol = request.Rol,
                    Contrasenia = BCrypt.Net.BCrypt.HashPassword(request.Contrasenia),
                    FechaRegistro = DateTime.Now,  // Establecer la fecha y hora actual
                    UltimaConexion = DateTime.Now  // Establecer la fecha y hora actual
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
                return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);

            

        }




        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }
    }
}
