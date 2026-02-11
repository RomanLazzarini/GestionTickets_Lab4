namespace GestionTickets_Lab4.ViewModels
{
    public class Paginador
    {
        public int PaginaActual { get; set; } //Número de página actual
        public int RegistrosPorPagina { get; set; } // Cantidad de registros a mostrar por página
        public int TotalRegistros { get; set; } // Total de registros disponibles
        public int TotalPaginas => (int)Math.Ceiling((decimal)TotalRegistros / RegistrosPorPagina); // Cálculo del total de páginas
        public Dictionary<string, string> ValoresQueryString { get; set; } = new Dictionary<string, string>();
        // Este diccionario se usará para mantener otros parámetros de búsqueda o filtrado en la URL, además del número de página.
    }
}

