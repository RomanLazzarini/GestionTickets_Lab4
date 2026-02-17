using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionTickets_Lab4.Data;
using GestionTickets_Lab4.Models;
using GestionTickets_Lab4.ViewModels;
using Microsoft.AspNetCore.Authorization;
using SpreadsheetLight;

namespace GestionTickets_Lab4.Controllers
{
    [Authorize] // Bloquea todo el controlador a usuarios no logueados
    public class AfiliadosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AfiliadosController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Afiliados (Con Búsqueda y Paginación)
        public async Task<IActionResult> Index(string buscarNombre, string buscarApellido, string buscarDNI, int pagina = 1)
        {
            var consulta = _context.Afiliados.AsQueryable();

            // 1. Filtros
            if (!string.IsNullOrEmpty(buscarNombre))
            {
                consulta = consulta.Where(x => x.Nombres.Contains(buscarNombre));
            }

            if (!string.IsNullOrEmpty(buscarApellido))
            {
                consulta = consulta.Where(x => x.Apellido.Contains(buscarApellido));
            }

            // --- FILTRO DNI ---
            if (!string.IsNullOrEmpty(buscarDNI))
            {
                consulta = consulta.Where(x => x.DNI.Contains(buscarDNI));
            }
            // ------------------------

            // 2. Paginación
            int registrosPorPagina = 5;
            int totalRegistros = await consulta.CountAsync();

            var registros = await consulta
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .ToListAsync();

            // 3. Armar el ViewModel
            AfiliadosViewModel modelo = new AfiliadosViewModel()
            {
                Afiliados = registros,
                BuscarNombre = buscarNombre,
                BuscarApellido = buscarApellido,
                BuscarDNI = buscarDNI, // <--- Asignamos al modelo
                Paginador = new Paginador()
                {
                    PaginaActual = pagina,
                    RegistrosPorPagina = registrosPorPagina,
                    TotalRegistros = totalRegistros
                }
            };

            // Guardar filtros para los botones de siguiente/anterior
            if (!string.IsNullOrEmpty(buscarNombre)) modelo.Paginador.ValoresQueryString.Add("buscarNombre", buscarNombre);
            if (!string.IsNullOrEmpty(buscarApellido)) modelo.Paginador.ValoresQueryString.Add("buscarApellido", buscarApellido);
            if (!string.IsNullOrEmpty(buscarDNI)) modelo.Paginador.ValoresQueryString.Add("buscarDNI", buscarDNI); // <--- Guardamos en paginación

            return View(modelo);
        }

        // GET: Afiliados/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var afiliado = await _context.Afiliados.FirstOrDefaultAsync(m => m.Id == id);
            if (afiliado == null) return NotFound();

