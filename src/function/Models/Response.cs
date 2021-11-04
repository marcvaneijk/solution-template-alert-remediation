namespace CustomScript.Webhook
{
    public class FunctionResponse
    {
        public string Result { get; set; }
        public string Description { get; set; }
        public string ResourceId { get; set; }
        public string ResourceStatus { get; set; }
        public string ExtensionResourceId { get; set; }
        public string ExtensionResourceProvisioningState { get; set; }
    }
}