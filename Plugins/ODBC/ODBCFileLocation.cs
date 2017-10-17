using System.Collections.Generic;

namespace bezlio.rdb.plugins
{
    public class ODBCFileLocation
    {
        public ODBCFileLocation() { }

        public string LocationName { get; set; }
        public string LocationPath { get; set; }
        public List<string> ContentFileNames { get; set; } = new List<string>();
    }
}
