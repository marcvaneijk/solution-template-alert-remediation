using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Identity;
using System.Net.Http;

namespace Alert.Remediation
{
    public static class http_alert
    {
        [FunctionName("http_alert")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic alert = JsonConvert.DeserializeObject(requestBody);

            string resourceId = alert.resourceId;
            string alertAction = alert.action;
            string scriptArguments = alert.arguments;

            // Uri
            Uri azureManagementUri = new Uri("https://management.azure.com/");

            // Auth
            var credential = new DefaultAzureCredential();
            var token = credential.GetToken(
                new Azure.Core.TokenRequestContext(
                    new[] { (azureManagementUri.AbsoluteUri + ".default") }));

            var accessToken = token.Token;

            // Create HTTP Client
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

            // Get machine properties
            Uri azureRestVmUri = new Uri(azureManagementUri, (resourceId + "?api-version=2021-05-20"));
            var response = await client.GetAsync(azureRestVmUri);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Machine machine = JsonConvert.DeserializeObject<Machine>(content);
            
            string name = req.Query["name"];

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
