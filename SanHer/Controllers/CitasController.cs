using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanHer.Models;

namespace SanHer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitasController : ControllerBase
    {
        private readonly SanHerContext _context;

        public CitasController(SanHerContext context)
        {
            _context = context;
        }

        // Obtener todas las citas
        [HttpGet]
        public async Task<IActionResult> GetCitas()
        {
            var citas = await _context.Citas.ToListAsync();
            return Ok(citas);
        }

        // Agendar una cita
        [HttpPost]
        public async Task<IActionResult> AgendarCita([FromBody] Cita cita)
        {
            if (EsDiaNoLaborable(cita.Fecha))
            {
                return BadRequest("No se pueden agendar citas en días no laborables.");
            }

            var horario = await _context.Horarios
                .FirstOrDefaultAsync(h => h.Id == cita.Horario);

            if (horario == null)
            {
                return BadRequest("Horario no válido.");
            }

            cita.Estatus = 1; // Establecer el estatus de la cita, por ejemplo, "agendada"
            _context.Citas.Add(cita);
            await _context.SaveChangesAsync();
            return Ok(cita);
        }

        // Obtener los días no laborables
        [HttpGet("diasnolaborables")]
        public IActionResult GetDiasNoLaborables()
        {
            var diasNoLaborables = _context.DiasNoLaborables.ToList();
            return Ok(diasNoLaborables);
        }

        // Agregar un día no laborable
        [HttpPost("diasnolaborables")]
        public async Task<IActionResult> AgregarDiaNoLaborable([FromBody] DiaNoLaborable dia)
        {
            if (!await _context.DiasNoLaborables.AnyAsync(d => d.Fecha == dia.Fecha))
            {
                _context.DiasNoLaborables.Add(dia);
                await _context.SaveChangesAsync();
            }
            return Ok(dia);
        }

        // Verificar si un día es no laborable
        private bool EsDiaNoLaborable(DateOnly fecha)
        {
            var dia = new DateTime(fecha.Year, fecha.Month, fecha.Day);
            return _context.DiasNoLaborables.Any(d => d.Fecha == dia) || dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}
