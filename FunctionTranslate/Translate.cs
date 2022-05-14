using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Collections.Generic;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using System.IO;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace FunctionTranslate
{
    public class Prueba
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("Input")]
        public string Input { get; set; }

        [JsonProperty("Output")]
        public string Output { get; set; }
    }

    public class Response<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; } //Lista return

    }

    public static class Translate
    {
        //Llaves de acceso al recurso speech en azure
        static readonly string speechSubscriptionKey = "c32eb7ad10524e13a841465f9924dbf7";
        static readonly string speechServiceRegion = "eastus";

        [FunctionName("Translate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Translation/")] HttpRequest req,
            [CosmosDB(
                databaseName: "db-CognitiveServices",
                collectionName: "SpeechTranslation",
                ConnectionStringSetting = "strCosmos")]
                IAsyncCollector<Prueba> items,
            ILogger log)
        {

            bool existeArchivo = false;

            string responseMessage = "";
            string responseMessageJSON = "";

            AudioConfig audioInput = null;

            //Configuraciones del TranslationConfig
            var region = speechServiceRegion;
            var endpointString = $"wss://{region}.stt.speech.microsoft.com/speech/universal/v2";
            var endpointUrl = new Uri(endpointString);
            var translationConfig = SpeechTranslationConfig
                .FromEndpoint(endpointUrl, speechSubscriptionKey);

            //Configuraciones del lenguaje
            //Lenguaje de entrada (Sólo al inicializar)
            string fromLanguage = "en-US";
            translationConfig.SpeechRecognitionLanguage = fromLanguage;

            //Lenguaje(s) de salida
            var toLanguages = new List<string> { "es" };
            //toLanguages = new List<string> { "es", "en", "fr", "de", "pt", "it" }; //Aumenta los lenguajes de salida
            toLanguages.ForEach(translationConfig.AddTargetLanguage);

            //Lenguajes de entrada admitidos
            translationConfig.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Latency");
            var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "es-MX", "en-US", "fr-FR", "de-DE", "pt-BR", "it-IT" });

            try
            {
                var audioReq = req.Form.Files[0].OpenReadStream();
                existeArchivo = true;
            }
            catch (Exception ex)
            {}

            if (existeArchivo)
            {
                Console.WriteLine("Si hay archivo");

                var audioReq = req.Form.Files[0].OpenReadStream();

                using var audioInputStream = AudioInputStream.CreatePushStream();
                using var audioFile = AudioConfig.FromStreamInput(audioInputStream);

                var reader = new BinaryReader(audioReq);
                byte[] readBytes;
                do
                {
                    readBytes = reader.ReadBytes(1024);
                    audioInputStream.Write(readBytes, readBytes.Length);
                }
                while (readBytes.Length > 0);

                audioInput = audioFile;
            }
            else 
            {
                Console.WriteLine("No hay archivo, detectando audio del micrófono...");

                var audioMic = AudioConfig.FromDefaultMicrophoneInput();

                audioInput = audioMic;
            }

            var stopTranslation = new TaskCompletionSource<int>();

            using (var recognizer = new TranslationRecognizer(translationConfig, autoDetectSourceLanguageConfig, audioInput))
            {

                //Reconocimiento finalizado
                recognizer.Recognized += (s, e) => {
                    if (e.Result.Reason == ResultReason.TranslatedSpeech)
                    {
                        var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                        Console.WriteLine($"Recognized in '{lidResult}': Text={e.Result.Text}");

                        responseMessage += $"Recognized in '{lidResult}': Text={e.Result.Text}\n";
                        responseMessageJSON += "{\"Input\":\"" + $"Recognized in '{lidResult}': Text={e.Result.Text}" + "\",\"Output\":\"";

                        foreach (var element in e.Result.Translations)
                        {
                            Console.WriteLine($"Translated into '{element.Key}': {element.Value}");

                            responseMessage += $"Translated into '{element.Key}': {element.Value}\n";
                            responseMessageJSON += $"Translated into '{element.Key}': {element.Value}";

                        }

                        Console.WriteLine("");

                        responseMessageJSON += "\"}";

                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                //Reconocimiento cancelado
                recognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");
                    responseMessage += $"CANCELED: Reason={e.Reason}";

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");

                        responseMessage += $"CANCELED: ErrorCode={e.ErrorCode}";
                        responseMessage += $"CANCELED: ErrorDetails={e.ErrorDetails}";
                        responseMessage += $"CANCELED: Did you set the speech resource key and region values?";
                    }

                    stopTranslation.TrySetResult(0);
                };

                //Configuraciones de inicio y fin de detección
                recognizer.SpeechStartDetected += (s, e) => {
                    Console.WriteLine("Speech start detected event.\n");
                };
                recognizer.SpeechEndDetected += (s, e) => {
                    Console.WriteLine("Speech end detected event.");
                };

                //Configuraciones de inicio y fin de sesión
                recognizer.SessionStarted += (s, e) => {
                    Console.WriteLine("Session started event.");
                };
                recognizer.SessionStopped += (s, e) => {
                    Console.WriteLine("Session stopped event.");
                    Console.WriteLine($"Stop translation.");
                    stopTranslation.TrySetResult(0);
                };


                // Start continuous recognition. Use StopContinuousRecognitionAsync() to stop recognition.
                Console.WriteLine("Start translation...");
                //await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                await recognizer.RecognizeOnceAsync();

                Task.WaitAny(new[] { stopTranslation.Task });
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }

            var responseMessagedb = JsonConvert.DeserializeObject<Prueba>(responseMessageJSON);

            await items.AddAsync(responseMessagedb);

            return new OkObjectResult(responseMessage);
        }


        [FunctionName("GetTranslations")]
        public static IActionResult RunGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Translation/")] HttpRequest req,
            [CosmosDB(
                databaseName: "db-CognitiveServices",
                collectionName: "SpeechTranslation",
                ConnectionStringSetting = "strCosmos")]
                IEnumerable<Prueba> items,
            ILogger log)
        {
            var Response = new Response<IEnumerable<Prueba>>
            {Data = items,};

            return new OkObjectResult(Response);
        }

        [FunctionName("DeleteTranslation")]
        public static async Task<IActionResult> RunDelete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Translation/{id}")] HttpRequest req,
            string id,
            [CosmosDB(
                databaseName: "db-CognitiveServices",
                collectionName: "SpeechTranslation",
                ConnectionStringSetting = "strCosmos")]
                DocumentClient client,
                ILogger log)
        {
            await client.DeleteDocumentAsync(
                UriFactory.CreateDocumentUri("db-CognitiveServices", "SpeechTranslation", id),
                new RequestOptions() { PartitionKey = new PartitionKey(id) });

            return new OkObjectResult("Se eliminó la traducción id: " + id);
        }


    }


}