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
    public class KafkaConsumer
    {
        IConsumer<Null, string> _kafkaConsumer;
        ILogger<KafkaConsumer> _logger;

        public KafkaConsumer(ILogger<KafkaConsumer> logger)
        {
            _logger = logger;
            try
            {
            var config = new ConsumerConfig {
                BootstrapServers = "my-cluster-kafka-bootstrap:9092",
                GroupId= "issue-group",
                EnableAutoCommit=false
            };
            _kafkaConsumer = new ConsumerBuilder<Null, string>(config).Build();
            _kafkaConsumer.Subscribe("policy");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());

            }

        }

        public string GetMessage()
        {
            try
            {
            var result = _kafkaConsumer.Consume(10000);
            return result.Message.Value;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }

}