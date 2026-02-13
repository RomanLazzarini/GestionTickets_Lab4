using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionTickets_Lab4.Data;
using GestionTickets_Lab4.Models;

namespace GestionTickets_Lab4.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Tickets
                .Include(t => t.Afiliado)       // Traemos datos del Afiliado
                .Include(t => t.Detalles)       // Traemos el historial de detalles
                .ThenInclude(d => d.Estado)     // Traemos la descripción del estado (Pendiente, etc)
                .OrderByDescending(t => t.FechaSolicitud); // Ordenamos: los más nuevos primero

            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Afiliado)       // Datos del afiliado
                .Include(t => t.Detalles)       // Historial
                .ThenInclude(d => d.Estado)     // Descripciones de estados
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            // --- NUEVO: Cargar la lista de estados para el formulario de evolución ---
            ViewData["ListaEstados"] = new SelectList(_context.Estados, "Id", "Descripcion");

            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido");
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Observacion,AfiliadoId")] Ticket ticket)
        {
            // Forzamos la fecha al momento actual (para que no venga null o vacía del formulario)
            ticket.FechaSolicitud = DateTime.Now;

            // Quitamos la validación de 'Afiliado' y 'Detalles' para que el ModelState no de error 
            // (ya que son propiedades de navegación que no vienen del formulario)
            ModelState.Remove("Afiliado");
            ModelState.Remove("Detalles");

            if (ModelState.IsValid)
            {
                // -----------------------------------------------------------
                // PASO 1: Guardar la Cabecera (El Ticket en sí)
                // -----------------------------------------------------------
                _context.Add(ticket);
                await _context.SaveChangesAsync(); // Aquí SQL Server genera el ID del Ticket

                // -----------------------------------------------------------
                // PASO 2: Generar el Detalle Automático (Estado "Pendiente")
                // -----------------------------------------------------------

                // Buscamos el ID del estado "Pendiente" en la base de datos
                var estadoPendiente = await _context.Estados
                                            .FirstOrDefaultAsync(e => e.Descripcion == "Pendiente");

                // Validación de seguridad: Si no existe el estado, no podemos crear el detalle
                if (estadoPendiente != null)
                {
                    var detalleInicial = new TicketDetalle
                    {
                        TicketId = ticket.Id, // Usamos el ID recién creado
                        EstadoId = estadoPendiente.Id,
                        FechaEstado = DateTime.Now,
                        DescripcionPedido = "Inicio del reclamo: " + ticket.Observacion
                    };

                    _context.Add(detalleInicial);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // Si algo falló, recargamos el combo de Afiliados
            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido", ticket.AfiliadoId);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido", ticket.AfiliadoId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FechaSolicitud,Observacion,AfiliadoId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido", ticket.AfiliadoId);
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Afiliado)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }

        // POST: Tickets/AgregarDetalle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarDetalle(int TicketId, int EstadoId, string Observacion)
        {
            // Validamos que venga algo de texto
            if (string.IsNullOrEmpty(Observacion))
            {
                // Si está vacío, recargamos la página sin hacer nada
                return RedirectToAction("Details", new { id = TicketId });
            }

            // Creamos el nuevo movimiento en el historial
            var nuevoDetalle = new TicketDetalle
            {
                TicketId = TicketId,
                EstadoId = EstadoId,
                FechaEstado = DateTime.Now,
                DescripcionPedido = Observacion
            };

            _context.Add(nuevoDetalle);
            await _context.SaveChangesAsync();

            // Redirigimos a la misma página de detalles para ver el nuevo comentario agregado
            return RedirectToAction("Details", new { id = TicketId });
        }
    }
}
