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

        public static async Task<string> SendMessage(PolicyDto policy, ILogger<PolicyController> logger)
        {
            var config = new ProducerConfig {

                BootstrapServers = "my-cluster-kafka-bootstrap:9092",
            };

            using (var p = new ProducerBuilder<Null, string>(config).Build())
            {
                try
                {
                    var dr = await p.ProduceAsync("policy", new Message<Null, string> { Value="test-with-app" });
                    Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                }
                catch (ProduceException<Null, string> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
            }
            return "success";
            // logger.LogInformation($"Current Working Directory: { Environment.CurrentDirectory }");

            // if(File.Exists("../etc/config/server.config"))logger.LogInformation("Found File");
            // else logger.LogInformation($"Didnt find file and using env value {Environment.GetEnvironmentVariable("file-path") }");

            // if(!File.Exists("./server.config") && !File.Exists(Environment.GetEnvironmentVariable("file-path")))
            // return "No files found";
            // try
            // {

            //     //var config = new Uri("http://my-connect-cluster-connect-api-development.apps.openshift.ne-innovation.com");
            // var config = LoadConfig( Environment.GetEnvironmentVariable("file-path") ?? "./server.config", null);
            // logger.LogInformation($"Bootstrap servers { config.BootstrapServers }");

            // var topic = "policy-issue";
            // await CreateTopicMaybe(topic, 1, 3, config);

            //Produce(topic, config);
            // }
            // catch(Exception ex)
            // {
            //     return ex.Message + "||" +ex.StackTrace;
            // }
            // return "Success";
        }

        static ClientConfig LoadConfig(string configPath, string certDir)
        {
            try
            {
                var cloudConfig = (File.ReadAllLines(configPath))
                    .Where(line => !line.StartsWith("#") && line.Contains('='))
                    .ToDictionary(
                        line => line.Substring(0, line.IndexOf('=')),
                        line => line.Substring(line.IndexOf('=') + 1));

                var clientConfig = new ClientConfig(cloudConfig);

                if (certDir != null)
                {
                    clientConfig.SslCaLocation = certDir;
                }

                return clientConfig;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occured reading the config file from '{configPath}': {e.Message}");
                System.Environment.Exit(1);
                return null; // avoid not-all-paths-return-value compiler error.
            }
        }

        static async Task CreateTopicMaybe(string name, int numPartitions, short replicationFactor, ClientConfig cloudConfig)
        {
            using (var adminClient = new AdminClientBuilder(cloudConfig).Build())
            {
                try
                {
                    await adminClient.CreateTopicsAsync(new List<TopicSpecification> {
                        new TopicSpecification { Name = name, NumPartitions = numPartitions, ReplicationFactor = replicationFactor } });
                }
                catch (CreateTopicsException e)
                {
                    if (e.Results[0].Error.Code != ErrorCode.TopicAlreadyExists)
                    {
                        Console.WriteLine($"An error occured creating topic {name}: {e.Results[0].Error.Reason}");
                    }
                    else
                    {
                        Console.WriteLine("Topic already exists");
                    }
                }
            }
        }
        
        static void Produce(string topic, ClientConfig config)
        {
            using (var producer = new ProducerBuilder<string, string>(config).Build())
            {
                int numProduced = 0;
                int numMessages = 10;
                for (int i=0 ; i < numMessages ; ++i)
                {
                    var key = "alice";
                    var val = JObject.FromObject(new { count = i }).ToString(Formatting.None);

                    Console.WriteLine($"Producing record: {key} {val}");

                    producer.Produce(topic, new Message<string, string> { Key = key, Value = val },
                        (deliveryReport) =>
                        {
                            if (deliveryReport.Error.Code != ErrorCode.NoError)
                            {
                                Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                            }
                            else
                            {
                                Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                                numProduced += 1;
                            }
                        });
                }

                producer.Flush(TimeSpan.FromSeconds(10));

                Console.WriteLine($"{numProduced} messages were produced to topic {topic}");
            }
        }

    }
}