using DynamicProxy.WSClients.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.WSClients.Abstract
{
    public interface IWSClient
    {
        string Name { get; set; }
        object Instance { get; set; }

        ConstructorParameters ConstructorParameters { get; set; }
        void Initialize();
        void Initialize(List<object> _params);
    }
}
