using DynamicProxy.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Services
{
    public class ExampleDatasourceGeneratorService
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GenerateExampleForMethodInput(Type clientName, string methodName)
        {
            List<object> instances = new List<object>();
            var methodInfo = clientName.GetMethod(methodName);
            if(methodInfo != null)
            {
                foreach (var _param in methodInfo.GetParameters())
                {
                    logger.Debug("ExampleDatasourceGeneratorService.GenerateExampleForMethodInput -> Mapping Parameter : " + _param.Name + " [" + _param.ParameterType + "]");
                    
                    var type = _param.ParameterType;
                    var instance = Activator.CreateInstance(type);
                    ConfigureInstanceProperties(instance);
                    instances.Add(instance);
                }
            }
            return instances.SerializeObject();
        }

        public static string GenerateExampleForMethodReturns(Type clientName, string methodName)
        {
            object instance = null;
            var methodInfo = clientName.GetMethod(methodName);
            if (methodInfo != null)
            {
                    var type = methodInfo.ReturnType;
                    instance = Activator.CreateInstance(type);
                    ConfigureInstanceProperties(instance);
            }
            return instance.SerializeObject();
        }
        private static void ConfigureInstanceProperties(object instance)
        {
            foreach (var _prop in instance.GetType().GetProperties())
            {
                logger.Debug("ExampleDatasourceGeneratorService.GenerateExampleForMethodInput -> Mapping Property : " + _prop.Name + " [" + _prop.PropertyType + "]");
                ConfigureInstanceProperty(instance, _prop);
            }
        }

        private static void ConfigureInstanceProperty(object instance, PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            if (!(propertyType.IsPrimitive || propertyType == typeof(Decimal) || propertyType == typeof(String) || propertyType == typeof(DateTime)))
            {

                var childInstance = Activator.CreateInstance(propertyType);
                if (childInstance == null) return;

                var isList = childInstance.GetType().GetInterface(typeof(IList<>).Name) != null;
                if (propertyType.IsPrimitive || propertyType == typeof(Decimal) || propertyType == typeof(String) || propertyType == typeof(DateTime)) return;

                //se ha un valore e non è una lista mappo le proprietà dell'oggetto istanziato
                if (!isList)
                {
                    ConfigureInstanceProperties(childInstance);
                }
                else //altrimenti riempio la lista
                {
                    ConfigureInstanceListProperty(childInstance, property);
                }
                property.SetValue(instance, childInstance);
            }
        }

        private static void ConfigureInstanceListProperty(object instance, PropertyInfo property)
        {
            if (!(property.PropertyType.IsPrimitive || property.PropertyType == typeof(Decimal) || property.PropertyType == typeof(String) || property.PropertyType == typeof(DateTime))) return;
            var childinstance = Activator.CreateInstance(instance.GetType().GenericTypeArguments.First());
            ConfigureInstanceProperties(childinstance);
            instance.GetType().GetMethod("Add").Invoke(instance, new object[] { childinstance });
        }
    }
}
