using Ticket.Models;
using Ticket.Modules.LogKafka;

namespace Ticket.Helpers;

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;

public class KafkaHelper : IKafkaHelper
{
    private readonly IProducer<Null, string> _producer;
    private readonly IServiceProvider  _serviceProvider;
    public KafkaHelper(ProducerConfig producerConfig, IServiceProvider  serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        var serializedMessage = JsonSerializer.Serialize(message);

        try
        {
            var deliveryResult = await _producer.ProduceAsync(topic, new Message<Null, string> { Value = serializedMessage });
            Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
        }
        catch (ProduceException<Null, string> ex)
        {
            Console.WriteLine($"Produce error: {ex.Error.Reason} (Error code: {ex.Error.Code})");

            if (ex.Error.IsFatal)
            {
                Console.WriteLine("Fatal error encountered. Stopping producer...");
                throw;
            }
            else
            {
                Console.WriteLine("Transient error. Retrying...");
            }
        }
    }
    public async Task ConsumeAsync(string topic,string groupId, Func<string, Task> handleMessage, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result != null)
                    {
                        await handleMessage(result.Message.Value);
                        
                        consumer.Commit(result);
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Consume error: {ex.Error.Reason}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Consumer operation canceled.");
                    break;
                }
            }
        }
        finally
        {
            consumer.Close(); 
        }
    }

    public async Task ConsumeWithRetryAsync(string topic, string groupId, Func<string, Task> messageHandler, CancellationToken stoppingToken,
        int maxRetryAttempts = 3, int retryDelayMs = 1000)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };
       using var consumer= new ConsumerBuilder<string,string>(config).Build();
       consumer.Subscribe(topic);
       try
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               try
               {
                   var result = consumer.Consume(stoppingToken);
                   if(result!=null)
                   {
                       await ExecuteWithRetryAsync(async () => await messageHandler(result.Message.Value), maxRetryAttempts, retryDelayMs,
                           topic, result.Message.Value);
                       consumer.Commit(result);
                   }
               }
               catch(ConsumeException  ex)
               {
                   Console.WriteLine($"Kafka consuming error: {ex.Message}"); 
               }
           }
       }
       finally
       {
           consumer.Close();
       }
    }
    
    private async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetryAttempts, int retryDelayMs,
        string topic, string message)
    {
        var retryCount = 0;
        while (retryCount < maxRetryAttempts)
        {
            try
            {
                await action();
                return; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
                retryCount++;
                if (retryCount >= maxRetryAttempts)
                {
                    var logModel=new KafkaErrorLogModel
                    {
                        Message = message,
                        Topic = topic,
                        Error = ex.Message
                    };
                    using var scope = _serviceProvider.CreateScope();
                    var logService = scope.ServiceProvider.GetRequiredService<ILogKafkaService>();
                    await logService.LogErrorAsync(logModel);
                    
                    Console.WriteLine("Max retry attempts reached. Logging error...");
                    break;
                }

                await Task.Delay(retryDelayMs);
            }
        }
    }
}