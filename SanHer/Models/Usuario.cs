namespace SanHer.Models
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido1 { get; set; }
        public string Apellido2 { get; set; }
        public string Correo { get; set; }
        public string Contrasenia { get; set; }
        public string Rol { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime UltimaConexion { get; set; }
    }
}
