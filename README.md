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
To change the solution in this repository to support different names or json paths for the keys, update 

// Picture

## Scripts

## Security

## Authentication

## Marketplace item

## Extensibility

## Next steps

