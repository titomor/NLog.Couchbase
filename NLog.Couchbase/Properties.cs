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
        public ObjectType Path { get; set; }
        public string Name { get; set; }
    }
    
    public class Include
    {
        public ObjectType PropertyType { get;set; }
        public string Name { get; set; }
        public string MapTo { get; set; }
    }

    public enum ObjectType
    {
        LogEventInfo = 0,
        Properties = 1,
        Parameters = 2 //NOT IMPLEMENTED
    }

}
