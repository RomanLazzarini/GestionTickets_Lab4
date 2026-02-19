using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace GestionTickets_Lab4.Models
{
    public class Afiliado
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombres { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "El DNI no puede superar los 20 caracteres")]
        public string DNI { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime FechaNacimiento { get; set; }

        [Display(Name = "Foto de Perfil")]
        public string? Foto { get; set; } // Guardar la ruta (path) del archivo

        // Relación: Un afiliado puede tener muchos tickets
        public ICollection<Ticket>? Tickets { get; set; }
    }
}