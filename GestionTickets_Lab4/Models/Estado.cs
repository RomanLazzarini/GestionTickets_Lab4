using System.ComponentModel.DataAnnotations;

namespace GestionTickets_Lab4.Models
{
    public class Estado
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(50)]
        public string Descripcion { get; set; } // Ej: Abierto, En Proceso, Cerrado
    }
}