namespace SanHer.Models
{
    public class Horario
    {
        public int Id { get; set; }
        public int IdContador { get; set; }
        public string DiaSemana { get; set; }
        public string HoraInicio { get; set; }
        public string HoraFin {  get; set; }
        public string Disponibilidad { get; set; }
    }
}
