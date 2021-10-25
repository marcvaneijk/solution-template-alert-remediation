using System.Collections.Generic;

namespace Alert.Remediation
{
    public class Mapping
    {
        public List<MappingProperties> Action { get; set; }
    }

        public class MappingProperties
    {
        public string Name { get; set; }
        public string WindowsScriptUri { get; set; }
        public string LinuxScriptUri { get; set; }
    }
}