namespace AuditoriaDocumental.Api.Servicios;

using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Configuration;
using AuditoriaDocumental.Api.Modelos;

public class ServicioExtraccionIA
{
    private readonly string _endpoint;
    private readonly string _key;

    // inyectamos la configuracion para leer los user secrets de forma segura
    public ServicioExtraccionIA(IConfiguration configuracion)
    {
        _endpoint = configuracion["IA:Endpoint"] ?? throw new InvalidOperationException("falta el endpoint de la ia chaval");
        _key = configuracion["IA:Key"] ?? throw new InvalidOperationException("falta la key de la ia");
    }

    public async Task<Extraccion> AnalizarFacturaAsync(Stream archivoStream)
    {
        // creamos el cliente para conectarnos al cerebro de azure
        var credencial = new AzureKeyCredential(_key);
        var cliente = new DocumentAnalysisClient(new Uri(_endpoint), credencial);

        // nos aseguramos de que leemos el pdf desde el principio
        archivoStream.Position = 0;

        // le mandamos el archivo al modelo pre-entrenado de facturas
        var operacion = await cliente.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-invoice", archivoStream);
        var resultado = operacion.Value;

        // creamos el objeto vacio que luego guardaremos en sql
        var extraccion = new Extraccion();
        
        // si la ia ha encontrado algun documento en el pdf, sacamos los datos
        if (resultado.Documents.Count > 0)
        {
            var documentoInfo = resultado.Documents[0];

            // buscamos el nombre del proveedor
            if (documentoInfo.Fields.TryGetValue("VendorName", out DocumentField? campoProveedor) && campoProveedor.FieldType == DocumentFieldType.String)
            {
                extraccion.Proveedor = campoProveedor.Value.AsString();
            }

            // solucion al bug del importe: aceptamos tipo currency y tipo double
            if (documentoInfo.Fields.TryGetValue("InvoiceTotal", out DocumentField? campoTotal))
            {
                if (campoTotal.FieldType == DocumentFieldType.Currency)
                {
                    extraccion.ImporteTotal = (decimal)campoTotal.Value.AsCurrency().Amount;
                }
                else if (campoTotal.FieldType == DocumentFieldType.Double)
                {
                    extraccion.ImporteTotal = (decimal)campoTotal.Value.AsDouble();
                }
            }

            // buscamos la fecha de la factura
            if (documentoInfo.Fields.TryGetValue("InvoiceDate", out DocumentField? campoFecha) && campoFecha.FieldType == DocumentFieldType.Date)
            {
                extraccion.FechaEmision = campoFecha.Value.AsDate().UtcDateTime;
            }
            
            // le ponemos un texto fijo de momento para que entity framework no se queje al guardar en sql
            extraccion.DatosRawJSON = "json desactivado temporalmente";
        }

        return extraccion;
    }
}