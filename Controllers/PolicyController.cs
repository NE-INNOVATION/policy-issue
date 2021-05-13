
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
    public class PolicyController : ControllerBase
    {

        private static List<string> PolicyData = new List<string>();

        private readonly ILogger<PolicyController> _logger;
        private readonly KafkaConsumer _consumer;

        private static string MongoConnectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION") ?? "mongodb://neion_db:neion_db_pwd@ec2-18-116-161-43.us-east-2.compute.amazonaws.com:8081,ec2-3-142-155-241.us-east-2.compute.amazonaws.com:8081,ec2-3-20-135-160.us-east-2.compute.amazonaws.com:8081/lrqi?replicaSet=rs0&authSource=admin&retryWrites=true";

        private static string MONGO_DB_NAME = Environment.GetEnvironmentVariable("MONGO_DB_NAME") ?? "lrqi";

        private static string MONGO_PolicyIssue_Collection = Environment.GetEnvironmentVariable("MONGO_POLICYISSUE_COLL") ?? "col_lrqi_policy_issue";

        public PolicyController(ILogger<PolicyController> logger, KafkaConsumer consumer)
        {
            _logger = logger;
            _consumer = consumer;
        }

        [HttpGet("config")]
        public string GetVariables()
        {
            string message = "";

            foreach (DictionaryEntry e in System.Environment.GetEnvironmentVariables())
            {
                message += e.Key.ToString() + "::" + e.Value.ToString() + "|||";
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
            if (PolicyData.Count > 0)
            {
                var content = new[] { PolicyData[0].ToString() };
                PolicyData.RemoveAt(0);
                return content[0];
            }
            return "";
        }

        [HttpPost("mongo/{collection}")]
        public string MongoUpdate(string collection, [FromBody] object content)
        {
            try
            {
                var mongo = new MongoConnector(MongoConnectionString, MONGO_DB_NAME);
                mongo.InsertData(collection, JObject.Parse(content.ToString()));
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return "Sucess";
        }

        [HttpGet("mongo")]
        public string MongoConnector([FromQuery] string database, [FromQuery] string collection, [FromQuery] string queryName, [FromQuery] string queryValue)
        {
            try
            {
                var mongo = new MongoConnector(MongoConnectionString, MONGO_DB_NAME);
                return mongo.GetCollectionData(database, collection, queryName, queryValue);
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [HttpPost("issue/{quoteId}")]
        public async Task<IActionResult> Issue(string quoteId, [FromBody] object content)
        {
            try
            {
                var policyNumber = GeneratePolicyNumber();
                var request = JObject.Parse(content.ToString());
                request["policyNumber"] = policyNumber;

                Console.WriteLine("request content here - " + content);

                var mongo = new MongoConnector(MongoConnectionString, MONGO_DB_NAME);

                mongo.InsertData(MONGO_PolicyIssue_Collection, request);

                var policyObject = mongo.GetPolicyObject(quoteId);

                policyObject.Add(new JProperty("policyNumber", policyNumber));
                policyObject.Add(new JProperty("issue-info", request));

                var message = await KafkaService.SendMessage(policyObject.ToString(), _logger);
                var message = "success";
                PolicyData.Add(policyObject.ToString());
                var finalResult = new JObject(new JProperty("policyNumber", policyNumber), new JProperty("result", new JObject(new JProperty("status", message), new JProperty("policy", policyObject))));
                return Ok(finalResult.ToString());
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return Ok();
            }
        }

        [HttpPost("publish")]
        public async System.Threading.Tasks.Task<string> publishAsync([FromBody] object requestBody)
        {
            _logger.LogInformation("Service called for publish");
            return await KafkaService.SendMessage(requestBody.ToString(), _logger);
        }

        private string GeneratePolicyNumber()
        {
            return RandomNumber(3, 9).ToString() + RandomNumber(111111, 999999).ToString() + RandomNumber(111111, 999999).ToString();
        }

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
