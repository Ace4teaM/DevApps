using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DevApps.PythonExtends
{
    public class Output
    {
        MemoryStream stream;
        string? cachedText;
        public Output(MemoryStream stream)
        {
            this.stream = stream;
        }
        public byte[] bytes()
        {
            return stream.GetBuffer();
        }
        public string text()
        {
            if (cachedText != null)
                return cachedText;
            cachedText = Encoding.UTF8.GetString(stream.GetBuffer());
            //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
            return cachedText;
        }
        public string[] lines()
        {
            if (cachedText != null)
                return Regex.Split(cachedText, "\r\n|\r|\n");
            cachedText = Encoding.UTF8.GetString(stream.GetBuffer());
            //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
            return Regex.Split(cachedText, "\r\n|\r|\n");
        }
        internal MemoryStream Stream
        {
            get
            {
                return this.stream;
            }
        }
    }
}
