namespace SanHer.Models
{
    public class Cita
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public DateOnly Fecha { get; set; }
        public int Horario { get; set; }
        public string telefono { get; set; }
        public int Estatus { get; set; }
        public int IdContadorAsignado { get; set; }
    }
}
