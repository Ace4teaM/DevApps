using IronPython.Compiler.Ast;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Diagnostics;
using System.IO;
using System.Text;
using static IronPython.Modules._ast;
using static IronPython.Modules.PythonCsvModule;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

internal partial class Program
{
    internal class DevSelect
    {
        internal List<DevObject> devObjects = new List<DevObject>();

        internal static readonly DevSelect Empty = new DevSelect();

        public DevSelect SetOutput(string text)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetOutput(text);
            }
            return this;
        }

        public DevSelect SetCode(string? code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetCode(code);
            }
            return this;
        }

        public DevSelect SetLoopMethod(string? code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetLoopMethod(code);
            }
            return this;
        }

        public DevSelect SetInitMethod(string? code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetInitMethod(code);
            }
            return this;
        }

        public DevSelect SetBuildMethod(string? code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetBuildMethod(code);
            }
            return this;
        }

        public void SetProperties(IEnumerable<KeyValuePair<string, string?>> items)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetProperties(items);
            }
        }

        public DevSelect AddProperty(string name, string? code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.AddProperty(name, code);
            }
            return this;
        }

        public void SetFunctions(IEnumerable<KeyValuePair<string, string?>> items)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetFunctions(items);
            }
        }

        public DevSelect AddFunction(string name, string code)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.AddFunction(name, code);
            }
            return this;
        }

        public void SetPointers(IEnumerable<KeyValuePair<string, string?>> items)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.SetPointers(items);
            }
        }

        public DevSelect AddPointer(string name, string reference)
        {
            foreach (DevObject devObject in devObjects)
            {
                devObject.AddPointer(name, reference);
            }
            return this;
        }
    }
}
