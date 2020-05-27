
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using policy_issue.Model;

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

        [HttpPost("issue/{quoteId}")]
        public string Issue(string quoteId, [FromBody] PolicyDto requestBody)
        {
            return new JObject(new JProperty("policy", new JObject(new JProperty("policy-number",GeneratePolicyNumber()
            )

            ))).ToString();
        }

        private string GeneratePolicyNumber()
        {
            return RandomNumber(111111,999999).ToString() + RandomNumber(1111111,9999999).ToString();
        }

        public int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
