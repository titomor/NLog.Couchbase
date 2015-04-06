using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLog.Couchbase.Test
{
    public class CouchbaseTargetMock : CouchbaseTarget
    {
        public void PublicInitializeTarget()
        {
            base.InitializeTarget();
        }



    }
}
