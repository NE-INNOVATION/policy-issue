using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using policy_issue.Controllers;
using policy_issue.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace policy_issue.Services
{
    public class KafkaConsumer
    {
        IConsumer<Ignore, string> _kafkaConsumer;
        ConsumerConfig _consumerConfig;
        ILogger<KafkaConsumer> _logger;

        private List<string> messages = new List<string>();

        public KafkaConsumer(ILogger<KafkaConsumer> logger)
        {
            _logger = logger;
            _consumerConfig=  new ConsumerConfig
            {
                BootstrapServers = "my-cluster-kafka-bootstrap:9092",
                GroupId = "csharp-consumer",
                EnableAutoCommit = true,
                StatisticsIntervalMs = 5000,
                SessionTimeoutMs = 6000,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = true
            };;

            //SetupConsume();
        }

        // public string ConsumeMessage(){
        //     var consumeResult = _kafkaConsumer.Consume();
        //     if (consumeResult.IsPartitionEOF)
        //     {
        //         // _logger.LogInformation(
        //         //     $"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
        //         return "";

        //     }
        //     _logger.LogInformation($"Received message at {consumeResult.TopicPartitionOffset}: {consumeResult.Message.Value}");

        //     Task tcs = new Task(()=> {
        //         _kafkaConsumer.Commit(consumeResult);
        //         _logger.LogInformation("Committing offset ");
        //         });

        //     return consumeResult?.Message?.Value ?? "No message text";
        // }

        public void CloseConsume()
        {
            if(_kafkaConsumer != null) _kafkaConsumer.Close();
        }

        public void SetupConsume(DataModel model = null)
        {
            var topics = new[] { "policy" };

            // Note: If a key or value deserializer is not set (as is the case below), the 
            // deserializer corresponding to the appropriate type from Confluent.Kafka.Deserializers
            // will be used automatically (where available). The default deserializer for string
            // is UTF8. The default deserializer for Ignore returns null for all input data
            // (including non-null data).
            _kafkaConsumer = new ConsumerBuilder<Ignore, string>(_consumerConfig)
                // Note: All handlers are called on the main .Consume thread.
                .SetErrorHandler((_, e) => _logger.LogInformation($"Error: {e.Reason}"))
                .SetStatisticsHandler((_, json) => _logger.LogInformation($"Statistics: {json}"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    //_logger.LogInformation($"Assigned partitions: [{string.Join(", ", partitions)}]");
                    // possibly manually specify start offsets or override the partition assignment provided by
                    // the consumer group by returning a list of topic/partition/offsets to assign to, e.g.:
                    // 
                    // return partitions.Select(tp => new TopicPartitionOffset(tp, externalOffsets[tp]));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    //_logger.LogInformation($"Revoking assignment: [{string.Join(", ", partitions)}]");
                })
                .Build();
            {
                _kafkaConsumer.Subscribe(topics);

                Task.Factory.StartNew((parameter)=> {

                    var dataStore =  parameter as DataStore;
                    while(true)
                    {
                        var consumeResult = dataStore.consumer.Consume();
                        if (consumeResult.IsPartitionEOF)
                        {
                            _logger.LogInformation(
                                $"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                            return "";
                        }
                        if(consumeResult?.Message?.Value != null){
                            dataStore.model.SetMessage(consumeResult.Message.Value);
                        }
                        dataStore.consumer.Commit(consumeResult);

                    }

                }, new DataStore{ consumer =_kafkaConsumer, model = model, logger =_logger });
                
            }

        }
    }

    public class DataStore
    {
        public DataModel model;

        public IConsumer<Ignore, string> consumer;

        public ILogger<KafkaConsumer> logger;

    }

}

