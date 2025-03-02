using ComponentAce.Compression.Libs.ZLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

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
        public void write(string text)
        {
            cachedText = text;
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(Encoding.UTF8.GetBytes(text));
        }
        public byte[] bytes()
        {
            return stream.GetBuffer();
        }
        public double number()
        {
            double val = 0;
            if (cachedText == null)
                cachedText = Encoding.UTF8.GetString(stream.GetBuffer());
            double.TryParse(cachedText, out val);
            return val;
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
        public string[] words(string columnsExp)
        {
            List<string> retval = new List<string>();

            var lines = this.lines();

            if (lines.Length == 0)
                return Array.Empty<string>();

            var reg = new Regex(columnsExp, RegexOptions.IgnoreCase);

            var results = lines.Select(p => reg.Match(p)).ToArray();

            var columns = results.Select(p => p.Groups.Values.Count() - 1).Max();

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < results.Length; j++)
                {
                    var text = results[j].Groups[1 + i].Value;
                    retval.Add(text);
                }
            }

            return retval.ToArray();
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
