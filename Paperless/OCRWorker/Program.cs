using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;


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


var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    await Task.Delay(500); // simulate OCR processing
};

await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

// Keep process alive
await Task.Delay(Timeout.Infinite);