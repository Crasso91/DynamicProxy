using DynamicProxy.WSClients.Abstract;
using DynamicProxy.WSClients.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Proxy.Abstract
{
    public interface IProxy
    {
        List<WSClient> ServicesClients { get; set; }
        void LoadServiceFromConfig();
        void Initialize();
        string CallServiceDynamically(string _serviceConfig, string _datasource, string _configuration);
    }
}
