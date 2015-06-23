using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using Couchbase.Extensions;
using Couchbase;
using Enyim.Caching.Memcached;
using Newtonsoft.Json;
using NLog.Couchbase;
using OB.Log.Couchbase;
using Couchbase.Configuration;
using Enyim.Caching.Configuration;


namespace NLog.Couchbase
{
    /// <summary>
    /// NLog message target for Couchbase NoSql database.
    /// This NLog target is dependent on version 1.3.10.0 of the Couchbase assemblies (SDK 2.0.3)
    /// Please download the Couchbase SDK 2.0.3 from here <see href="http://packages.couchbase.com.s3.amazonaws.com/clients/net/2.0/Couchbase-Net-Client-2.0.3.zip"/>
    /// </summary>
    [Target("Couchbase")]
    public class CouchbaseTarget : Target
    {
        private CouchbaseClient _couchbaseClient;

        private Queue<Tuple<string, object>> _objectsToPersist;
        private Timer _flushBackgroundWorker;
        private Mutex _lock;

        /// <summary>
        /// Default Constructor.
        /// It initializes the internal background worker that flushes all the LogEventInfos to the Couchbase db.
        /// <see cref="_flushBackgroundWorker"/>
        /// </summary>
        public CouchbaseTarget()
        {
            _lock = new Mutex(false);
            FlushInterval = TimeSpan.FromSeconds(12);
            _objectsToPersist = new Queue<Tuple<string, object>>(50000);
            _flushBackgroundWorker = new Timer(new TimerCallback(StoreAll), _objectsToPersist, (int)FlushInterval.TotalMilliseconds, Timeout.Infinite);
            Servers = new List<Server>();
            Mappings = new List<Properties>();
            _flatExcludes = new List<Exclude>();
            _flatIncludes = new List<Include>();
            _flatIncludeLayouts = new List<Layout>();
        }

        #region Fields

        /// <summary>
        /// TimeSpan with the period to run the background worker that flushes all the objects to Couchbase NoSql database.
        /// </summary>
        public TimeSpan FlushInterval { get; set; }

        /// <summary>
        /// Name of the Couchbase Bucket. This field is required in the NLog configuration.
        /// This is used to connect to the right Bucket (aka Database name in a RDBMS).
        /// </summary>
        [RequiredParameter]
        public string Bucket { get; set; }

        /// <summary>
        /// Password used for the Couchbase Bucket.
        /// </summary>
        public string BucketPassword { get; set; }

        /// <summary>
        /// List of server URIs used for the different couchbase servers.
        /// With Couchbase, the Load Balancing is done on the client-side (smart LB).
        /// Example:
        /// <code>
        /// &gt;target name="couchbase" xsi:type="Couchbase" bucket="system_logging" bucketPassword="vagrant" serializeOptions="Parameters"&lt;
        ///     &gt;server uri="http://192.168.56.101:8091/pools" /&lt;
        ///     &gt;server uri="http://192.168.56.102:8091/pools" /&lt;
        ///&gt;/target&lt;
        /// </code>
        /// </summary>
        [ArrayParameter(typeof(NLog.Couchbase.Server), "server")]
        public IList<NLog.Couchbase.Server> Servers { get; set; }

        /// <summary>
        /// Default Layout that is used if SerializeOptions is not configured (SerializeOptions.None).
        /// </summary>
        public Layout Layout { get; set; }

        /// <summary>
        /// Layout for the object key used while inserting the object in the bucket.
        /// </summary>
        public Layout Key { get; set; }

        /// <summary>
        /// DocumentSource options for the target.
        /// If DocumentSource is not given (equal to None) the Layout is used as the object document/value.
        /// If DocumentSource is equal to Properties it will use all the properties in the LogEventInfo for the Couchbase document value.
        /// If DocumentSource is equal to Parameters it will use the first parameter in the LogEventInfo as the Couchbase document value. If there are multiple parameters it will use all parameters as an array of objects for the Couchbase document.
        /// </summary>
        public DocumentSource DocumentSource { get; set; }

        /// <summary>
        /// Couchbase document (value) format. By default it's text. Change this in the configuration to JSON to store the document/value as a JSON object.
        /// </summary>
        public DocumentFormat DocumentFormat { get; set; }

        /// <summary>
        /// Couchbase document (value) expiration timespan. By default, the value is stored indefinitely. Change this value to define a document expiration Timespan according to the Couchbase API.
        /// </summary>
        public TimeSpan? DocumentExpiration { get; set; }

        private List<Exclude> _flatExcludes;
        private List<Include> _flatIncludes;
        private List<Layout> _flatIncludeLayouts;
        

