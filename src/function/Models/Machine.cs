using System.Collections.Generic;

namespace Alert.Remediation
{
    public class Machine
    {
        // https://docs.microsoft.com/en-us/rest/api/hybridcompute/machines/get#machine
        public string id { get; set; }
        public string location { get; set; }
        public MachineProperties properties { get; set; }
        public List<MachineResource> resources { get; set; }
    }

    public class MachineProperties
    {
        public string status { get; set; }
        public string osName { get; set; }
    }

    public class MachineResource
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }

        public MachineResourceProperties properties { get; set; }
    }

    public class MachineResourceProperties
    {
        public string publisher { get; set; }
        public string type { get; set; }
        public MachineResourcePropertySettings settings { get; set; }
    }

    public class MachineResourcePropertySettings
    {
        public string timestamp { get; set; }
    }
}