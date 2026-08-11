using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AsistenteGobierno
{
    public class AgendarCita
    {
        private readonly ILogger _logger;

        public AgendarCita(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<AgendarCita>();
        }

        [Function("AgendarCita")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Procesando nueva solicitud de cita en ventanilla...");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var datosCita = JsonSerializer.Deserialize<DatosCita>(requestBody);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            if (datosCita == null || string.IsNullOrEmpty(datosCita.identificador))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("{\"error\": \"Faltan datos requeridos para el trámite.\"}");
                return response;
            }

            // Validación de longitud de CURP/RFC
            string identificadorLimpio = datosCita.identificador.Trim();
            if (identificadorLimpio.Length < 12 || identificadorLimpio.Length > 18)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                var errorRespuesta = new
                {
                    estatus = "error",
                    mensaje = "El identificador es inválido. Un RFC debe tener 12 o 13 caracteres y una CURP 18."
                };
                await response.WriteStringAsync(JsonSerializer.Serialize(errorRespuesta));
                return response;
            }

            datosCita.identificador = identificadorLimpio;

            System.Random rnd = new System.Random();
            string folioOficial = $"TRAMITE-{rnd.Next(10000, 99999)}";

            // Guardado en Azure Table Storage
            var nuevaCita = new CitaEntity
            {
                PartitionKey = "Citas",
                RowKey = folioOficial,
                Nombre = datosCita.nombre ?? "Sin Nombre",
                Identificador = datosCita.identificador,
                Tramite = datosCita.tramite ?? "No especificado",
                Fecha = datosCita.fecha ?? "Sin fecha"
            };

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableServiceClient serviceClient = new TableServiceClient(connectionString);
            TableClient tableClient = serviceClient.GetTableClient("CitasTramix");

            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(nuevaCita);

            _logger.LogInformation($"¡Base de datos en la nube actualizada! Folio guardado: {folioOficial}");

            var resultado = new
            {
                estatus = "exito",
                mensaje = "Cita agendada y guardada en Azure",
                folio = folioOficial,
                nombreConfirmado = datosCita.nombre,
                fechaConfirmada = datosCita.fecha
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(resultado));
            return response;
        }
    }
}