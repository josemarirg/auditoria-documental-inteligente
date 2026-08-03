namespace AuditoriaDocumental.Api.Controladores;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using AuditoriaDocumental.Api.Modelos;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly AppDbContext _context;

    // inyectamos la base de datos para poder consultar las tablas
    public DocumentosController(AppDbContext context)
    {
        _context = context;
    }

    // get: api/documentos
    // este endpoint nos servirá para llenar la tabla de angular
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos()
    {
        // buscamos los documentos e incluimos los datos de la ia si los tiene
        var documentos = await _context.Documentos
            .Include(d => d.Extraccion)
            .ToListAsync();

        return Ok(documentos);
    }

    // post: api/documentos/upload
    // aquí es donde mandaremos el pdf desde el frontend
    [HttpPost("upload")]
    public IActionResult UploadDocumento()
    {
        // TODO: aquí irá el código para subir a azure y llamar a la ia
        return Ok(new { mensaje = "endpoint listo para recibir archivos en el futuro" });
    }
}