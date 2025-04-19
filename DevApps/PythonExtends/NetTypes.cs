using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevApps.PythonExtends
{
    public class NetTypes
    {
        public byte[] array(IronPython.Runtime.Bytes _bytes)
        {
            return _bytes.ToArray();
        }
    }
}
