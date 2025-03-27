namespace SanHer.Models
{
    public class Auditoria
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public string Accion {  get; set; }
        public DateOnly Fecha { get; set; }
        public string TablaAfectada { get; set; }
        public string Descripcion { get; set; }
    }
}
