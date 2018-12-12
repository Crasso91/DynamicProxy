using Configuration.Entities;
using DynamicProxy.WSClients.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configuration
{
    public partial class ProxyConfig : JsonConfig
    {
        public List<string> WSAssemblies { get; set; }
        public List<WSClient> WSClients { get; set; }
        public int MaxRetry { get; set; }
        public int RetryDelay { get; set; }
    }
}
