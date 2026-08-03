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
    public async Task<IActionResult> UploadDocumento(IFormFile archivo)
    {
        // comprobamos que nos han mandado algo y que es un pdf
        if (archivo == null || archivo.Length == 0)
            return BadRequest("No has subido ningún archivo.");

        if (archivo.ContentType != "application/pdf" && !archivo.FileName.EndsWith(".pdf"))
            return BadRequest("Solo aceptamos archivos pdf.");

        // creamos una carpeta temporal en el proyecto para ir guardando los pdfs
        var rutaCarpeta = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(rutaCarpeta))
            Directory.CreateDirectory(rutaCarpeta);

        // generamos un nombre único para que no se pisen si suben dos con el mismo nombre
        var nombreArchivo = Guid.NewGuid().ToString() + ".pdf";
        var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        // guardamos el archivo físico en la carpeta
        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        // creamos el registro para guardarlo en la base de datos sql
        var nuevoDocumento = new Documento
        {
            NombreArchivo = archivo.FileName, // el nombre original que tenía el usuario
            RutaBlobAzure = rutaCompleta, // de momento guardamos la ruta local, luego será la de azure
            Estado = "Pendiente",
            FechaSubida = DateTime.UtcNow
        };

        _context.Documentos.Add(nuevoDocumento);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "PDF subido con éxito", documentoId = nuevoDocumento.Id });
    }
}