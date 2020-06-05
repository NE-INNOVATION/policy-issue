
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using policy_issue.Model;
using System.Collections;
using System.Linq;

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
        public string GetVariables()
        {
            string message="";

            foreach(DictionaryEntry e in System.Environment.GetEnvironmentVariables())
            {
                message += e.Key.ToString()  + ":" + e.Value.ToString();
            }

            return message;

        }

        [HttpPost("issue/{quoteId}")]
        public string Issue(string quoteId, PolicyDto requestBody)
        {
            return new JObject(
                new JProperty("policy", 
                    new JObject(
                        new JProperty("policy-number",GeneratePolicyNumber()
            )))).ToString();
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
