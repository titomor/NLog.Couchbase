using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLog.Couchbase
{
    public class KeyAlreadyExistsException : Exception
    {
        public KeyAlreadyExistsException(string message) : base(message)
        {

        }
    }
}
