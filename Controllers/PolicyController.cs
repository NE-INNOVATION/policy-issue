
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;

namespace policy_issue.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PolicyController : ControllerBase
    {

        private readonly ILogger<PolicyController> _logger;

        public PolicyController(ILogger<PolicyController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [EnableCors("AllowAll")]
        public string GetVariables()
        {
            string message="";

            foreach(DictionaryEntry e in System.Environment.GetEnvironmentVariables())
            {
                message += e.Key.ToString()  + "::" + e.Value.ToString() + "|||";
            }

            return message;

        }

        [HttpPost("issue/{quoteId}")]
        [Authorize]
        [EnableCors("AllowAll")]
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
        [Authorize]
        [EnableCors("AllowAll")]
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
