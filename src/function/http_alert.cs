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
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Alert.Remediation
{
    public static class http_alert
    {
        [FunctionName("http_alert")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string filePath = Path.Combine(context.FunctionAppDirectory, "scripts/restart-service.ps1");
            var fileContent = System.IO.File.ReadAllText(filePath);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic alert = JsonConvert.DeserializeObject(requestBody);

            string resourceId = alert.resourceId;
            string alertAction = alert.action;
            string scriptArguments = alert.arguments;

            // Convert to resourceId Object
            var resourceIdObject = ResourceId.FromString(resourceId);

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

            
            // Test if custom script extension exist (get name and timestamp)
            foreach (MachineResource existingExtension in machine.resources) 
            {
                // Test if OS is linux or windows and has existing vm extension
                if (machine.properties.osName == "windows" && existingExtension.properties.type == "CustomScriptExtension"){}
                else if (machine.properties.osName == "linux" && existingExtension.properties.type == "CustomScript"){}
                else {}
            }

            // Deployment
            string deploymentName = "";
            Uri azureRestDeploymentUri = new Uri(azureManagementUri, $"/subscriptions/{resourceIdObject.SubscriptionId}/resourcegroups/{resourceIdObject.ResourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}?api-version=2021-04-01");
            // response = await client.PutAsync(azureRestDeploymentUri, content);
            // response.EnsureSuccessStatusCode();
            // content = await response.Content.ReadAsStringAsync();
            // Machine machine = JsonConvert.DeserializeObject<Machine>(content);
            
            string name = req.Query["name"];

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
