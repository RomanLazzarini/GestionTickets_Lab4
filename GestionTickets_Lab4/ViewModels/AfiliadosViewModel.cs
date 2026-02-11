using GestionTickets_Lab4.Models;

namespace GestionTickets_Lab4.ViewModels
{
    public class AfiliadosViewModel
    {
        public List<Afiliado> Afiliados { get; set; } = new List<Afiliado>();
        public Paginador Paginador { get; set; } = new Paginador();

        // Filtros de búsqueda
        public string BuscarNombre { get; set; }
        public string BuscarApellido { get; set; }
        public string BuscarDNI { get; set; } 
    }
}