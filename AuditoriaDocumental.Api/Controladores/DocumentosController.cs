namespace AuditoriaDocumental.Api.Controladores;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditoriaDocumental.Api.Datos;
using AuditoriaDocumental.Api.Modelos;
using AuditoriaDocumental.Api.Servicios; // añadido para poder usar nuestro servicio de ia
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class DocumentosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;
    private readonly string _containerName = "documentos-auditados";
    private readonly ServicioExtraccionIA _servicioIA; // variable para guardar el cerebro de la ia

    // inyectamos la base de datos, la configuracion y nuestro nuevo servicio de ia
    public DocumentosController(AppDbContext context, IConfiguration configuration, ServicioExtraccionIA servicioIA)
    {
        _context = context;
        _servicioIA = servicioIA; // lo guardamos para usarlo luego

        // maxima seguridad: leemos claves de variables de entorno para no filtrarlas a github
        _connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                            ?? configuration.GetConnectionString("AzureStorage")
                            ?? throw new InvalidOperationException("no se encuentra la cadena de conexion de azure");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Documento>>> GetDocumentos()
    {
        var documentos = await _context.Documentos
            .Include(d => d.Extraccion)
            .ToListAsync();

        return Ok(documentos);
    }

    // nuevo metodo seguro para el dashboard del frontend
    [HttpGet("historial")]
    public async Task<IActionResult> GetHistorial()
    {
        // limitamos a 10 resultados para no saturar la base de datos ni consumir ancho de banda
        var historial = await _context.Extracciones
                                      .OrderByDescending(e => e.Id)
                                      .Take(10)
                                      .ToListAsync();
        return Ok(historial);
    }

    [HttpPost("upload")]
    [EnableRateLimiting("limiteSubida")] // escudo activo: evita que saturen la api y te cobren en openai/azure
    [RequestSizeLimit(2097152)] // limite estricto 2mb por archivo
    public async Task<IActionResult> UploadDocumento(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest("no has subido ningún archivo.");

        if (archivo.ContentType != "application/pdf" && !archivo.FileName.EndsWith(".pdf"))
            return BadRequest("solo aceptamos archivos pdf.");

        try
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var nombreArchivo = Guid.NewGuid().ToString() + ".pdf";
            var blobClient = containerClient.GetBlobClient(nombreArchivo);

            // abrimos el archivo una sola vez
            using (var stream = archivo.OpenReadStream())
            {
                // 1. lo subimos al blob storage de azure
                await blobClient.UploadAsync(stream, true);

                // 2. se lo pasamos a la ia para que extraiga el json, el importe, etc.
                var datosExtraidos = await _servicioIA.AnalizarFacturaAsync(stream);

                var rutaAzure = blobClient.Uri.AbsoluteUri;

                // 3. creamos el documento y le enganchamos lo que ha extraido la ia
                var nuevoDocumento = new Documento
                {
                    NombreArchivo = archivo.FileName,
                    RutaBlobAzure = rutaAzure,
                    Estado = "Pendiente",
                    FechaSubida = DateTime.UtcNow,
                    Extraccion = datosExtraidos // aqui hacemos la magia de unir las dos tablas
                };

                // guardamos todo del tiron en sql server
                _context.Documentos.Add(nuevoDocumento);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "pdf subido y analizado por ia con éxito",
                    documentoId = nuevoDocumento.Id,
                    url = rutaAzure,
                    datosIA = datosExtraidos // lo devolvemos para mostrarlo en angular
                });
            }
        }
        catch (Exception ex)
        {
            // no devolvemos el ex.message en produccion para no dar pistas a atacantes, pero para desarrollo sirve
            return StatusCode(500, $"error interno al subir a azure o analizar con ia: {ex.Message}");
        }
    }
    // metodo para limpiar la base de datos y los archivos fisicos de azure
    [HttpDelete("limpiar")]
    public async Task<IActionResult> LimpiarBaseDeDatos()
    {
        try
        {
            // 1. nos conectamos a azure blob storage de forma segura
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            
            // si el contenedor existe, recorremos y borramos todos los pdfs fisicos
            if (await containerClient.ExistsAsync())
            {
                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    await containerClient.DeleteBlobIfExistsAsync(blob.Name);
                }
            }

            // borramos los registros de las tablas en sql server
            var documentos = await _context.Documentos.ToListAsync();
            _context.Documentos.RemoveRange(documentos);

            var extracciones = await _context.Extracciones.ToListAsync();
            _context.Extracciones.RemoveRange(extracciones);

            // guardamos los cambios en la base de datos
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "pdfs de azure y datos de sql limpiados correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"error al limpiar el entorno: {ex.Message}");
        }
    }
}