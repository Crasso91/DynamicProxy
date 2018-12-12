using DynamicProxy.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Services
{
    public class AutoMapperService
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal static object[] Map(ParameterInfo[] parameterInfo, object datasource, object configuration)
        {
            var _params = new List<object>();
            //Per tutti i parametri in input del metodo
            foreach (var _param in parameterInfo)
            {
                logger.Debug("AutoMapperService.Map -> Mapping Parameter : " + _param.Name + " [" + _param.ParameterType + "]");
                //instanzio il parametro
                var instance = Activator.CreateInstance(_param.ParameterType);
                //Mappo le proprietà dell'oggetto
                MapProperties(instance, datasource, configuration);
                //Aggiungo il parametro alla lista dei parametri
                _params.Add(instance);
            }
            return _params.ToArray();
        }

        internal static void MapProperties(object instance, object datasource, object configuration)
        {
            //per ogni proprietà dell'instanza in input
            foreach(var property in instance.GetType().GetProperties())
            {

                var propertyType = property.PropertyType;
                logger.Debug("AutoMapperService.MapProperties -> Mapping Property : " + property.Name + " [" + propertyType + "]");

                //se il tipo della proprietà è una primitiva o String o Decimal o Datetime
                if (propertyType.IsPrimitive || propertyType == typeof(Decimal) || propertyType == typeof(String) || propertyType == typeof(DateTime))
                    MapPrimitiveProperty(instance, property, datasource, configuration);
                else //Se la proprietà è una classe
                    MapClassProperty(instance, property, datasource, configuration);

            }
        }

        public static void MapPrimitiveProperty(object instance, PropertyInfo property, object datasource, object configuration)
        {
            //Recupero il nome della proprietà nel datasource, se non configurata prende il nome della proprietà stessa
            var dsPropertyName = configuration.GetConfigColumnName(property.Name);
            //Recupero il valore della proprietà dal datasource
            var value = datasource.GetPropertyValueOrDefault(dsPropertyName);
            if (value == null) return;

            //Se ho il setter della proprietà allora imposto il valore 
            if (property.CanWrite && property.GetSetMethod(true).IsPublic)
            {
                //se il tipo recuperato dinamicamente è diverso da quello della proprietà lo converto
                if (value.GetType() != property.PropertyType)
                {
                    if (property.PropertyType != typeof(DateTime))
                        value = Convert.ChangeType(value, property.PropertyType);
                    else
                        value = Convert.ChangeType(value, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture);

                }
                property.SetValue(instance, value);
            }
        }

        public static void MapClassProperty(object instance, PropertyInfo property, object datasource, object configuration)
        {
            //instanzio la proprietà
            var childInstance = Activator.CreateInstance(property.PropertyType, null);
            //Recupero il nome della proprietà nel datasource, se non configurata prende il nome della proprietà stessa
            var dsPropertyName = configuration.GetConfigColumnName(property.Name);
            //Recupero il valore della proprietà dal datasource
            var value = datasource.GetPropertyValueOrDefault(dsPropertyName);
            if (value == null) return;

            var isList = childInstance.GetType().GetInterface(typeof(IList<>).Name) != null;
            

            //se ha un valore e non è una lista mappo le proprietà dell'oggetto istanziato
            if (!isList)
            {
                MapProperties(childInstance, value, configuration);
            }
            else 
            {
                MapListToProperty(childInstance, property, value, configuration);
            }
            
            property.SetValue(instance, childInstance);
        }

        private static void MapListToProperty(object instance, PropertyInfo property, object datasource, object configuration)
        {
            logger.Debug("AutoMapperService.MapListToProperty -> Mapping List for property : " + property.Name + " [" + instance.GetType() + "]");
            //per tutti gli oggetti del datasource
            foreach (var item in ((IList<object>)datasource))
            {
                var isListOfPrimitive = property.PropertyType.GenericTypeArguments[0].IsPrimitive || property.PropertyType.GenericTypeArguments[0] == typeof(Decimal) || property.PropertyType.GenericTypeArguments[0] == typeof(String) || property.PropertyType.GenericTypeArguments[0] == typeof(DateTime);

                if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(Decimal) || property.PropertyType == typeof(String) || property.PropertyType == typeof(DateTime))
                {
                    object _val = null;
                    if(property.PropertyType != typeof(DateTime))
                        _val = Convert.ChangeType(item, property.PropertyType);
                    else
                        _val = Convert.ChangeType(item, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture);

                    instance.GetType().GetMethod("Add").Invoke(instance, new object[] { _val });
                }
                else if (isListOfPrimitive)
                {
                    object _val = null;
                    if (property.PropertyType.GenericTypeArguments[0] != typeof(DateTime))
                        _val = Convert.ChangeType(item, property.PropertyType.GenericTypeArguments[0]);
                    else
                        _val = Convert.ChangeType(item, property.PropertyType.GenericTypeArguments[0], System.Globalization.CultureInfo.InvariantCulture);

                    instance.GetType().GetMethod("Add").Invoke(instance, new object[] { _val });
                }
                else
                {
                    //instanzio l'oggetto che prende in input la lista
                    var childinstance = Activator.CreateInstance(instance.GetType().GenericTypeArguments.First());
                    //mappo le proprietà dell'instanza
                    MapProperties(childinstance, item, configuration);
                    //aggiungio l'instanza alla lista
                    instance.GetType().GetMethod("Add").Invoke(instance, new object[] { childinstance });
                }
            }
        }

    }
}
