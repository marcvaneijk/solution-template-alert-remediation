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
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Sas;
using System.Linq;

namespace CustomScript.Webhook
{
    public static class http_trigger
    {
        [FunctionName("http_trigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("Recieved an HTTP post, starting the function");

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic trigger = JsonConvert.DeserializeObject(requestBody);

                // From webhook
                string resourceId = trigger.resourceId;
                string triggerAction = trigger.action;
                string scriptArguments = trigger.arguments;

                // Convert to resourceId Object
                var resourceIdObject = ResourceId.FromString(resourceId);

                // Uri
                Uri azureManagementUri = new Uri("https://management.azure.com/");

                // Get action <=> scriptUri mapping
                string mappingFilePath = Path.Combine(context.FunctionAppDirectory, "scripts/mapping.json");
                var mapppingContent = System.IO.File.ReadAllText(mappingFilePath);
                Mapping mapping = JsonConvert.DeserializeObject<Mapping>(mapppingContent);
                MappingProperties scriptMapping = mapping.Action.Single(x => x.Name == triggerAction);

                // Get Authentication Token
                var defaultAzureCredential = new DefaultAzureCredential();
                var token = defaultAzureCredential.GetToken(
                    new Azure.Core.TokenRequestContext(
                        new[] { (azureManagementUri.AbsoluteUri + ".default") }));

                // Create HTTP Client
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token.Token);

                // Get machine properties
                Uri azureRestVmUri = new Uri(azureManagementUri, (resourceId + "?api-version=2021-05-20"));
                var machineResponse = await httpClient.GetAsync(azureRestVmUri);
                machineResponse.EnsureSuccessStatusCode();
                var machineResponseContent = await machineResponse.Content.ReadAsStringAsync();
                Machine machine = JsonConvert.DeserializeObject<Machine>(machineResponseContent);

                // If machine is offline?
                if (machine.Properties.Status != "Connected")
                {
                    FunctionResponse functionResponse = new FunctionResponse
                    {
                        State = "notExecuted",
                        ResourceId = machine.Id,
                        ResourceStatus = machine.Properties.Status
                    };
                    return new OkObjectResult(functionResponse);
                }
                // Get extension name and timestamp
                string extensionName = "CustomScript";
                int timestamp = 1;
                foreach (MachineResource existingExtension in machine.Resources)
                {
                    // Test if OS is linux or windows and has existing vm extension
                    if (machine.Properties.OsName == "windows" && existingExtension.Properties.Type == "CustomScriptExtension")
                    {
                        extensionName = existingExtension.Name;
                        if (existingExtension.Properties.Settings.GetType().GetProperty("Timestamp") != null)
                        {
                            timestamp = int.Parse(existingExtension.Properties.Settings.Timestamp) + 1;
                        }

                        // If existing vm extension is still executing or in an error state
                        if (existingExtension.Properties.ProvisioningState != "Succeeded")
                        {
                            FunctionResponse functionResponse = new FunctionResponse
                            {
                                State = "notExecuted",
                                ResourceId = machine.Id,
                                ResourceStatus = machine.Properties.Status,
                                ExtensionResourceId = existingExtension.Id,
                                ExtensionResourceProvisioningState = existingExtension.Properties.ProvisioningState

                            };
                            return new OkObjectResult(functionResponse);
                        }

                    }
                    else if (machine.Properties.OsName == "linux" && existingExtension.Properties.Type == "CustomScript")
                    {
                        extensionName = existingExtension.Name;
                        if (existingExtension.Properties.Settings.GetType().GetProperty("Timestamp") != null)
                        {
                            timestamp = int.Parse(existingExtension.Properties.Settings.Timestamp) + 1;
                        }

                        // If existing vm extension is still executing or in an error state
                        if (existingExtension.Properties.ProvisioningState != "Succeeded")
                        {
                            FunctionResponse functionResponse = new FunctionResponse
                            {
                                State = "notExecuted",
                                ResourceId = machine.Id,
                                ResourceStatus = machine.Properties.Status,
                                ExtensionResourceId = existingExtension.Id,
                                ExtensionResourceProvisioningState = existingExtension.Properties.ProvisioningState

                            };
                            return new OkObjectResult(functionResponse);
                        }
                    }
                    else { }
                }

                // Get Script Uri
                string scriptAbsoluteUri = "";
                Uri scriptUri;
                if (machine.Properties.OsName == "windows") { scriptAbsoluteUri = scriptMapping.WindowsScriptUri; }
                else if (machine.Properties.OsName == "linux") { scriptAbsoluteUri = scriptMapping.LinuxScriptUri; }

                // Get string file name
                // If scriptUri is set to built-in, create SAS token for script.
                string scriptFileName = "";

                if (scriptAbsoluteUri == "built-in")
                {
                    // SAS config
                    if (machine.Properties.OsName == "windows") { scriptFileName = triggerAction + ".ps1"; }
                    else if (machine.Properties.OsName == "linux") { scriptFileName = triggerAction + ".sh"; }
                    ShareFileClient shareFileClient = new ShareFileClient(Environment.GetEnvironmentVariable("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"), Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE"), $"site/wwwroot/scripts/{machine.Properties.OsName}/{scriptFileName}");
                    var sasUri = shareFileClient.GenerateSasUri(ShareFileSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(10));
                    scriptUri = sasUri;
                }
                else
                {
                    scriptUri = new Uri(scriptAbsoluteUri);
                }

                // Deployment
                string templateFilePath = "";
                if (machine.Properties.OsName == "windows") { templateFilePath = Path.Combine(context.FunctionAppDirectory, "vmextension-template/windows.json"); }
                else if (machine.Properties.OsName == "linux") { templateFilePath = Path.Combine(context.FunctionAppDirectory, "vmextension-template/windows.json"); }
                var templateContent = System.IO.File.ReadAllText(templateFilePath);
                dynamic templateContentJObject = JsonConvert.DeserializeObject(templateContent);

                DeploymentInner deploymentBody = new DeploymentInner
                {
                    //Location = machine.Location,
                    Properties = new DeploymentProperties
                    {
                        Template = templateContentJObject, //templateContentObject.ToString(Formatting.None),
                        Mode = DeploymentMode.Incremental,
                        Parameters = new DeploymentParameters
                        {
                            VmName = new DeploymentParameter { Value = resourceIdObject.Name },
                            Location = new DeploymentParameter { Value = machine.Location },
                            VmExtensionName = new DeploymentParameter { Value = extensionName },
                            Timestamp = new DeploymentParameter { Value = timestamp.ToString() },
                            ScriptUri = new DeploymentParameter { Value = scriptUri.AbsoluteUri },
                            ScriptName = new DeploymentParameter { Value = Path.GetFileName(scriptUri.LocalPath) },
                            ScriptArguments = new DeploymentParameter { Value = scriptArguments }
                        }
                    }
                };

                var deplopymentBody = new StringContent(JsonConvert.SerializeObject(deploymentBody), System.Text.Encoding.UTF8, "application/json");
                string deploymentName = "vm-extension-" + resourceIdObject.Name + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                Uri azureRestDeploymentUri = new Uri(azureManagementUri, $"/subscriptions/{resourceIdObject.SubscriptionId}/resourcegroups/{resourceIdObject.ResourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}?api-version=2021-04-01");
                var deploymentResponse = await httpClient.PutAsync(azureRestDeploymentUri, deplopymentBody);
                deploymentResponse.EnsureSuccessStatusCode();
                var deploymentResponseContent = await deploymentResponse.Content.ReadAsStringAsync();
                dynamic deploymentResponseJObject = JsonConvert.DeserializeObject(deploymentResponseContent);

                string responseMessage = deploymentResponseContent;

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                log.LogError($"Caught exception: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