        /// <summary>
        /// Array of Properties to exclude/include from serialization.
        /// </summary>
        [ArrayParameter(typeof(Properties), "mappings")]
        public IList<Properties> Mappings { get; set; }

      
        
        #endregion
      
        /// <summary>
        /// Initializes the target. Can be used by inheriting classes
        /// to initialize logging.
        /// </summary>
        /// <exception cref="NLog.NLogConfigurationException">Can not resolve MongoDB ConnectionString. Please make sure the ConnectionString property is set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            if (string.IsNullOrEmpty(Bucket))
                throw new NLogConfigurationException("Can not resolve Couchbase bucket. Please make sure the bucket property is set.");

            if (Servers == null || Servers.Count == 0)
                throw new NLogConfigurationException("Can not find any configured Couchbase servers. Please make sure there is at least one Couchbase server element set as child of the target.");
                    
            var couchbaseConfig = new CouchbaseClientConfiguration()
            {
                Bucket = this.Bucket,
                BucketPassword = this.BucketPassword                
            };

            foreach (var server in Servers)
            {
                if (string.IsNullOrWhiteSpace(server.Uri))
                    throw new NLogConfigurationException("NLog.Couchbase target server URI attribute is not well defined (empty values not accepted).");
                couchbaseConfig.Urls.Add(new Uri(server.Uri));
            }

            try
            {
                _couchbaseClient = new CouchbaseClient(couchbaseConfig);
            }
            finally
            {
                _couchbaseClient.NodeFailed += OnCouchbaseClient_NodeFailed;
            }

            if (Mappings != null && Mappings.Count > 0)
            {              
                _flatIncludes = new List<Include>();
                _flatExcludes = new List<Exclude>();
                foreach (var propertiesList in this.Mappings)
                {
                    foreach (var exclude in propertiesList.Excludes)
                    {
                        _flatExcludes.Add(exclude);
                    }
                    foreach(var include in propertiesList.Includes)
                    {
                        _flatIncludes.Add(include);
                        if (include.Context == ContextType.Layout)
                        {
                            _flatIncludeLayouts.Add(Layout.FromString(include.Name));
                        }
                    }
                }
            }
        }

      

        /// <summary>
        /// Writes an array of logging events to the log target. By default it iterates on all
        /// events and passes them to "Write" method. Inheriting classes can use this method to
        /// optimize batch writes.
        /// </summary>
        /// <param name="logEvents">Logging events to be written out.</param>
        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            if (logEvents == null || logEvents.Length == 0)
                return;

