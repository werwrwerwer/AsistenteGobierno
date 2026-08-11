using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AsistenteGobierno
{
    public class TramitesChat
    {
        private readonly ILogger _logger;
        // HttpClient se recicla por rendimiento en Azure Functions
        private static readonly HttpClient _httpClient = new HttpClient();

        public TramitesChat(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TramitesChat>();
        }

        [Function("TramitesChat")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Procesando pregunta para Llama 3 en la nube...");

            // 1. Leer la pregunta del usuario desde el HTML
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var datos = JsonSerializer.Deserialize<JsonElement>(requestBody);
            string pregunta = datos.GetProperty("pregunta").GetString();

            // 2. DETECCIÓN DE ENTORNO Y CONFIGURACIÓN DINÁMICA
            string ambiente = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
            bool esLocal = ambiente == "Development";

            // Variables por defecto (Producción en Azure usando Groq)
            string apiUrl = "https://api.groq.com/openai/v1/chat/completions";
            string apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            string nombreModelo = "llama-3.1-8b-instant";

            if (esLocal)
            {
                // Entorno de pruebas (Local usando Ollama y Llama 3)
                _logger.LogInformation("Ejecutando en entorno local: Conectando al agente Llama 3 por Ollama...");
                apiUrl = "http://localhost:11434/v1/chat/completions";
                apiKey = "ollama"; // Ollama no requiere llave real
                nombreModelo = "llama3";
            }
            else
            {
                _logger.LogInformation("Ejecutando en Producción (Azure): Conectando a Groq...");
            }
           
            // 3. Empaquetar las instrucciones para el modelo dinámico
            var requestBodyJson = new
            {
                model = nombreModelo,
                messages = new[]
                {
                    new {
                           role = "system",
                           content = @"Eres el 'Asistente Tramix', un asesor virtual oficial del gobierno de México. 
    Tu objetivo es guiar a los ciudadanos de forma amable, clara y concisa (máximo 3 párrafos cortos por respuesta).
    
    ESTÁS AUTORIZADO para brindar información y requisitos sobre:
    - SAT (Régimen fiscal, e.firma, declaraciones).
    - IMSS (Alta patronal, vigencia de derechos, semanas cotizadas).
    - Secretaría de Relaciones Exteriores (Trámite de Pasaportes).
    - INE (Trámite de credencial para votar y reposiciones).
    - Registro Civil (Actas de nacimiento, matrimonio y defunción).
    - Trámites vehiculares (Licencias de conducir, tarjetas de circulación, placas).
    - Cetesdirecto y bonos gubernamentales.

    REGLA CRÍTICA DE NAVEGACIÓN:
    Tú te encuentras operando dentro del portal web oficial de 'Tramix'. En la parte superior de la pantalla del usuario existe un menú con tres pestañas: '📝 Trámite en Ventanilla', '💬 Asesoría con IA' y '🗂️ Panel Admin'.
    
    SI un usuario te pregunta CÓMO agendar, sacar o registrar una cita para cualquier trámite, ADEMÁS de darle los requisitos, DEBES decirle explícitamente: 'Para agendar tu cita en este momento, por favor haz clic en la pestaña 📝 Trámite en Ventanilla ubicada en la parte superior de tu pantalla, llena el formulario con tu CURP/RFC y selecciona la fecha deseada.'
    
    Si el usuario te pregunta por temas NO gubernamentales, discúlpate amablemente y explica tu función."
                          },
                    new {
                        role = "user",
                        content = pregunta
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBodyJson), Encoding.UTF8, "application/json");

            // ---------------------------------------------------------
            // 4. Hacer la llamada real a la nube con manejo de excepciones
            // ---------------------------------------------------------
            try
            {
                // ¡Aquí está la magia! Ya no está pegada la URL de Groq, ahora usa apiUrl
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
                requestMessage.Content = content;

                var aiResponse = await _httpClient.SendAsync(requestMessage);
                string aiResponseString = await aiResponse.Content.ReadAsStringAsync();

                // --- ESCUDO DE SEGURIDAD (Rechazos de Groq) ---
                if (!aiResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Groq rechazó la petición: {aiResponseString}");
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    errorResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                    await errorResponse.WriteStringAsync("El asistente está fuera de servicio temporalmente por mantenimiento.");
                    return errorResponse;
                }

                // 5. Desempaquetar la respuesta
                var aiData = JsonSerializer.Deserialize<JsonElement>(aiResponseString);
                string respuestaFinal = aiData.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                // 6. Enviar de regreso a la página web
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await response.WriteStringAsync(respuestaFinal);

                return response;
            }
            catch (HttpRequestException netEx)
            {
                // Atrapa caídas de red o si el servidor de Groq no responde
                _logger.LogError($"Error de conexión con IA: {netEx.Message}");
                var fallbackResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                fallbackResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await fallbackResponse.WriteStringAsync("Los servidores gubernamentales están experimentando intermitencias. Por favor, intenta de nuevo en un minuto.");
                return fallbackResponse;
            }
            catch (Exception ex)
            {
                // Atrapa cualquier otro error inesperado (fallos de JSON, etc.)
                _logger.LogError($"Error crítico en el backend: {ex.Message}");
                var fallbackResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                fallbackResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await fallbackResponse.WriteStringAsync("Ocurrió un error interno en el sistema. Estamos trabajando para solucionarlo.");
                return fallbackResponse;
            }
        } // Cierra el método Run
    } // Cierra la clase TramitesChat
} // Cierra el namespace AsistenteGobierno