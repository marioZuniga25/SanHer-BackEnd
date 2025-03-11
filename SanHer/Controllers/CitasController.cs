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
        public async Task<IActionResult> AgendarCita([FromBody] CitaAuxiliar citaAuxiliar)
        {
            if (EsDiaNoLaborable(citaAuxiliar.Fecha))
            {
                return BadRequest("No se pueden agendar citas en días no laborables.");
            }

            // Obtener todos los horarios que coinciden con el rango de horario seleccionado
            var horariosDisponibles = await _context.Horarios
                .Where(h => h.HoraInicio == citaAuxiliar.HoraInicio && h.HoraFin == citaAuxiliar.HoraFin)
                .ToListAsync();

            if (horariosDisponibles == null || !horariosDisponibles.Any())
            {
                return BadRequest("Horario no válido.");
            }

            // Buscar un contador disponible para el horario seleccionado
            int? contadorAsignado = null;
            int? horarioAsignado = null;
            foreach (var horario in horariosDisponibles)
            {
                bool contadorOcupado = await _context.Citas
                    .AnyAsync(c => c.Fecha == citaAuxiliar.Fecha && c.Horario == horario.Id && c.IdContadorAsignado == horario.IdContador);

                if (!contadorOcupado)
                {
                    contadorAsignado = horario.IdContador;
                    horarioAsignado = horario.Id; // Asignar el ID del horario seleccionado
                    break; // Asignar el primer contador disponible
                }
            }

            if (contadorAsignado == null || horarioAsignado == null)
            {
                return BadRequest("No hay contadores disponibles para este horario.");
            }

            // Crear la cita con los datos completos
            var cita = new Cita
            {
                IdUsuario = citaAuxiliar.IdUsuario,
                Fecha = citaAuxiliar.Fecha,
                Horario = horarioAsignado.Value,
                Telefono = citaAuxiliar.Telefono,
                Estatus = citaAuxiliar.Estatus,
                IdContadorAsignado = contadorAsignado.Value,
                Asunto = citaAuxiliar.Asunto
            };

            // Guardar la cita en la base de datos
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
    
        public class CitaAuxiliar
        {
            public int IdUsuario { get; set; }
            public DateOnly Fecha { get; set; }
            public string HoraInicio { get; set; }
            public string HoraFin { get; set; }
            public string Telefono { get; set; }
            public int Estatus { get; set; } = 1;
            public string Asunto { get; set; }
        }
    
}