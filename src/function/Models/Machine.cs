using System.Collections.Generic;

namespace CustomScript.Webhook
{
    public class Machine
    {
        // https://docs.microsoft.com/en-us/rest/api/hybridcompute/machines/get#machine
        public string Id { get; set; }
        public string Location { get; set; }
        public MachineProperties Properties { get; set; }
        public List<MachineResource> Resources { get; set; }
    }

    public class MachineProperties
    {
        public string Status { get; set; }
        public string OsName { get; set; }
    }

    public class MachineResource
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public MachineResourceProperties Properties { get; set; }
    }

    public class MachineResourceProperties
    {
        public string Publisher { get; set; }
        public string Type { get; set; }
        public string ProvisioningState { get; set; }
        public MachineResourcePropertySettings Settings { get; set; }
    }

    public class MachineResourcePropertySettings
    {
        public string Timestamp { get; set; }
    }
}