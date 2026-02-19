using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // 👈 NUEVO: Para la seguridad
using GestionTickets_Lab4.Data;
using GestionTickets_Lab4.Models;
using GestionTickets_Lab4.ViewModels; // 👈 NUEVO: Para el paginador y filtros

namespace GestionTickets_Lab4.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Tickets (PÚBLICO: Cualquier usuario puede ver el listado)
        public async Task<IActionResult> Index(string buscarAfiliado, string filtroEstado, int pagina = 1)
        {
            // 1. COMPORTAMIENTO POR DEFECTO: Si no hay filtro, mostrar Pendientes y En Proceso
            if (string.IsNullOrEmpty(filtroEstado))
            {
                filtroEstado = "Activos";
            }

            var consulta = _context.Tickets
                .Include(t => t.Afiliado)
                .Include(t => t.Detalles!)
                .ThenInclude(d => d.Estado)
                .AsQueryable();

            // 2. FILTRO POR TEXTO (Afiliado)
            if (!string.IsNullOrEmpty(buscarAfiliado))
            {
                consulta = consulta.Where(t =>
                    t.Afiliado!.Apellido.Contains(buscarAfiliado) ||
                    t.Afiliado!.Nombres.Contains(buscarAfiliado));
            }

            // 3. FILTRO POR ESTADO (Verificando el último detalle registrado)
            if (filtroEstado == "Activos")
            {
                // Muestra solo si el último estado es Pendiente o En Proceso
                consulta = consulta.Where(t =>
                    t.Detalles!.OrderByDescending(d => d.FechaEstado).FirstOrDefault()!.Estado!.Descripcion == "Pendiente" ||
                    t.Detalles!.OrderByDescending(d => d.FechaEstado).FirstOrDefault()!.Estado!.Descripcion == "En Proceso");
            }
            else if (filtroEstado == "Resueltos")
            {
                // Muestra solo si el último estado es Resuelto
                consulta = consulta.Where(t =>
                    t.Detalles!.OrderByDescending(d => d.FechaEstado).FirstOrDefault()!.Estado!.Descripcion == "Resuelto");
            }
            // Si el filtro es "Todos", no aplicamos la restricción Where.

            // Ordenamos: los más nuevos primero
            consulta = consulta.OrderByDescending(t => t.FechaSolicitud);

            // 4. PAGINACIÓN
            int registrosPorPagina = 5;
            int totalRegistros = await consulta.CountAsync();

            var registros = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // 5. ARMAR EL VIEWMODEL
            var modelo = new TicketsViewModel()
            {
                Tickets = registros,
                BuscarAfiliado = buscarAfiliado,
                FiltroEstado = filtroEstado,
                Paginador = new Paginador()
                {
                    PaginaActual = pagina,
                    RegistrosPorPagina = registrosPorPagina,
                    TotalRegistros = totalRegistros
                }
            };

            // Mantener los filtros al cambiar de página
            if (!string.IsNullOrEmpty(buscarAfiliado)) modelo.Paginador.ValoresQueryString.Add("buscarAfiliado", buscarAfiliado);
            modelo.Paginador.ValoresQueryString.Add("filtroEstado", filtroEstado);

            // Opciones para el desplegable de filtros en la vista
            var opciones = new List<SelectListItem>
            {
                new SelectListItem { Value = "Activos", Text = "Pendientes y En Proceso" },
                new SelectListItem { Value = "Resueltos", Text = "Solo Resueltos" },
                new SelectListItem { Value = "Todos", Text = "Ver Todos" }
            };

            // Envolvemos la lista en un "SelectList" y le pasamos "filtroEstado" 
            // para que el sistema sepa cuál debe quedar seleccionado en pantalla.
            ViewBag.OpcionesEstado = new SelectList(opciones, "Value", "Text", filtroEstado);

            return View(modelo);
        }

        // GET: Tickets/Details/5 (PÚBLICO)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Afiliado)
                .Include(t => t.Detalles!)
                .ThenInclude(d => d.Estado)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            ViewData["ListaEstados"] = new SelectList(_context.Estados, "Id", "Descripcion");
            return View(ticket);
        }

        // 🔒 CREATE, EDIT Y DELETE AHORA REQUIEREN LOGIN
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var listaAfiliados = await _context.Afiliados
                .Select(a => new {
                    Id = a.Id,
                    NombreCompleto = a.Apellido + ", " + a.Nombres + " (DNI: " + a.DNI + ")"
                })
                .OrderBy(a => a.NombreCompleto)
                .ToListAsync();

            ViewData["AfiliadoId"] = new SelectList(listaAfiliados, "Id", "NombreCompleto");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // 🔒
        public async Task<IActionResult> Create([Bind("Id,Observacion,AfiliadoId")] Ticket ticket)
        {
            ticket.FechaSolicitud = DateTime.Now;
            ModelState.Remove("Afiliado");
            ModelState.Remove("Detalles");

            if (ModelState.IsValid)
            {
                _context.Add(ticket);
                await _context.SaveChangesAsync();

                var estadoPendiente = await _context.Estados.FirstOrDefaultAsync(e => e.Descripcion == "Pendiente");

                if (estadoPendiente != null)
                {
                    var detalleInicial = new TicketDetalle
                    {
                        TicketId = ticket.Id,
                        EstadoId = estadoPendiente.Id,
                        FechaEstado = DateTime.Now,
                        DescripcionPedido = "Inicio del reclamo: " + ticket.Observacion
                    };

                    _context.Add(detalleInicial);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            var listaAfiliados = await _context.Afiliados
                .Select(a => new {
                    Id = a.Id,
                    NombreCompleto = a.Apellido + ", " + a.Nombres + " (DNI: " + a.DNI + ")"
                })
                .OrderBy(a => a.NombreCompleto)
                .ToListAsync();

            ViewData["AfiliadoId"] = new SelectList(listaAfiliados, "Id", "NombreCompleto", ticket.AfiliadoId);
            return View(ticket);
        }

        [Authorize] // 🔒
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido", ticket.AfiliadoId);
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // 🔒
        public async Task<IActionResult> Edit(int id, [Bind("Id,FechaSolicitud,Observacion,AfiliadoId")] Ticket ticket)
        {
            if (id != ticket.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AfiliadoId"] = new SelectList(_context.Afiliados, "Id", "Apellido", ticket.AfiliadoId);
            return View(ticket);
        }

        [Authorize] // 🔒
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Afiliado)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize] // 🔒
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null) _context.Tickets.Remove(ticket);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
            return _context.Tickets.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // 🔒 Solo usuarios logueados pueden evolucionar un ticket
        public async Task<IActionResult> AgregarDetalle(int TicketId, int EstadoId, string Observacion)
        {
            if (string.IsNullOrEmpty(Observacion)) return RedirectToAction("Details", new { id = TicketId });

            var nuevoDetalle = new TicketDetalle
            {
                TicketId = TicketId,
                EstadoId = EstadoId,
                FechaEstado = DateTime.Now,
                DescripcionPedido = Observacion
            };

            _context.Add(nuevoDetalle);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = TicketId });
        }
    }
}