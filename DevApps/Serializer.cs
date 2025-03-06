using Microsoft.Scripting.Hosting;
using static IronPython.Modules._ast;
using static IronPython.Modules.PythonCsvModule;

namespace Serializer
{
    internal class DevObject
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObject content;

        public DevObject()
        {
            this.content = new Program.DevObject();
        }
        public DevObject(Program.DevObject content)
        {
            this.content = content;
        }

        /// <summary>
        /// Description de l'objet (optionnel)
        /// </summary>
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public KeyValuePair<string, string?>[] Pointers { get { return content.GetPointers().ToArray(); } set { content.SetPointers(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.GetFunctions().ToArray(); } set { content.SetFunctions(value); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.GetProperties().ToArray(); } set { content.SetProperties(value); } }
        public string? UserAction { get { return content.GetUserAction(); } set { content.SetUserAction(value); } }
        public string? LoopMethod { get { return content.GetLoopMethod(); } set { content.SetLoopMethod(value); } }
        public string? InitMethod { get { return content.GetInitMethod(); } set { content.SetInitMethod(value); } }
        public string? BuildMethod { get { return content.GetBuildMethod(); } set { content.SetBuildMethod(value); } }
        public string? ObjectCode { get { return content.GetCode(); } set { content.SetCode(value); } }
        public string? DrawCode { get { return content.GetDrawCode(); } set { content.SetDrawCode(value); } }
        public System.Windows.Rect Zone { get { return content.GetZone(); } set { content.SetZone(value); } }
    }

    internal class DevProject
    {
        public DevProject()
        {
        }
        public KeyValuePair<string, DevObject>[] Objects { 
            get {
                return Program.DevObject.References.Select(p=>new KeyValuePair<string, DevObject>(p.Key, new DevObject(p.Value))).ToArray();
            }
            set
            {
                Program.DevObject.References.Clear();

                foreach (var o in value)
                {
                    Program.DevObject.References.Add(o.Key, o.Value.content);
                }
            }
        }
    }
}
