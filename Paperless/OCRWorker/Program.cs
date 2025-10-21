using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OcrWorker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("OCR Worker starting...");

            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMQ__Host") ?? "rabbitmq",
                UserName = Environment.GetEnvironmentVariable("RabbitMQ__Username") ?? "user",
                Password = Environment.GetEnvironmentVariable("RabbitMQ__Password") ?? "password"
            };

            var queueName = Environment.GetEnvironmentVariable("RabbitMQ__Queue") ?? "ocr_queue";

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            Console.WriteLine($"Listening for messages on queue: {queueName}");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received message: {message}");
                await Task.Delay(500); // simulate OCR processing
                Console.WriteLine("OCR processing complete (simulated).");
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            // keep process alive indefinitely
            await Task.Delay(Timeout.Infinite);
        }
    }
}