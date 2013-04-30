using System.Collections.Generic;

namespace ArcGISSQLLog
{
    public class Service
    {
        public string name { get; set; }
        public string type { get; set; }
    }

    public class Catalog
    {
        public double currentVersion { get; set; }
        public List<string> folders { get; set; }
        public List<Service> services { get; set; }
    }
}
