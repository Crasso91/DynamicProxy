using DynamicProxy.Proxy.Abstract;
using DynamicProxy.WSClients.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Services
{
    public class ReflectionService
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static object CreateIstance(string _type)
        {
            object _serviceIstance = null;
            try
            {
                foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains(_type))
                        {
                            _serviceIstance = Activator.CreateInstance(type);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            return _serviceIstance;
        }
        
        internal static Type GetTypeFromAsseblies(string typeName)
        {
            Type _serviceType = null;
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name.Contains(typeName))
                        {
                            return type;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            return _serviceType;
        }
        /// <summary>
        /// Verifica che il tipo in input abbia definito un costruttore senza parametri in input
        /// </summary>
        /// <param name="type">Il tipo da verificare</param>
        /// <returns>true se ha un costruttore senza parametri</returns>
        public static bool ExistConstructorWithoutParams(Type type)
        {
            var result = false;
            var constructors = type.GetConstructors();
            foreach(var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                result = parameters == null || (parameters != null && parameters.Count() == 0);
                if (result) break;
            }
            return result;
        }

        /// <summary>
        /// Invoca il metodo passato in input passando i parametri
        /// </summary>
        /// <typeparam name="T">Tipo di ritorno</typeparam>
        /// <param name="istance">istanza dell'oggetto sul quale invocare il metodo</param>
        /// <param name="methodName">metodo da invocare</param>
        /// <param name="_params">i parametri che il metodo si aspetta in input</param>
        /// <returns></returns>
        public static T InvokeMethod<T>(object istance, string methodName, object[] _params)
        {
            try
            {
                var methodInfo = istance.GetType().GetMethod(methodName);
                return (T)methodInfo.Invoke(istance, _params);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// Invoca il metodo statico passato in input passando i parametri
        /// </summary>
        /// <typeparam name="T">Tipo di ritorno</typeparam>
        /// <param name="type">tipo dell'oggetto sul quale invocare il metodo</param>
        /// <param name="methodName">metodo da invocare</param>
        /// <param name="_params">i parametri che il metodo si aspetta in input</param>
        /// <returns></returns>
        public static T InvokeStaticMethod<T>(Type type, string methodName, object[] _params)
        {
            try
            {
                var methodInfo = type.GetMethod(methodName);
                if (methodInfo != null)
                    return (T)methodInfo.Invoke(null, _params);
                else return default(T);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal static List<string> GetClientsInAssembly(string libraryName)
        {
            List<string> _serviceType = new List<string>();
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x=>x.CodeBase.ToLower().Contains(libraryName.ToLower())).ToList())
                {
                    assembly.GetTypes().Where(x => x.Name.ToLower().Contains("client")).ToList().ForEach(x=> {
                        _serviceType.Add(x.Name);
                    });
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            return _serviceType;
        }
    }
}
