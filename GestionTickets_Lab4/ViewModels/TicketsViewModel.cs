using GestionTickets_Lab4.Models;

namespace GestionTickets_Lab4.ViewModels
{
    public class TicketsViewModel
    {
        public IEnumerable<Ticket>? Tickets { get; set; }
        public string? BuscarAfiliado { get; set; }
        public string? FiltroEstado { get; set; }
        public Paginador? Paginador { get; set; }
    }
}