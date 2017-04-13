using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bezlio.rdb.plugins
{
    class VisualConnection
    {
        public VisualConnection() { }
        public string ConnectionName { get; set; }
        public string InstanceName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
