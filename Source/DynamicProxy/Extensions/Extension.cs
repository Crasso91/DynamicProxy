using DynamicProxy.Proxy.Abstract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Extensions
{
    public static class Extension
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static T GetPropertyValueOrThrow<T>(this object _in, string _propName)
        {
            try
            {
                JObject attributesAsJObject = (JObject)_in;
                Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

                var _result = values.First(x => x.Key == _propName).Value;
                return (T)_result;
                //return (T)_in.GetType().GetProperty(_propName).GetValue(_in, null);
            }
            catch (Exception ex)
            {
                logger.Error("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrThrow(" + _propName + ")", ex);
                throw new ArgumentNullException(_propName + " not found in datasource given in input", ex);
            }
        }

        public static T GetPropertyValueOrThrow<T>(this IProxy _in, string _propName)
        {
            try
            {
                return (T)_in.GetType().GetProperty(_propName).GetValue(_in, null);
            }
            catch (Exception ex)
            {
                logger.Error("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrThrow(" + _propName + ")", ex);
                throw new ArgumentNullException(_propName + " not found in datasource given in input", ex);
            }
        }

        public static List<object> GetPropertyValueOrThrow(this object _in, string _propName)
        {
            try
            {
                JObject attributesAsJObject = (JObject)_in;
                Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

                var _result = values.First(x => x.Key == _propName).Value;
                return ((JArray)_result).Select(x => (dynamic)x).ToList();
            }
            catch (Exception ex)
            {
                logger.Error("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrThrow(" + _propName + ")", ex);
                throw new ArgumentNullException(_propName + " not found in datasource given in input");
            }
        }

        public static dynamic GetPropertyValueOrDefault(this object _in, string _propName, dynamic _default = null)
        {
            try
            {

                JObject attributesAsJObject = (JObject)_in;
                Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

                var _result = values.First(x => x.Key == _propName).Value;
                if(_result.GetType() == typeof(JArray))
                {
                    return ((JArray)_result).Select(x => (dynamic)x).ToList();
                } 
                return _result;
               
            }
            catch (Exception ex)
            {
                logger.Debug("Extensions.GetPropertyValueOrDefault -> PropName: " + _propName + " [" + _in.GetType() + "]");
                return _default;
            }
        }

        //public static List<dynamic> GetPropertyValueOrDefault(this object _in, string _propName, List<dynamic> _default = null)
        //{
        //    try
        //    {
        //        JObject attributesAsJObject = (JObject)_in;
        //        Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

        //        var _result = values.First(x => x.Key == _propName).Value;
        //        return ((JArray)_result).Select(x => (dynamic)x).ToList<dynamic>();
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Debug("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrDefault-> DefaultValue : " + _propName);
        //        return _default;
        //    }
        //}

        //public static int GetPropertyValueOrDefault(this object _in, string _propName, int _default = 0)
        //{
        //    try
        //    {
        //        JObject attributesAsJObject = (JObject)_in;
        //        Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

        //        return (int)values.First(x => x.Key == _propName).Value;


        //        //return (int)_in.GetType().GetProperty(_propName).GetValue(_in, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Debug("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrDefault-> DefaultValue : " + _propName);
        //        return _default;
        //    }
        //}

        //public static object GetPropertyValueOrDefault(this object _in, string _propName, object _default = null)
        //{
        //    try
        //    {

        //        JObject attributesAsJObject = (JObject)_in;
        //        Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

        //        return (object)values.First(x => x.Key == _propName).Value;

        //        //return (T)_in.GetType().GetProperty(_propName).GetValue(_in, null);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Debug("param: " + nameof(_in) + " Type: [" + _in.GetType() + "] - GetPropertyValueOrDefault-> DefaultValue : " + _propName);
        //        return _default;
        //    }
        //}

        public static string GetConfigColumnName(this object _in, string _propName)
        {
            try
            {

                JObject attributesAsJObject = (JObject)_in;
                Dictionary<string, object> values = attributesAsJObject.ToObject<Dictionary<string, object>>();

                var _result = values.First(x => x.Key == _propName).Value;
                return (string)_result;

            }
            catch (Exception ex)
            {
                return _propName;
            }
            //return _in.GetPropertyValueOrDefault(_propName, _propName);
        }

        public static object DeserilizeJson(this string _in)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject(_in);
        }

        public static T DeserilizeJson<T>(this string _in)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(_in);
        }

        public static string SerializeObject<T>(this T _in)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(_in);
        }

        public static void SetValueFromDataSourceOrDefault(this string _in, object _datasource, string _propName)
        {
            try
            {
                _in = (string)_datasource.GetType().GetProperty(_propName).GetValue(_in, null);
            }
            catch
            {
                _in = string.Empty;
            }
        }
    }
}
