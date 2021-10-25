namespace Alert.Remediation
{
    public class DeploymentParameters
    {
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.management.resourcemanager.fluent.models.deploymentproperties?view=azure-dotnet
        public DeploymentParameter VmName { get; set; }
        public DeploymentParameter Location { get; set; }
        public DeploymentParameter VmExtensionName { get; set; }
        public DeploymentParameter Timestamp { get; set; }
        public DeploymentParameter ScriptUri { get; set; }
        public DeploymentParameter ScriptName { get; set; }
        public DeploymentParameter ScriptArguments { get; set; }
    }

        public class DeploymentParameter
    {
        public string Value { get; set; }
    }
}