            return View(afiliado);
        }

        // GET: Afiliados/Create
        [Authorize(Roles = "Administrador")] // Solo admin crea
        public IActionResult Create()
        {
            return View();
        }

        // POST: Afiliados/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("Id,Nombres,Apellido,DNI,FechaNacimiento")] Afiliado afiliado)
        {
            if (ModelState.IsValid)
            {
                var archivos = HttpContext.Request.Form.Files;
                if (archivos != null && archivos.Count > 0)
                {
                    var archivoFoto = archivos[0];
                    if (archivoFoto.Length > 0)
                    {
                        // Ruta destino: wwwroot/imagenes
                        var pathDestino = Path.Combine(_env.WebRootPath, "imagenes");
                        if (!Directory.Exists(pathDestino)) Directory.CreateDirectory(pathDestino);

                        // Generar nombre único
                        var archivoDestino = Guid.NewGuid().ToString() + Path.GetExtension(archivoFoto.FileName);

                        using (var filestream = new FileStream(Path.Combine(pathDestino, archivoDestino), FileMode.Create))
                        {
                            await archivoFoto.CopyToAsync(filestream);
                            // Guardamos la ruta relativa para usarla en el <img src="...">
                            afiliado.Foto = "/imagenes/" + archivoDestino;
                        }
                    }
                }

                _context.Add(afiliado);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(afiliado);
        }

        // GET: Afiliados/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var afiliado = await _context.Afiliados.FindAsync(id);
            if (afiliado == null) return NotFound();
            return View(afiliado);
        }

        // POST: Afiliados/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        [RequestSizeLimit(104857600)] // Aumentamos el límite a 100 MB (100 * 1024 * 1024 bytes)
        // 1. Se agrega "IFormFile archivoFoto" para recibir la imagen limpiamente
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombres,Apellido,DNI,FechaNacimiento,Foto")] Afiliado afiliado, IFormFile? archivoFoto)
        {
            if (id != afiliado.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // 2. Verificamos si subieron una NUEVA foto
                if (archivoFoto != null && archivoFoto.Length > 0)
                {
                    // A. Definir rutas
                    string wwwRootPath = _env.WebRootPath;
                    string nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(archivoFoto.FileName);
                    string rutaFinal = Path.Combine(wwwRootPath, "imagenes", nombreArchivo);

                    // B. Guardar la nueva foto
                    using (var fileStream = new FileStream(rutaFinal, FileMode.Create))
                    {
                        await archivoFoto.CopyToAsync(fileStream);
                    }

                    // C. Borrar la foto VIEJA (Si existe y es diferente a la nueva)
                    // Usamos try-catch para que si falla el borrado, NO se detenga el programa
                    if (!string.IsNullOrEmpty(afiliado.Foto))
                    {
                        try
                        {
                            string fotoViejaNombre = afiliado.Foto.Replace("/imagenes/", "");
                            string rutaVieja = Path.Combine(wwwRootPath, "imagenes", fotoViejaNombre);

                            if (System.IO.File.Exists(rutaVieja))
                            {
                                System.IO.File.Delete(rutaVieja);
                            }
                        }
                        catch (Exception)
                        {
                            // Si no se puede borrar la vieja (por permisos o uso), 
                            // simplemente lo ignoramos y seguimos. Lo importante es guardar la nueva.
                        }
                    }

                    // D. Actualizamos el modelo con la ruta de la nueva foto
                    afiliado.Foto = "/imagenes/" + nombreArchivo;
                }

                try
                {
                    _context.Update(afiliado);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AfiliadoExists(afiliado.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(afiliado);
        }

        // GET: Afiliados/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var afiliado = await _context.Afiliados.FirstOrDefaultAsync(m => m.Id == id);
            if (afiliado == null) return NotFound();

            return View(afiliado);
        }

        // POST: Afiliados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var afiliado = await _context.Afiliados.FindAsync(id);
            if (afiliado != null)
            {
                // Opcional: Borrar foto física al borrar el registro
                if (!string.IsNullOrEmpty(afiliado.Foto))
                {
                    string fotoNombre = afiliado.Foto.Replace("/imagenes/", "");
                    string pathFoto = Path.Combine(_env.WebRootPath, "imagenes", fotoNombre);
                    if (System.IO.File.Exists(pathFoto)) System.IO.File.Delete(pathFoto);
                }

                _context.Afiliados.Remove(afiliado);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // IMPORTAR EXCEL (Usando SpreadsheetLight)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ImportarExcel(IFormFile archivoExcel)
        {
            if (archivoExcel != null && archivoExcel.Length > 0)
            {
                using (var stream = archivoExcel.OpenReadStream())
                {
                    SLDocument sl = new SLDocument(stream);
                    int fila = 2; // Asumiendo que la fila 1 son cabeceras

                    while (!string.IsNullOrEmpty(sl.GetCellValueAsString(fila, 1)))
                    {
                        var nuevoAfiliado = new Afiliado
                        {
                            Nombres = sl.GetCellValueAsString(fila, 1),
                            Apellido = sl.GetCellValueAsString(fila, 2),
                            DNI = sl.GetCellValueAsString(fila, 3),
                            FechaNacimiento = sl.GetCellValueAsDateTime(fila, 4)
                        };

                        _context.Add(nuevoAfiliado);
                        fila++;
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AfiliadoExists(int id)
        {
            return _context.Afiliados.Any(e => e.Id == id);
        }
    }
}