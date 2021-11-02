using System.Collections.Generic;

namespace CustomScript.Webhook
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