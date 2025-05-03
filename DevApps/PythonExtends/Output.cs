using ComponentAce.Compression.Libs.ZLib;
using IronPython.Runtime;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DevApps.PythonExtends
{
    public class Output
    {
        MemoryStream stream;
        string? cachedText;
        internal string Filename;

        internal void Flush()
        {
            using var file = File.Open(Filename, FileMode.OpenOrCreate);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(file);
        }

        internal void Reload()
        {
            using var file = File.Open(Filename, FileMode.Open);
            stream.Seek(0, SeekOrigin.Begin);
            file.CopyTo(stream);
            stream.SetLength(file.Length);
        }

        public Output(MemoryStream stream, string filename)
        {
            this.stream = stream;
            Filename = filename;
        }
        public void write(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            cachedText = text;
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(bytes);
            stream.SetLength(bytes.Length);
        }
        public void append(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var length = stream.Length;
            stream.Seek(0, SeekOrigin.End);
            stream.Write(bytes);
            cachedText = null;
            stream.SetLength(length + bytes.Length);
        }
        public void write_bytes(IronPython.Runtime.Bytes _bytes)
        {
           var bytes = _bytes.ToArray();
           cachedText = Encoding.UTF8.GetString(bytes);
           stream.Seek(0, SeekOrigin.Begin);
           stream.Write(bytes);
           stream.SetLength(bytes.Length);
        }
        public IronPython.Runtime.Bytes bytes()
        {
            return new IronPython.Runtime.Bytes(stream.ToArray());
        }
        public byte[] Bytes()
        {
            return stream.ToArray();
        }
        public long size()
        {
            return stream.Length;
        }
        public double number()
        {
            double val = 0;
            if (cachedText == null)
                cachedText = Encoding.UTF8.GetString(stream.ToArray());
            double.TryParse(cachedText, out val);
            return val;
        }
        public string text()
        {
            if (cachedText != null)
                return cachedText;
            cachedText = Encoding.UTF8.GetString(stream.ToArray());
            //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
            return cachedText;
        }
        public string[] lines()
        {
            if (cachedText != null)
                return Regex.Split(cachedText, "\r\n|\r|\n");
            cachedText = Encoding.UTF8.GetString(stream.ToArray());
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
