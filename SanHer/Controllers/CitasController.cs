using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
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


        [HttpGet("MisCitas")]
        public async Task<IActionResult> GetCitasCliente(int id)
        {
            var citas = await (from cita in _context.Citas
                               where cita.IdUsuario == id
                               join horario in _context.Horarios
                               on cita.Horario equals horario.Id
                               join usuario in _context.Usuarios
                               on cita.IdUsuario equals usuario.Id
                               select new
                               {
                                   cita.Id,
                                   usuario.Nombre,
                                   usuario.Apellido1,
                                   usuario.Apellido2,
                                   cita.Fecha,
                                   horario.HoraInicio,
                                   horario.HoraFin,
                                   cita.Asunto,
                                   cita.Telefono,
                                   cita.Estatus
                               }).ToListAsync();

            var citasFormateadas = citas.Select(c => new CitaDetalleAux
            {
                Id = c.Id,
                Nombre = $"{c.Nombre} {c.Apellido1} {c.Apellido2}",
                Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                Hora = $"{c.HoraInicio} - {c.HoraFin}",
                Asunto = c.Asunto,
                Telefono = c.Telefono,
                Estatus = c.Estatus
            })
            .OrderBy(c => c.Hora)
            .ToList();

            return Ok(citasFormateadas);
        }



        [HttpGet("miagenda")]
        public async Task<IActionResult> GetAgenda(int idContador, string fecha)
        {
            try
            {
                // Convertir el string de fecha a DateOnly
                var fechaCita = DateOnly.Parse(fecha);

                // Filtrar las citas por idContador y fecha
                var citas = await _context.Citas
                    .Where(c => c.IdContadorAsignado == idContador && c.Fecha == fechaCita)
                    .ToListAsync();

                // Realizar la unión con las tablas Usuarios y Horarios
                var resultado = citas
                    .Join(_context.Usuarios,
                        cita => cita.IdUsuario,
                        usuario => usuario.Id,
                        (cita, usuario) => new { cita, usuario })
                    .Join(_context.Horarios,
                        cu => cu.cita.Horario,
                        horario => horario.Id,
                        (cu, horario) => new CitaDetalleAux
                        {
                            Id = cu.cita.Id,
                            Nombre = $"{cu.usuario.Nombre} {cu.usuario.Apellido1} {cu.usuario.Apellido2}",
                            Fecha = cu.cita.Fecha.ToString("yyyy-MM-dd"),
                            Hora = $"{horario.HoraInicio} - {horario.HoraFin}",
                            Asunto = cu.cita.Asunto,
                            Telefono = cu.cita.Telefono,
                            Estatus = cu.cita.Estatus // Asegúrate de incluir el estatus si lo necesitas
                        })
                    .OrderBy(c => c.Hora) // Ordenar por hora
                    .ToList();

                return Ok(resultado);
            }
            catch (FormatException)
            {
                return BadRequest("Formato de fecha no válido. Use el formato yyyy-MM-dd.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error interno del servidor: " + ex.Message);
            }
        }

        [HttpGet("recientes")]
        public async Task<IActionResult> GetCitasRecientes()
        {
            var citas = await _context.Citas
            .Where(c => c.Fecha == DateOnly.FromDateTime(DateTime.Today))
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();


            return Ok(citas);
        }

        [HttpGet("miscitashoy")]
        public async Task<IActionResult> GetMisCitasHoy(int idContador)
        {
            var citas = await _context.Citas
                .Where(c => c.Fecha == DateOnly.FromDateTime(DateTime.Today))
                .Where(c => c.IdContadorAsignado == idContador)
                .ToListAsync(); // 🟢 Traemos los datos a memoria

            var resultado = citas
                .Join(_context.Usuarios,
                      cita => cita.IdUsuario,
                      usuario => usuario.Id,
                      (cita, usuario) => new { cita, usuario })
                .Join(_context.Horarios,
                      cu => cu.cita.Horario,
                      horario => horario.Id,
                      (cu, horario) => new CitaDetalleAux
                      {
                          Nombre = $"{cu.usuario.Nombre} {cu.usuario.Apellido1} {cu.usuario.Apellido2}",
                          Fecha = cu.cita.Fecha.ToString("yyyy-MM-dd"),
                          Hora = $"{horario.HoraInicio} - {horario.HoraFin}",
                          Asunto = cu.cita.Asunto,
                          Telefono = cu.cita.Telefono
                      })
                .OrderByDescending(c => c.Fecha)
                .ToList();

            return Ok(resultado);
        }


        public class CitaDetalleAux
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Fecha { get; set; }
            public string Hora { get; set; }
            public string Asunto { get; set; }
            public string Telefono { get; set; }
            public int Estatus { get; set; }
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

            // Filtrar los contadores disponibles
            var contadoresDisponibles = new List<(int IdHorario, int IdContador)>();
            foreach (var horario in horariosDisponibles)
            {
                bool contadorOcupado = await _context.Citas
                    .AnyAsync(c => c.Fecha == citaAuxiliar.Fecha && c.Horario == horario.Id && c.IdContadorAsignado == horario.IdContador);

                if (!contadorOcupado)
                {
                    contadoresDisponibles.Add((horario.Id, horario.IdContador));
                }
            }

            if (!contadoresDisponibles.Any())
            {
                return BadRequest("No hay contadores disponibles para este horario.");
            }

            // Seleccionar un contador disponible de manera aleatoria
            var random = new Random();
            var contadorAsignado = contadoresDisponibles[random.Next(contadoresDisponibles.Count)];

            // Crear la cita con los datos completos
            var cita = new Cita
            {
                IdUsuario = citaAuxiliar.IdUsuario,
                Fecha = citaAuxiliar.Fecha,
                Horario = contadorAsignado.IdHorario,
                Telefono = citaAuxiliar.Telefono,
                Estatus = citaAuxiliar.Estatus,
                IdContadorAsignado = contadorAsignado.IdContador,
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





        [HttpGet("contador/{idContador}")]
        public async Task<ActionResult<IEnumerable<CitasDiaSemana>>> GetReporteContador(int idContador)
        {
            // Ejecutar la consulta SQL y mapear los resultados al modelo auxiliar
            var reporte = await _context.Database
                .SqlQueryRaw<CitasDiaSemana>(@"
                    SELECT 
                        H.DiaSemana AS Name,
                        COALESCE(SUM(CASE WHEN C.Estatus = 2 THEN 1 ELSE 0 END), 0) AS Atendidas,
                        COALESCE(SUM(CASE WHEN C.Estatus = 3 THEN 1 ELSE 0 END), 0) AS Canceladas
                    FROM ( 
                        -- Lista fija de días de la semana
                        SELECT 'Lunes' AS DiaSemana UNION ALL
                        SELECT 'Martes' UNION ALL
                        SELECT 'Miercoles' UNION ALL
                        SELECT 'Jueves' UNION ALL
                        SELECT 'Viernes'
                    ) AS H
                    LEFT JOIN Horario AS Ho ON H.DiaSemana = Ho.DiaSemana
                    LEFT JOIN Cita AS C ON Ho.Id = C.Horario AND C.IdContadorAsignado = {0}
                    GROUP BY H.DiaSemana
                    ORDER BY 
                        CASE 
                            WHEN H.DiaSemana = 'Lunes' THEN 1
                            WHEN H.DiaSemana = 'Martes' THEN 2
                            WHEN H.DiaSemana = 'Miercoles' THEN 3
                            WHEN H.DiaSemana = 'Jueves' THEN 4
                            WHEN H.DiaSemana = 'Viernes' THEN 5
                        END;
                ", idContador)
                .ToListAsync();

            return Ok(reporte);
        }

        public class CitasDiaSemana
        {
            public string Name { get; set; }
            public int Atendidas { get; set; }
            public int Canceladas { get; set; }
        }

        [HttpPut("{id}/estatus")]
        public async Task<IActionResult> CambiarEstatusCita(int id, [FromBody] int estatus)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null)
            {
                return NotFound();
            }

            cita.Estatus = estatus;
            _context.Entry(cita).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> CancelarCita(int id)
        {
            var cita = await _context.Citas.FindAsync(id);
            if (cita == null)
            {
                return NotFound("Cita no encontrada.");
            }

            // Cambiar el estatus a 3 (Cancelada)
            cita.Estatus = 3;
            _context.Entry(cita).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Citas.Any(e => e.Id == id))
                {
                    return NotFound("Cita no encontrada.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }



        [HttpGet("por-contador/{contadorId}")]
        public async Task<IActionResult> GetCitasPorContador(int contadorId, [FromQuery] string startDate = null, [FromQuery] string endDate = null)
        {
            var query = from cita in _context.Citas
                        join usuario in _context.Usuarios on cita.IdUsuario equals usuario.Id
                        join horario in _context.Horarios on cita.Horario equals horario.Id
                        where cita.IdContadorAsignado == contadorId
                        select new
                        {
                            cita.Id,
                            cita.Fecha,
                            HoraInicio = horario.HoraInicio,
                            HoraFin = horario.HoraFin,
                            cita.Asunto,
                            cita.Estatus,
                            cita.Telefono,
                            NombreCliente = usuario.Nombre + " " + usuario.Apellido1,
                            DiaSemana = horario.DiaSemana,
                            HoraInicioOrden = horario.HoraInicio // Agregado para ordenamiento
                        };

            // Filtrar por fechas si se especifica
            if (!string.IsNullOrEmpty(startDate))
            {
                var fechaInicio = DateOnly.Parse(startDate);
                query = query.Where(c => c.Fecha >= fechaInicio);
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                var fechaFin = DateOnly.Parse(endDate);
                query = query.Where(c => c.Fecha <= fechaFin);
            }

            var citas = await query
                .OrderByDescending(c => c.Fecha)
                .ThenBy(c => c.HoraInicioOrden) // Usamos el campo agregado
                .ToListAsync();

            // Mapear estatus numérico a texto y formatear datos
            var resultado = citas.Select(c => new
            {
                c.Id,
                Fecha = c.Fecha.ToString("dd/MM/yyyy"),
                c.HoraInicio,
                c.HoraFin,
                c.Asunto,
                Estatus = c.Estatus switch
                {
                    1 => "Pendiente",
                    2 => "Atendida",
                    3 => "Cancelada",
                    _ => "Desconocido"
                },
                c.NombreCliente,
                c.Telefono,
                c.DiaSemana,
                HoraCompleta = $"{c.HoraInicio} - {c.HoraFin}"
            });

            return Ok(resultado);
        }


        [HttpGet("reporte-contador")]
        public async Task<IActionResult> GenerarReporteContador(
    [FromQuery] string startDate,
    [FromQuery] string endDate,
    [FromQuery] int contadorId)
        {
            try
            {
                var fechaInicio = DateOnly.Parse(startDate);
                var fechaFin = DateOnly.Parse(endDate);

                var reporte = await (
                    from cita in _context.Citas
                    join usuario in _context.Usuarios on cita.IdUsuario equals usuario.Id
                    join horario in _context.Horarios on cita.Horario equals horario.Id
                    where cita.IdContadorAsignado == contadorId &&
                          cita.Fecha >= fechaInicio &&
                          cita.Fecha <= fechaFin
                    select new
                    {
                        cita.Id,
                        Cliente = $"{usuario.Nombre} {usuario.Apellido1} {usuario.Apellido2}",
                        cita.Asunto,
                        Fecha = cita.Fecha.ToString("dd/MM/yyyy"),
                        Hora = horario.HoraInicio,
                        cita.Estatus
                    }).ToListAsync();

                var atendidas = reporte.Count(c => c.Estatus == 2);
                var canceladas = reporte.Count(c => c.Estatus == 3);

                var resultado = new
                {
                    FechaInicio = fechaInicio.ToString("dd/MM/yyyy"),
                    FechaFin = fechaFin.ToString("dd/MM/yyyy"),
                    TotalAtendidas = atendidas,
                    TotalCanceladas = canceladas,
                    Citas = reporte.Select(c => new
                    {
                        c.Cliente,
                        c.Asunto,
                        c.Fecha,
                        c.Hora,
                        Estatus = c.Estatus // Mantenemos el número para el frontend
                    }).ToList()
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al generar el reporte: {ex.Message}");
            }
        }

    }
}