using System.ComponentModel.DataAnnotations;

namespace GestionTickets_Lab4.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Display(Name = "Fecha de Solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Observación")]
        public string Observacion { get; set; }

        // Clave Foránea a Afiliado
        [Display(Name = "Afiliado")]
        public int AfiliadoId { get; set; }
        public Afiliado? Afiliado { get; set; }

        // Relación: Un ticket tiene un historial de detalles
        public ICollection<TicketDetalle>? Detalles { get; set; }
    }
}