using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GenAIWorker 
{ 
    class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("GenAI Worker starting...");

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "rabbitmq",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__Username") ?? "user",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "password"
            };

            var queueName = Environment.GetEnvironmentVariable("RabbitMQ__Queue") ?? "genai_queue";
            var apiKey = Environment.GetEnvironmentVariable("GenAI__ApiKey");
            var restUrl = Environment.GetEnvironmentVariable("Rest__BaseUrl") ?? "http://rest:8081";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("ERROR: No GenAI API key set!");
                return;
            }

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            Console.WriteLine($"Listening on {queueName}...");
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var msg = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var payload = JsonSerializer.Deserialize<GenAIRequest>(msg);
                    if (payload == null)
                    {
                        Console.WriteLine("Received invalid GenAIRequest message.");
                        return;
                    }

                    Console.WriteLine($"Processing summary request for: {payload.DocumentId}");

                    // 1. Call Google Gemini
                    var summary = await GenerateSummary(apiKey, payload.OcrText);

                    // 2. Send summary back to REST API
                    await PostSummaryToRest(restUrl, payload.DocumentId, summary);

                    Console.WriteLine($"Summary stored for document {payload.DocumentId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GenAI Worker error: " + ex.Message);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: true, consumer);
            await Task.Delay(Timeout.Infinite);
        }

        record GenAIRequest(Guid DocumentId, string OcrText);

        static async Task<string> GenerateSummary(string apiKey, string text)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] {
                            new { text = $"Summarize the following document concisely:\n{text}" }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
               "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API call failed: {err}");
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            // Safe JSON parsing
            try
            {
                return doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";
            }
            catch
            {
                throw new Exception("Gemini response format was unexpected.");
            }
        }

        static async Task PostSummaryToRest(string restUrl, Guid documentId, string summary)
        {
            using var client = new HttpClient();

            var body = new { Summary = summary };
            var json = JsonSerializer.Serialize(body);

            var response = await client.PostAsync(
                $"{restUrl}/document/{documentId}/summary",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new Exception($"REST rejected summary: {text}");
            }
        }
    }
}

