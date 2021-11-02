# Custom Script Extension webhook for Azure Arc enabled servers

The code in this repository generates a solution template artifact that can be used to publish an Azure marketplace item. A deployed instance of the published marketplace item provides an HTTP webhook for executing Custom Scripts on Azure Arc enabled servers.

The goal of this solution is to minimize the complexity for integrating existing operational tooling with Azure Arc enabled servers. Consider the following scenario's

- You have an existing monitoring solution and you would like to enable auto-remediation for specific events that happen on your Azure Arc enabled servers. You are able to configure your monitoring solution with a rule that sneds an alert when a specific event occurs. The alert is typically sent to an external system (e.g. email, MS Teams, Slack, etc.) Most monitoring solutions are also able to send out an HTTP request to an external HTTP listener. The solution in this repository provides that external HTTP listener and translates the request (e.g. restart service) to the execution of a script on the specified Azure Arc enabled server.
- You query Azure Resource Manager and identify a subset of Azure Arc enabled servers you would like to install an agent on. The solution in this repository provides that external HTTP listener and translates the request (e.g. install agent) to the execution of a script on the subset of specified Azure Arc enabled servers.

## Solution

A deployed instance of the solution in this marketplace item consists of a single resource group containing
- Function App
- Storage Account
- App Service Plan

The Function App contains an HTTP listener. To HTTP listener is triggered on a HTTP POST. The body of the HTTP post requires three key/value pairs.

``` JSON
{
    "resourceId": "/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.HybridCompute/machines/{machineName}",
    "action": "restart-service",
    "arguments": "arg1 arg2"
}
```
An HTTP Post to the web listerner can only have one resourceId, one action and any number of arguments.

> Note: To change the solution in this repository to support a body with different names or json paths for the keys, update the values of the ```resourceId```, ```triggerAction``` and ```scriptArguments``` in the [Function App](/blob/main/src/function/http_trigger.cs#L34) to match the properties of the body in the post.

## Scripts

Once the POST with the body is recieved, the Function App translates the action (specified in the body of the POST) to the script that needs to be executed on the resource (specified in the body of the POST). The mapping between the specified action and the script to be executed is configured in the [mapping.json](/blob/main/src/scripts/mapping.json) file. The mapping.json file contains an array of actions that have a Uri for the related Windows Script (.ps1) and a Uri for the related Linux script (.sh). If the resourceId (provided in the POST body) is a Linux machine, the specified Linux script will be executed, if the machine is a Windows machine, the specified Windows script will be executed.

```
{
    "action": [
        {
            "name": "restart-service",
            "windowsScriptUri": "built-in",
            "linuxScriptUri": "built-in"
        },
        {
            "name": "stop-service",
            "windowsScriptUri": "https://raw.github.com/somePath/script1.ps1",
            "linuxScriptUri": "https://raw.github.com/somePath/script1.sh"
        }
    ]
}
```

The mapping.json file can updated (replacing the existing examples and/or adding additional actions). Each object in the action array of the mapping.json file must have a ```name```, ```windowsScriptUri``` and ```linuxScriptUri``` key value pair. Every action that can be specified in the body of the HTTP POST must have an object with a corresponding value for the ```name``` key, in the mapping.json file.

As you can see in the provided mappings.json the ```windowsScriptUri``` and the ```linuxScriptUri``` can have a value of ```built-in``` or reference an publically accessible HTTPS endpoint to the script.

### Scripts built-in
If the value is set to ```built-in```, the Function App uses the scripts that are provided as part of this repository. Windows script that are placed in the [windows folder](/tree/main/src/scripts/windows) and Linux scripts that are placed in the [linux folder](/tree/main/src/scripts/linux) are automatically added to the solution by the GitHub Actions workflow. 

Each deployed instance of this solution is configured with a file share in the storage account. Inside of this file share a copy of the scripts exists in ```site/wwwroot/scripts```. This folder in the share also contains the mapping.json. 

When a specific action in an HTTP POST triggers the execution of a script, where the value of the script uri in the mapping.json is set to ```built-in```, the Function App will use the name of the action, append the relevant extension to the name (.ps1 or .sh) and generate a SAS token for the script with that name in the file share with a SAS lifetime of 10 minutes. This allows the VM extension on the resource to pull the script from the file share. If the script with the same name as the action does not exist in the file share (```site/wwwroot/scripts/windows/{action}.ps1``` for a Windows machine and ```site/wwwroot/scripts/linux/{action}.sh``` does not exist, the Function App will return an error and the Script Extension will not be executed.

### Script on publically accessible HTTPS endpoint
If you do not want to include a script in the solution template, it is also possible to specify a publically accessible HTTPS endpoint for the script. This can be configured by the publisher in the mapping.json file in this repsoitory or by the consumer of this solution in the mapping.json file (in ```site/wwwroot/scripts```) of the file share of a deployed instance's storage account. 

When a specific action in an HTTP POST triggers the execution of a script, where the value of the script uri in the mapping.json is not set to ```built-in```, the Function App will use relevant uri (```windowsScriptUri``` for a Windows machine and ```linuxScriptUri``` for a Linux machine) from the mapping.json file and configure the VM Extension to pull the script from the provided Uri. Note that the VM Extension pull this content without any authentication (unless you specify a SAS token in the Uri).

## Authentication

During the deployment of the solution template the consumer is asked for a Service Principal (```AppId```, ```AppSecret``` and ```TenantId```). This Service Principal is used to trigger the execution of the VM extension on machines specified in the body of HTTP POST. The Service Princicpal must be configured in advance of the deployment by the consumer.

## Firewall

## Marketplace item

## Extensibility

## Next steps

