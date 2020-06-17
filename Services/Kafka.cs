using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using policy_issue.Controllers;
using policy_issue.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace policy_issue.Services
{
    public class KafkaService
    {
        public static async Task<string> SendMessage(string policy, ILogger<PolicyController> logger)
        {
            var config = new ProducerConfig {
                BootstrapServers = "my-cluster-kafka-bootstrap:9092",
            };

            using (var p = new ProducerBuilder<Null, string>(config).Build())
            {
                try
                {
                    var dr = await p.ProduceAsync("policy", new Message<Null, string> { Value=policy });
                    logger.LogInformation($"Delivered to topic '{dr.Topic}'");
                    return $"Delivered to topic '{dr.Topic}'";
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }
            return "success";
        }
    }
}