            try
            {
                foreach (var logEvent in logEvents)
                {
                    Write(logEvent);                  
                }

                foreach (var logEvent in logEvents)
                {
                    logEvent.Continuation(null);
                }
              
                //foreach (var ev in logEvents)
                //    ev.Continuation(null);
            }
            catch (Exception ex)
            {
                if (ex is StackOverflowException || ex is ThreadAbortException || ex is OutOfMemoryException || ex is NLogConfigurationException)
                    throw;

                InternalLogger.Error("Error when writing to Couchbase {0}", ex);

                foreach (var ev in logEvents)
                    ev.Continuation(ex);
            }
        }

        /// <summary>
        /// Writes logging event to the log target.
        /// classes.
        /// </summary>
        /// <param name="logEvent">Logging event to be written out.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            try
            {                          
                string renderedKey = Key != null ? Key.Render(logEvent) : null;

                string key = !string.IsNullOrWhiteSpace(renderedKey) ? Key.Render(logEvent) : "NLOG_" + Guid.NewGuid();

                string renderedValue = null;

                if (Layout != null)
                {
                    renderedValue = Layout.Render(logEvent);
                }

                bool hasProperties = logEvent.Properties != null && logEvent.Properties.Count > 0;

                bool hasParameters = logEvent.Parameters != null && logEvent.Parameters.Length > 0;

                
                bool hasMultipleParameters = logEvent.Parameters != null && logEvent.Parameters.Length > 1;
           
                if (DocumentSource == DocumentSource.Properties && hasProperties)
                {
                    var props = logEvent.Properties;
                    
                    if (_flatExcludes != null && _flatExcludes.Any())
                    {
                        props = props.Where(x => !_flatExcludes.Any(y=> object.Equals(x.Key, y.Name)
                            && y.Context == ContextType.Properties)).ToDictionary(x => x.Key, x => x.Value);
                    }

                    props = MapIncludes(props, logEvent);

                    AddToQueue(key, props);
                }
                else if (DocumentSource == DocumentSource.Parameters && hasMultipleParameters)
                {
                    AddToQueue(key, logEvent.Parameters);
                }
                else if(DocumentSource == DocumentSource.Parameters && hasParameters)
                {
                    AddToQueue(key, logEvent.Parameters[0]);
                }
                else if (DocumentSource == DocumentSource.All)
                {
                    IDictionary<object, object> obj = new Dictionary<object, object>();
                    obj = MapIncludes(obj, logEvent);                    
                    obj = Filter(obj, logEvent);
                   
                    AddToQueue(key, obj);
                }
                else
                {
                    AddToQueue(key, hasParameters && DocumentSource == DocumentSource.Parameters ? logEvent.Parameters[0] : renderedValue);
                }        
            }
            catch (Exception ex)
            {
                if (ex is StackOverflowException || ex is ThreadAbortException || ex is OutOfMemoryException || ex is NLogConfigurationException)
                    throw;

                InternalLogger.Error("Error when writing to Couchbase buket {0}, cause: {1}", this.Bucket,ex);
            }
        }

        protected virtual void AddToQueue(string key, object value)
        {            
            this._objectsToPersist.Enqueue(new Tuple<string, object>(key, value));
        }

        /// <summary>
        /// Method that actually writes to the Couchbase Bucket using the defined document format.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        protected virtual void Store(string key, object value)
        {
            bool result = false;

            if (DocumentFormat == OB.Log.Couchbase.DocumentFormat.JSON)
            {                
                if (!this.DocumentExpiration.HasValue)
                    result = _couchbaseClient.StoreJson(StoreMode.Add, key, value);
                else result = _couchbaseClient.StoreJson(StoreMode.Add, key, value, this.DocumentExpiration.Value);
            }
            else
            {
                if(!this.DocumentExpiration.HasValue)
                    result = _couchbaseClient.Store(StoreMode.Add, key, value);
                else result = _couchbaseClient.Store(StoreMode.Add, key, value, this.DocumentExpiration.Value);
            }

            if (!result)
            {            
                object existingObj = null;

                try
                {
                    existingObj = _couchbaseClient.Get(key);
                }
                catch(Exception e)
                {
                    throw new Exception(string.Format("Error when connection to Couchbase \"StoreJson:\" Key:{0} Value:{1}", key, value), e);
                }

                if (existingObj != null)
                {
                    throw new KeyAlreadyExistsException(string.Format("couchbase bucket \"{0}\" already contains an object with key \"{1}\"", this.Bucket, key));
                }

                throw new Exception(string.Format("Error while saving object to Couchbase \"StoreJson:\" Key:{0} Value:{1} .Please check your credentials.", key, value));
                
            }
        }

        protected override void CloseTarget()
        {
 	         base.CloseTarget();
        } 

        protected virtual void StoreAll(object state)
        {
            try
            {                
                _lock.WaitOne(); //It's better not to use lock(SyncRoot)
                var exceptions = new List<Exception>();
                Tuple<string, object> record;
                int objectsToPersist = this._objectsToPersist.Count;
                while (this._objectsToPersist.Count > 0)
                {
                    record = this._objectsToPersist.Dequeue();

                    try
                    {
                        Store(record.Item1, record.Item2);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }

                //if (exceptions.Count < objectsToPersist && objectsToPersist > 0)

                if (exceptions.Count > 0)
                    throw new AggregateException("NLog.CouchbaseTarget failed while saving objects", exceptions);
                
            }
            catch (Exception e)
            {
                if (state is AsyncContinuation)
                {
                    ((AsyncContinuation)state).Invoke(e);
                }
                else
                {
                    if (e is AggregateException)
                    {
                        AggregateException aggregatedException = (AggregateException)e;
                        InternalLogger.Error(e.Message);
                        foreach (var innerException in aggregatedException.InnerExceptions)
                        {
                            InternalLogger.Error("Couchbase NLOG target could not write data. Cause:{0}", e);
                        }
                    }
                    
                }
            }
            finally
            {
                _lock.ReleaseMutex();
                
                //Activates the timer again. It makes sure that the worker only runs after store is done for all objects.
                this._flushBackgroundWorker.Change((int)this.FlushInterval.TotalMilliseconds, Timeout.Infinite);

                if (state is AsyncContinuation)
                { 
                    ((AsyncContinuation)state).Invoke(null);
                }                
            }
        }

        protected void OnCouchbaseClient_NodeFailed(IMemcachedNode obj)
        {
            InternalLogger.Error("Error while connecting to Couchbase. Endpoint: {0}", obj.EndPoint.Address);
        }


        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            StoreAll(asyncContinuation);            
        }

        /// <summary>
        /// Implements the exlude mappings config element.
        /// Filters the given dictionary of properties with the property names to exclude returning a new object.
        /// </summary>
        /// <param name="properties">The reference Dictionary with the properties to filter.</param>
        /// <param name="toExclude">The list of property names to filter.</param>
        /// <returns>A new dictionary with the filtered properties</returns>
        private IDictionary<object, object> Filter(IDictionary<object, object> properties, List<string> toExclude)
        {
            if (toExclude == null || toExclude.Count == 0)
                return properties;

            var newProps = new Dictionary<object, object>();
            foreach (var property in properties)
            {
                if (!(property.Key is string) || !toExclude.Any(x=> x.Equals((string)(property.Key), StringComparison.InvariantCultureIgnoreCase)))
                {
                    newProps.Add(property.Key, property.Value);
                }
            }
            return newProps;
        }

        /// <summary>
        /// Processes the Includes in the Mappings config element.
        /// Maps include properties to different names and returns the modified Dictionary (same reference as the given one).
        /// </summary>
        /// <param name="newObj"></param>
        /// <param name="logEvent">Used for context=EventInfo.</param>
        /// <returns></returns>
        private IDictionary<object, object> MapIncludes(IDictionary<object, object> newObj, LogEventInfo logEvent)
        {            
            foreach (var include in _flatIncludes)
            {
                if (include.Context == ContextType.GDC)
                {
                    newObj[string.IsNullOrEmpty(include.To) ? include.Name : include.To] = GlobalDiagnosticsContext.Get(include.Name);
                }
                else if (include.Context == ContextType.MDC)
                {
                    newObj[string.IsNullOrEmpty(include.To) ? include.Name : include.To] = MappedDiagnosticsContext.Get(include.Name);
                }
                else if (include.Context == ContextType.Layout)
                {
                    Layout customLayout = new NLog.Layouts.SimpleLayout(include.Name);
                    string value = customLayout.Render(logEvent);
                    newObj[string.IsNullOrEmpty(include.To) ? include.Name : include.To] = value;
                }
                else if (include.Context == ContextType.EventInfo)
                {
                    if (include.Name.Equals("loggerName", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.LoggerName;

                    if (include.Name.Equals("level", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] =  logEvent.Level;

                    if (include.Name.Equals("message", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.Message;

                    if (include.Name.Equals("parameters", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.Parameters;

                    if (include.Name.Equals("properties", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var filteredProps = Filter(logEvent.Properties, _flatExcludes.Where(x => x.Context == ContextType.Properties).Select(x => x.Name).ToList());
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = filteredProps;
                    }

                    if (logEvent.Exception != null && include.Name.Equals("exception", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.Exception;

                    if (include.Name.Equals("timeStamp", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.TimeStamp;

                    if (logEvent.StackTrace != null && include.Name.Equals("stackTrace", StringComparison.InvariantCultureIgnoreCase))
                        newObj[string.IsNullOrWhiteSpace(include.To) ? include.Name : include.To] = logEvent.StackTrace.ToString();                   
                }
            }
            return newObj;
        }

        private IDictionary<object, object> Filter(IDictionary<object,object> newObj, LogEventInfo logEvent)
        {
            var eventInfoProps = _flatExcludes.Where(x => x.Context == ContextType.EventInfo).Select(x => x.Name).ToList();
        
            if (!eventInfoProps.Any(name => name.Equals("loggerName", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("loggerName", logEvent.LoggerName);

            if (!eventInfoProps.Any(name => name.Equals("level", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("level", logEvent.Level);

            if (!eventInfoProps.Any(name => name.Equals("message", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("message", logEvent.Message);

            if (!eventInfoProps.Any(name => name.Equals("parameters", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("parameters", logEvent.Parameters);

            if (!eventInfoProps.Any(name => name.Equals("properties", StringComparison.InvariantCultureIgnoreCase)))
            {
                var filteredProps = Filter(logEvent.Properties, _flatExcludes.Where(x => x.Context == ContextType.Properties).Select(x => x.Name).ToList());
                newObj.Add("properties", filteredProps);
            }

            if (logEvent.Exception != null && !eventInfoProps.Any(name => name.Equals("exception", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("exception", logEvent.Exception);

            if (!eventInfoProps.Any(name => name.Equals("timeStamp", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("timeStamp", logEvent.TimeStamp);

            if (logEvent.StackTrace != null && !eventInfoProps.Any(name => name.Equals("stackTrace", StringComparison.InvariantCultureIgnoreCase)))
                newObj.Add("stackTrace", logEvent.StackTrace.ToString());



            return newObj;
        }

  

        protected override void Dispose(bool disposing)
        {            
            this.Flush(new AsyncContinuation((ex) =>
            {
                InternalLogger.Error("Error while flushing Couchbase objects. Cause : {0}", ex);
            }));

            this._flushBackgroundWorker.Dispose();
 	        base.Dispose(disposing);
        }
    }
}
