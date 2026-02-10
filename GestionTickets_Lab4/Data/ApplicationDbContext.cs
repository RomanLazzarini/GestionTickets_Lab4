using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GestionTickets_Lab4.Models; // <--- Importante este using

namespace GestionTickets_Lab4.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // TABLAS DEL SISTEMA
        public DbSet<Afiliado> Afiliados { get; set; }
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketDetalle> TicketDetalles { get; set; }
    }
}