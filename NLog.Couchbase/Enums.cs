using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OB.Log.Couchbase
{
    public enum DocumentSource
    {
        None,
        Properties,
        Parameters,
        All
    }

    public enum DocumentFormat
    {
        Default,       
        JSON
    }
}
