using System.ComponentModel.DataAnnotations;

namespace GestionTickets_Lab4.Models
{
    public class TicketDetalle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe especificar la descripción del pedido")]
        [Display(Name = "Descripción del Pedido")]
        public string DescripcionPedido { get; set; }

        [Display(Name = "Fecha del Estado")]
        public DateTime FechaEstado { get; set; } = DateTime.Now;

        // Relación con Ticket (Muchos detalles pertenecen a un Ticket)
        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        // Relación con Estado (Cada detalle tiene un estado asignado)
        [Display(Name = "Estado")]
        public int EstadoId { get; set; }
        public Estado? Estado { get; set; }
    }
}