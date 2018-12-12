using DynamicProxy.Services;
using DynamicProxy.WSClients.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.WSClients.Concrete
{
    public class WSClient : IWSClient
    {
        public ConstructorParameters ConstructorParameters { get; set; }
        public string Name { get; set; }
        public object Instance { get; set; } = null;

        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// Inizializza la l'instanza del client tramite reflection cercando e caricando il tipo dagli assembly caricati dall'applicazione
        /// Non viene instanziato nel caso in cui i parametri sono nulli
        /// </summary>
        public void Initialize()
        {
            logger.Debug("WSClient " + Name + " Initialization");
            //Recupero il tipo del client dagli assembly caricati a runtime
            var type = ReflectionService.GetTypeFromAsseblies(Name);
            if (type != null)
            {
                var initializedParameters = GetTypedParameters();

                //creo un'instanza della classe solo nel caso in cui il tipo ha un construttore senza parametri o la proprietà ConstructorParameters è definita
                if (ReflectionService.ExistConstructorWithoutParams(type) || !ReflectionService.ExistConstructorWithoutParams(type) && ConstructorParameters != null)
                {
                    Instance = Activator.CreateInstance(type: type, args: initializedParameters.ToArray());

                }
                else //Segnalo nel log come avere informazioni sui parametri del client
                logger.Info("WSClient : " + Name + "Attenzione parametri del construttore non impostati, richiamare il metodo BaseProxy.GetClientConstructorInfo(nomeServizio) /r/n per avere ulteriori informazioni in merito a cosa sia obbligatorio per instanziare la classe");
            }
            else
            {
                logger.Debug("Type of : " + Name);
                throw new ArgumentNullException("Type of : " + Name);
            }
                
        }
        /// <summary>
        /// Metodo per recuperare i costruttore del client
        /// </summary>
        /// <returns></returns>
        public ConstructorInfo[] GetConstructors()
        {
            ConstructorInfo[] constructor;
            var type = ReflectionService.GetTypeFromAsseblies(Name);
            if (type != null)
            {
                constructor = type.GetConstructors();
            }
            else
            {
                logger.Debug("Type of : " + Name);
                throw new ArgumentNullException("Type of : " + Name);
            }
            return constructor;
        }

        /// <summary>
        /// Metodo per creare un'instanza del tipo tramite i parametri passati in input
        /// </summary>
        /// <param name="_params">Parametri necessari all'instanziamento della classe</param>
        public void Initialize(List<object> _params)
        {
            if (_params == null)
            {
                throw new ArgumentNullException(nameof(_params));
            }

            var type = ReflectionService.GetTypeFromAsseblies(Name);
            if (type != null)
            {
                Instance = Activator.CreateInstance(type, _params);
            }
            else
            {
                logger.Debug("Type of : " + Name);
                throw new ArgumentNullException("Type of : " + Name);
            }
        }
        private List<object> GetTypedParameters()
        {
            var result = new List<object>();
            foreach (var par in ConstructorParameters)
            {
                object _initPar = null;
                if (!"string,int".Contains(par.Type.ToLower()))
                {
                    var _type = ReflectionService.GetTypeFromAsseblies(par.Type);
                    _initPar = Newtonsoft.Json.JsonConvert.DeserializeObject(par.Value.ToString(), type: _type);
                }
                else
                    _initPar = Convert.ChangeType(par.Value, par.Value.GetType());
                result.Add(_initPar);
            }
            return result;
        }
    }
}
