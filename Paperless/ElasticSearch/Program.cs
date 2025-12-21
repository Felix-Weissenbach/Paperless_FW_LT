using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Elastic.Clients.Elasticsearch;
using System;
using System.Threading.Tasks;
using System.Threading;
using Paperless.ElasticSearch;
using System.Net.Http;

record IndexMessage(Guid DocumentId, string FileName, string OcrText);

namespace Paperless.ElasticSearch
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Indexing Worker started");

            var client = ElasticClientFactory.Create();

            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "user",
                Password = "password"
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "index_queue",
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var msg = JsonSerializer.Deserialize<IndexMessage>(json);

                if (msg == null)
                {
                    Console.WriteLine("Received invalid IndexMessage.");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }
                Console.WriteLine($"Indexing document {msg.DocumentId}");

                await client.IndexAsync(new
                {
                    documentId = msg.DocumentId,
                    fileName = msg.FileName,
                    content = msg.OcrText,
                    createdAt = DateTime.UtcNow
                }, i => i.Index("documents"));

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await channel.BasicConsumeAsync("index_queue", false, consumer);

            await Task.Delay(Timeout.Infinite);
        }
    }

}

