
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using policy_issue.Model;
using System.Collections;
using System.Linq;
using policy_issue.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace policy_issue.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PolicyController : ControllerBase
    {

        private readonly ILogger<PolicyController> _logger;
        private readonly KafkaConsumer _consumer;

        private static string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://mongodb_user:mongodb_password@mongodb:27017/mongodb?replicaSet=rs0";

        public PolicyController(ILogger<PolicyController> logger, KafkaConsumer consumer)
        {
            _logger = logger;
            _consumer =  consumer;
        }

        [HttpGet("config")]
        public string GetVariables()
        {
            string message="";

            foreach(DictionaryEntry e in System.Environment.GetEnvironmentVariables())
            {
                message += e.Key.ToString()  + "::" + e.Value.ToString() + "|||";
            }

            return message;

        }

        [HttpGet("message")]
        public List<string> GetMessage([FromQuery] long time)
        {
            return _consumer.SetupConsume((time > 20000 || time == 0 )?4000 : time );
        }

        [HttpGet("mongo")]
        public string MongoConnector([FromQuery] string database, [FromQuery] string collection, [FromQuery] string queryName, [FromQuery] string queryValue)
        {
            try
            {
            var mongo = new MongoConnector(MongoConnectionString);
            return mongo.GetCollectionData(database,collection, queryName,queryValue);
            }
            catch(Exception ex)
            {
                return ex.ToString();
            }
        }

        [HttpPost("issue/{quoteId}")]
        public async Task<IActionResult> Issue(string quoteId,[FromBody] object content)
        {
            var request = JObject.Parse(content.ToString());
            
            var policyInfo = new JObject(
                new JProperty("policy", 
                    new JObject(
                        new JProperty("policy-number",GeneratePolicyNumber()
            ),new JProperty("policy-info", request))));
            var policyToken = policyInfo.SelectToken("policy");

            var message = await KafkaService.SendMessage(policyInfo.ToString(), _logger);
            var finalResult = new JObject(new JProperty("result",new JObject(new JProperty("status",message),new JProperty("policy",policyToken))));
            return Ok(finalResult.ToString());

        }

        [HttpPost("publish")]
        public async System.Threading.Tasks.Task<string> publishAsync( Object requestBody)
        {
            _logger.LogInformation("Service called for publish");
            return await KafkaService.SendMessage(requestBody.ToString(), _logger);
        }

        private string GeneratePolicyNumber()
        {
            return RandomNumber(3,9).ToString()+ RandomNumber(111111,999999).ToString() + RandomNumber(111111,999999).ToString();
        }

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
