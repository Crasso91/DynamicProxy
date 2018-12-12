using Configuration;
using DynamicProxy.Entities.Concrete;
using DynamicProxy.Extensions;
using DynamicProxy.Proxy.Abstract;
using DynamicProxy.Services;
using DynamicProxy.WSClients.Abstract;
using DynamicProxy.WSClients.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicProxy.Proxy.Concrete
{
    /// <summary>
    /// tramite questa classe è possibile chiamare dinamicamente dei metodi di servizio per dei client che vengono caricati a runtime in base alla configurazione all'interno del file
    /// config/BaseProxy.json
    /// </summary>
    public class BaseProxy : IProxy
    {
        public List<WSClient> ServicesClients { get; set; }

        private ProxyConfig Config;
		private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        public BaseProxy()
        {
            //Recupero il file di configurazione del proxy, se non è presente lo crea
            Config = ExternalConfig.GetConfig<ProxyConfig>(GetType().Name + ".json");
            Initialize();
        }

        public void Initialize()
        {
            LoadServiceFromConfig();
            //Instanzio tutti i client configurati
            if(Config.WSClients != null)
                ServicesClients.ForEach(x=>x.Initialize());
        }
		
        public void LoadServiceFromConfig()
        {
            //Se nel config non sono definiti gli assembly da caricare o i client dei servizi da instanziare allora esco
            if (Config.WSAssemblies == null)
                throw new ArgumentNullException("Config.WSAssemblies == null");
            
            //Carico gli assembly
			Config.WSAssemblies.ForEach(x => { Assembly.LoadFile(x); });
            //Recupero i client da instanziare
            ServicesClients = Config.WSClients;
        }

        /// <summary>
        /// Metodo per invocare dinamicamente un metodo di servizio su un client tra quelli caricati a runtime in base alle configurazioni all'interno del BaseProxy.json
        /// </summary>
        /// <param name="_callConfig">definisce il nome del client sul quale invocare il metodo di servizio e il metodo di servizio da invocare
        /// {
        ///     "ServiceClientName" : "Nome del client", 
        ///     "ServiceMethodName" : "Nome del metodo da invocare"
        /// }
        /// se 'ServiceClientName' nullo o vuoto la classe si occupa di fare una ricerca del metodo su tutti i client definiti nel config 
        /// 'ServiceMethodName' non può essere ne nullo ne vuoto
        /// </param>
        /// <param name="_datasource">Il datasource con il quale fare il binding sull'oggetto che riceve in input il metodo di servizio, se non si conosce la struttura che deve avere
        /// invocare il metodo BaseProxy.GetDataSourceInfo(_callConfig) per avere un esempio della struttura
        /// </param>
        /// <param name="_configuration">Non obbligatorio, necessario solo nel caso in cui nel datasource sono presenti delle proprietà con nome diverso rispetto a quello dell'oggetto
        /// sul quel fare il binding. In quel caso va definito in questo modo:
        /// {
        ///     "NomeProprietàOggettoDestinazione" : "NomeProprietàOggettoJson"
        /// }
        /// ES.
        /// {
        ///     "Name" = "ragsoc",
        ///     "InboundShipmentPlanRequestItems" = "tt-provoci-parent",
        ///     "member" = "tt-provoci"
        /// }
        /// </param>
        /// <returns>Risultato della chiamata al servizio in formato JSON</returns>
        public string CallServiceDynamically(string _callConfig, string _datasource, string _configuration = "{}")
        {
            string _result = string.Empty;

            logger.Debug(this.GetType().Name + ".CallServiceDynamically -> CallConfig: " + _callConfig);
            logger.Debug(this.GetType().Name + ".CallServiceDynamically -> DataSource: " + _datasource);
            logger.Debug(this.GetType().Name + ".CallServiceDynamically -> DataSourceColumnDefinition: " + _configuration);

            try
            {
                //Recupero la configurazione della chiamata al servizio (nome del client e metodo da chiamare)
                var callConfig = _callConfig.DeserilizeJson<DynamicInvokeConfig>();
                //Recupero l'instanza del client del servizio
                var service = GetServiceInstance(callConfig);
                //Recupero le informazioni del metodo da invocare
                var methodInfo = service?.GetType()?.GetMethod(callConfig.ServiceMethodName);
                //Creo l'istanza della request tramite l'automapper bindando le proprietà con stessa nomenclatura (o come configurazione) tra datasource e instanza
                var request = AutoMapperService.Map(methodInfo.GetParameters(), _datasource.DeserilizeJson(), _configuration.DeserilizeJson());
                logger.Info(GetType().Name + ".CallServiceDynamically -> Calling Service: " + service.GetType().Name + "." + methodInfo.Name);
                //invoco il metodo 
                RetryService.Excecute(Config.MaxRetry, Config.RetryDelay, () => _result = methodInfo.Invoke(service, request).SerializeObject());
            }
            catch (Exception ex)
            {
                logger.Error(GetType().Name + ".CallServiceDynamically -> Deserialization | Reflection | Invoking Error", ex);
                throw;
            }
            logger.Debug(GetType().Name + ".CallServiceDynamically -> result: " + _result);
            return _result;
        }

        /// <summary>
        /// Restituisce un oggetto json di esempio che deve essere passato al metodo CallServiceDynamically
        /// </summary>
        /// <returns>Il formato della configurazione in formato JSON</returns>
        public string GetCallConfigInfo()
        {
            return "{ \"ServiceClientName\" : \"#ServiceName#\", \"ServiceMethodName\" : \"#MethodName#\"}";
        }

        /// <summary>
        /// Restituisce i costruttori del client richiesto in formato JSON. Princiaplmente viene usato per capire come impostare il cofig 'BaseProxy.json' per far instanziare correttamente i client
        /// </summary>
        /// <param name="_serviceClient">Nome del clienti del quale recuperare i costruttori</param>
        /// <returns>I costruttori in formato stringa JSON</returns>
        public string GetClientConstructorInfo(string _serviceClient)
        {

            var serviceClient = new WSClient { Name = _serviceClient };
            var constructors = serviceClient.GetConstructors();

            List<object> _result = new List<object>();

            for(var i = 0; i < constructors.Length; i++)
            {
                var constructor = constructors[i];
                var _params = new List<object>();
                foreach(var par in constructor.GetParameters())
                {
                    var _param = new { name = par.Name, type = par.ParameterType.Name };
                    _params.Add(_param);
                }

                var _constr = new
                {
                    num = i.ToString(),
                    parameters = _params
                };
                _result.Add(_constr);
            }

            return _result.SerializeObject();
        }

        /// <summary>
        /// Restituisce un esempio in formato JSON di come deve essere strutturato il datasource che deve essere passato al metodo CallServiceDynamically
        /// </summary>
        /// <param name="_callConfig">definisce il nome del client sul quale invocare il metodo di servizio e il metodo di servizio da invocare
        /// {
        ///     "ServiceClientName" : "Nome del client", 
        ///     "ServiceMethodName" : "Nome del metodo da invocare"
        /// }
        /// se 'ServiceClientName' nullo o vuoto la classe si occupa di fare una ricerca del metodo su tutti i client definiti nel config 
        /// 'ServiceMethodName' non può essere ne nullo ne vuoto
        /// </param>
        /// <returns>Il formato del datasource che si aspetta in input il metodo di servizio in formato JSON</returns>
        public string GetDatasourceInfo(string _callConfig)
        {
            string _result;
            try
            {
                var callConfig = _callConfig.DeserilizeJson<DynamicInvokeConfig>();
                var service = GetServiceInstance(callConfig);
                _result = ExampleDatasourceGeneratorService.GenerateExampleForMethodInput(service.GetType(), callConfig.ServiceMethodName);
            }
            catch (Exception ex)
            {
                logger.Error(GetType().Name + ".CallServiceDynamically -> Relection Error", ex);
                throw;
            }
            return _result;
        }

        /// <summary>
        /// Restituisce un esempio in formato JSON della response della chiamata al metodo in input
        /// </summary>
        /// <param name="_callConfig">definisce il nome del client sul quale invocare il metodo di servizio e il metodo di servizio da invocare
        /// {
        ///     "ServiceClientName" : "Nome del client", 
        ///     "ServiceMethodName" : "Nome del metodo da invocare"
        /// }
        /// se 'ServiceClientName' nullo o vuoto la classe si occupa di fare una ricerca del metodo su tutti i client definiti nel config 
        /// 'ServiceMethodName' non può essere ne nullo ne vuoto
        /// </param>
        /// <returns>Il format della response che restituisce il metodo invocato dinamicamente</returns>
        public string GetResponseInfo(string _callConfig)
        {
            string _result;
            try
            {
                var callConfig = _callConfig.DeserilizeJson<DynamicInvokeConfig>();
                var service = GetServiceInstance(callConfig);
                _result = ExampleDatasourceGeneratorService.GenerateExampleForMethodReturns(service.GetType(), callConfig.ServiceMethodName);
            }
            catch (Exception ex)
            {
                logger.Error(GetType().Name + ".CallServiceDynamically -> Reflection Error", ex);
                throw;
            }
            return _result;
        }
        /// <summary>
        /// Restituisce tutti i clients nella libreria passata in input
        /// </summary>
        /// <param name="_libraryName">nome completo della libreria</param>
        /// <returns>Elenco dei clients con elenco dei costruttori disponibili per ognuno</returns>
        public string GetClientsInLibrary(string _libraryName)
        {
            var clientWithConstructors = new List<object>();
            try
            {
                var clients = ReflectionService.GetClientsInAssembly(_libraryName);
                clients.ForEach(x => {
                    var clientWithConstructor = new
                    {
                        ClientName = x,
                        Constructors = GetClientConstructorInfo(x).DeserilizeJson()
                    };
                    clientWithConstructors.Add(clientWithConstructor);
                });
            }
            catch (Exception ex)
            {
                logger.Error(GetType().Name + ".CallServiceDynamically -> Reflection Error", ex);
                throw;
            }
            return clientWithConstructors.SerializeObject();
        }

        private object GetServiceInstance(DynamicInvokeConfig callConfig)
        {
            //Se mi è stato passato il nome del clienti lo recupero
            if (!String.IsNullOrEmpty(callConfig.ServiceClientName))
                return GetServiceClientInProxy(callConfig.ServiceClientName);
            else //altrimenti recupero il primo client che ha il metodo che mi è stato passato
            {
                try
                {
                    logger.Debug(GetType().Name + ".GetServiceInstance getting client from method name: " + callConfig.ServiceMethodName);
                    return ServicesClients.First(x => x?.Instance?.GetType().GetMethod(callConfig.ServiceMethodName) != null).Instance;
                }
                catch (Exception ex)
                {
                    logger.Error(GetType().Name + ".GetServiceInstance -> Method " + callConfig.ServiceMethodName + " not found in clinets Error", ex);
                    throw;
                }
            }
        }

        private object GetServiceClientInProxy(string serviceClient)
        {
            logger.Debug("GetServiceClientInProxy proxy: " + GetType().Name + " serviceClient : " + serviceClient);
            //Ritorno l'instanza del servizio richiesto in input
            return ServicesClients.First(x => x.Name == serviceClient).Instance;
        }
    }
}
