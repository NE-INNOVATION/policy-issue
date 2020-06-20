
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
using Microsoft.AspNetCore.Cors;

namespace policy_issue.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors]
    public class PolicyController : ControllerBase
    {

        private readonly ILogger<PolicyController> _logger;
        private readonly KafkaConsumer _consumer;

        private static string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://lrqi_db:lrqi_db_pwd@lrqidb-shard-00-00-wksjy.mongodb.net:27017,lrqidb-shard-00-01-wksjy.mongodb.net:27017,lrqidb-shard-00-02-wksjy.mongodb.net:27017/test?authSource=admin&replicaSet=LRQIDB-shard-0&readPreference=primary&retryWrites=true&ssl=true";

        private static string MONGO_DB_NAME = Environment.GetEnvironmentVariable("MONGO_DB_NAME") ?? "lrqi";

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

        [HttpGet("policyData")]
        public string GetPolicyData([FromQuery] string quoteId)
        {
           var mongo = new MongoConnector(MongoConnectionString, MONGO_DB_NAME);
            var policyObject = mongo.GetPolicyObject(quoteId);
            return policyObject.ToString();

        }


        [HttpGet("message")]
        public string GetMessage([FromQuery] long time)
        {
            return _consumer.SetupConsume((time > 20000 || time == 0 )?4000 : time ).FirstOrDefault();
        }

        [HttpGet("mongo")]
        public string MongoConnector([FromQuery] string database, [FromQuery] string collection, [FromQuery] string queryName, [FromQuery] string queryValue)
        {
            try
            {
            var mongo = new MongoConnector(MongoConnectionString,MONGO_DB_NAME);
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
            var policyNumber = GeneratePolicyNumber();
            var request = JObject.Parse(content.ToString());
            
            var mongo = new MongoConnector(MongoConnectionString, MONGO_DB_NAME);
            var policyObject = mongo.GetPolicyObject(quoteId);

            policyObject.Add(new JProperty("PolicyNumber", policyNumber));
            policyObject.Add(new JProperty("issue-info", request));
            
            var message = await KafkaService.SendMessage(policyObject.ToString(), _logger);
            var finalResult = new JObject(new JProperty("policyNumber", policyNumber), new JProperty("result",new JObject(new JProperty("status",message),new JProperty("policy",policyObject))));
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
