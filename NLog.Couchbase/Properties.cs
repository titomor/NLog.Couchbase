using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace NLog.Couchbase
{
    public class Properties
    {
        public Properties()
        {
            Excludes = new List<Exclude>();
            Includes = new List<Include>();
        }
        [NLog.Config.ArrayParameter(typeof(NLog.Couchbase.Exclude), "exclude")]
        public IList<Exclude> Excludes { get; set; }

        [NLog.Config.ArrayParameter(typeof(NLog.Couchbase.Include), "include")]
        public IList<Include> Includes { get; set; }
    }

    
    public class Exclude
    {
        public ContextType Context { get; set; }
        public string Name { get; set; }
    }
    
    public class Include
    {
        public ContextType Context { get;set; }
        public string Name { get; set; }
        public string To { get; set; }
    }

    public enum ContextType
    {
        EventInfo = 0,
        Properties = 1,       
        MDC = 3,
        GDC = 4,
        Parameters = 5, //NOT IMPLEMENTED
        Layout = 6
    }

}
