using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanHer;
using SanHer.Models;
using static SanHer.Controllers.CitasController;

namespace SanHer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HorariosController : ControllerBase
    {
        private readonly SanHerContext _context;

        public HorariosController(SanHerContext context)
        {
            _context = context;
        }

        // GET: api/Horarios/disponibles?dia=1&fecha=2023-10-02
        [HttpGet("disponibles")]
        public async Task<ActionResult<IEnumerable<object>>> GetHorariosDisponibles([FromQuery] int dia, [FromQuery] string fecha)
        {
            // Convertir el número del día al nombre del día en español
            string nombreDia = ObtenerNombreDia(dia);

            // Convertir la fecha a DateOnly
            var fechaCita = DateOnly.Parse(fecha);

            // Obtener todos los horarios para el día seleccionado
            var horarios = await _context.Horarios
                .Where(h => h.DiaSemana == nombreDia)
                .ToListAsync();

            // Obtener las citas agendadas para la fecha seleccionada
            var citasAgendadas = await _context.Citas
                .Where(c => c.Fecha == fechaCita)
                .ToListAsync();

            // Filtrar horarios disponibles
            var horariosDisponibles = horarios
                .GroupBy(h => new { h.HoraInicio, h.HoraFin })
                .Select(g => new
                {
                    HoraInicio = g.Key.HoraInicio,
                    HoraFin = g.Key.HoraFin,
                    ContadoresDisponibles = g.Where(h => !citasAgendadas.Any(c => c.Horario == h.Id && c.IdContadorAsignado == h.IdContador))
                })
                .Where(g => g.ContadoresDisponibles.Any()) // Solo horarios con contadores disponibles
                .Select(g => new
                {
                    HoraInicio = g.HoraInicio,
                    HoraFin = g.HoraFin
                })
                .ToList();

            return Ok(horariosDisponibles);
        }

        // Método para convertir el número del día al nombre del día en español
        private string ObtenerNombreDia(int dia)
        {
            string[] dias = { "Domingo", "Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado" };
            return dias[dia];
        }


        [HttpGet("mishorarios")]
        public async Task<ActionResult<IEnumerable<object>>> GetHorariosContador([FromQuery] int idContador, [FromQuery] string dia)
        {
            var horarios = await _context.Horarios
                .Where(h => h.DiaSemana == dia).Where(h => h.IdContador == idContador)
                .ToListAsync();

            return Ok(horarios);
        }

        [HttpPost]
        public async Task<IActionResult> AgendarCita([FromBody] Horario horario)
        {
            _context.Horarios.Add(horario);
            await _context.SaveChangesAsync();

            return Ok(horario);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHorario(int id)
        {
            var horario= await _context.Horarios.FindAsync(id);
            if (horario == null)
            {
                return NotFound();
            }

            _context.Horarios.Remove(horario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}