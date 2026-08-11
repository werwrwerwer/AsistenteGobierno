using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AsistenteGobierno
{
    public class ConsultarCitas
    {
        private readonly ILogger _logger;

        public ConsultarCitas(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsultarCitas>();
        }

        [Function("ConsultarCitas")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            _logger.LogInformation("Extrayendo la lista de citas desde Azure Table Storage...");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            TableServiceClient serviceClient = new TableServiceClient(connectionString);
            TableClient tableClient = serviceClient.GetTableClient("CitasTramix");

            await tableClient.CreateIfNotExistsAsync();

            var listaCitas = new List<object>();
            var query = tableClient.QueryAsync<CitaEntity>(filter: "PartitionKey eq 'Citas'");

            await foreach (var cita in query)
            {
                listaCitas.Add(new
                {
                    FolioOficial = cita.RowKey,
                    Nombre = cita.Nombre,
                    Identificador = cita.Identificador,
                    Tramite = cita.Tramite,
                    Fecha = cita.Fecha
                });
            }

            string jsonRespuesta = JsonSerializer.Serialize(listaCitas);
            await response.WriteStringAsync(jsonRespuesta);

            return response;
        }
    }
}