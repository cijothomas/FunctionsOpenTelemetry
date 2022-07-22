using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FunctionsOpenTelemetry
{
    public static class Function1
    {
        internal static Meter MyMeter = new Meter("FunctionsOpenTelemetry.MyMeter");
        internal static Counter<long> MyCounter = MyMeter.CreateCounter<long>("MyCounter");

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogWarning("C# HTTP trigger function processed a request.");
            MyCounter.Add(1, new("name", "apple"), new("color", "red"));

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            log.LogWarning("Name is {name}", name);
            Activity.Current?.SetTag("name", name);

            return new OkObjectResult(responseMessage);
        }
    }
}
