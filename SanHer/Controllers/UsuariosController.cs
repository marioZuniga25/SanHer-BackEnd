using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.AspNetCore.Authorization;
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


        [HttpGet("getContadores")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetContadores()
        {
            var contadores = await _context.Usuarios.Where(u => u.Rol == "CO" || u.Rol == "A").ToListAsync();


            return Ok(contadores);
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
                idUsuario = usuario.Id,
                rol = usuario.Rol
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
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPut("update-last-connection/{id}")]
        public async Task<IActionResult> UpdateLastConnection(int id)
        {
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // Actualizar la última conexión
            usuarioExistente.UltimaConexion = DateTime.Now;

            _context.Entry(usuarioExistente).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
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

            // Obtener el usuario existente de la base de datos
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // Encriptar la contraseña solo si fue modificada
            if (!string.IsNullOrEmpty(usuario.Contrasenia) && usuario.Contrasenia != usuarioExistente.Contrasenia)
            {
                usuario.Contrasenia = BCrypt.Net.BCrypt.HashPassword(usuario.Contrasenia);
            }

            // Mantener otros campos sin modificar si no se enviaron nuevos valores
            _context.Entry(usuarioExistente).CurrentValues.SetValues(usuario);
            _context.Entry(usuarioExistente).State = EntityState.Modified;

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

            
            usuario.Estatus = 0; 
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/reactivar")]  // Usamos PATCH para actualización parcial
        public async Task<IActionResult> ReactivarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Cambiar el estatus a "Activo" (1)
            usuario.Estatus = 1; // O usar EstatusUsuario.Activo si tienes un enum

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Usuario reactivado exitosamente",
                Usuario = usuario
            });
        }


        [HttpPatch("{id}/rol")]
        public async Task<IActionResult> UpdateRol(int id, [FromBody] UpdateRolDto updateDto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound("Usuario no encontrado");
            }

            // Solo actualiza el rol sin validaciones adicionales
            usuario.Rol = updateDto.NuevoRol;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class UpdateRolDto
        {
            [Required]
            [RegularExpression("^(cliente|CO|A)$", ErrorMessage = "Rol no válido")]
            public string NuevoRol { get; set; }
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id == id);
        }


        [HttpPut("update-profile/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
        {
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // Verificar la contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, usuarioExistente.Contrasenia))
            {
                return BadRequest("La contraseña actual es incorrecta.");
            }

            // Actualizar los datos del perfil
            usuarioExistente.Nombre = request.Nombre;
            usuarioExistente.Apellido1 = request.Apellido1;
            usuarioExistente.Apellido2 = request.Apellido2;
            usuarioExistente.Correo = request.Correo;

            _context.Entry(usuarioExistente).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            var usuarioExistente = await _context.Usuarios.FindAsync(id);
            if (usuarioExistente == null)
            {
                return NotFound();
            }

            // Verificar la contraseña actual
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, usuarioExistente.Contrasenia))
            {
                return BadRequest("La contraseña actual es incorrecta.");
            }

            // Cambiar la contraseña
            usuarioExistente.Contrasenia = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _context.Entry(usuarioExistente).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class UpdateProfileRequest
        {
            public string Nombre { get; set; }
            public string Apellido1 { get; set; }
            public string Apellido2 { get; set; }
            public string Correo { get; set; }
            public string CurrentPassword { get; set; }
        }

        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
            public string ConfirmNewPassword { get; set; }
        }

    }
}
