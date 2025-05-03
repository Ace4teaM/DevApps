using Microsoft.Scripting.Hosting;
using static IronPython.Modules._ast;
using static Program;

namespace Serializer
{
    internal class DevVariable
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevVariable content;

        public DevVariable()
        {
            this.content = new Program.DevVariable();
        }
        public DevVariable(Program.DevVariable content)
        {
            this.content = content;
        }

        /// <summary>
        /// Description de l'objet (optionnel)
        /// </summary>
        public String? Description { get { return content.Description; } set { content.Description = value; } }
        /// <summary>
        /// Valeur de l'objet
        /// </summary>
        public object Value { get { return content.Value; } set { content.Value = value; } }
    }
    internal class DevObjectInstance
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectInstance content;

        [Newtonsoft.Json.JsonIgnore]
        internal string dataPath;

        public DevObjectInstance()
        {
            this.content = new Program.DevObjectInstance();
        }
        public DevObjectInstance(Program.DevObjectInstance content)
        {
            this.content = content;
        }

        public HashSet<string> Tags { get { return content.tags; } set { content.tags = value; } }
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String InitialDataBase64 { get { return content.InitialDataBase64; } set { content.InitialDataBase64 = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, Program.DevObjectInstance.Pointer>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, DevObject.Pointer>(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.functions.Select(p=>new KeyValuePair<string,string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.functions = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value,null)))); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.properties = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value, null)))); } }
        public string? UserAction { get { return content.userAction.Item1; } set { content.userAction = (value,null); } }
        public string? LoopMethod { get { return content.loopMethod.Item1; } set { content.loopMethod = (value, null); ; } }
        public string? InitMethod { get { return content.initMethod.Item1; } set { content.initMethod = (value, null); ; } }
        public string? BuildMethod { get { return content.buildMethod.Item1; } set { content.buildMethod = (value, null); } }
        public string? ObjectCode { get { return content.objectCode.Item1; } set { content.objectCode = (value, null); } }
        public string? DrawCode { get { return content.drawCode.Item1; } set { content.drawCode = (value, null); } }
    }

    internal class DevObjectReference
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectReference content;

        [Newtonsoft.Json.JsonIgnore]
        internal string dataPath;

        public DevObjectReference()
        {
            this.content = new Program.DevObjectReference();
        }
        public DevObjectReference(Program.DevObjectReference content)
        {
            this.content = content;
        }

        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String InitialDataBase64 { get { return content.InitialDataBase64; } set { content.InitialDataBase64 = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public String? BaseObjectName { get { return content.BaseObjectName; } set { content.BaseObjectName = value; } }
        public KeyValuePair<string, string?>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, string>(value); } }
    }

    internal class DevFacet
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevFacet content;

        public DevFacet()
        {
            this.content = new Program.DevFacet();
        }
        public DevFacet(Program.DevFacet content)
        {
            this.content = content;
        }

        public KeyValuePair<string, Program.DevFacet.ObjectProperties?>[] Objects { get { return content.GetObjects().ToArray(); } set { content.SetObjects(value); } }
        public KeyValuePair<string, string>[] Commands { get { return content.GetCommands().ToArray(); } set { content.SetCommands(value); } }
        public Program.DevFacet.Geometry[] Geometries { get { return content.GetGeometries().ToArray(); } set { content.SetGeometries(value); } }
        public Program.DevFacet.Text[] Texts { get { return content.GetTexts().ToArray(); } set { content.SetTexts(value); } }
    }

    internal class DevProject
    {
        public DevProject()
        {
        }
        public KeyValuePair<string, DevVariable>[] Variables
        { 
            get {
                return Program.DevVariable.References.Select(p => new KeyValuePair<string, DevVariable>(p.Key, new DevVariable(p.Value as Program.DevVariable))).ToArray();
            }
            set
            {
                Program.DevVariable.References.Clear();
                foreach (var o in value)
                {
                    Program.DevVariable.References.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectInstance>[] Objects { 
            get {
                return Program.DevObject.References.Where(p => p.Value is Program.DevObjectInstance).Select(p=>new KeyValuePair<string, DevObjectInstance>(p.Key, new DevObjectInstance(p.Value as Program.DevObjectInstance))).ToArray();
            }
            set
            {
                foreach (var s in Program.DevObject.References.Where(p => p.Value is Program.DevObjectInstance).ToArray())
                {
                    Program.DevObject.References.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    Program.DevObject.References.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectReference>[] References
        {
            get
            {
                return Program.DevObject.References.Where(p=>p.Value is Program.DevObjectReference).Select(p => new KeyValuePair<string, DevObjectReference>(p.Key, new DevObjectReference(p.Value as Program.DevObjectReference))).ToArray();
            }
            set
            {
                foreach (var s in Program.DevObject.References.Where(p => p.Value is Program.DevObjectReference).ToArray())
                {
                    Program.DevObject.References.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    Program.DevObject.References.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevFacet>[] Facets
        {
            get
            {
                return Program.DevFacet.References.Select(p => new KeyValuePair<string, DevFacet>(p.Key, new DevFacet(p.Value))).ToArray();
            }
            set
            {
                Program.DevFacet.References.Clear();

                foreach (var o in value)
                {
                    Program.DevFacet.References.Add(o.Key, o.Value.content);
                }
            }
        }
    }
    internal class DevExternalProject
    {
        public Dictionary<string, Program.DevObject> ReferencesO = new Dictionary<string, Program.DevObject>();
        public Dictionary<string, Program.DevFacet> ReferencesF = new Dictionary<string, Program.DevFacet>();
        public Dictionary<string, Program.DevVariable> ReferencesV = new Dictionary<string, Program.DevVariable>();

        public DevExternalProject()
        {
        }
        public KeyValuePair<string, DevVariable>[] Variables
        {
            get
            {
                return ReferencesV.Select(p => new KeyValuePair<string, DevVariable>(p.Key, new DevVariable(p.Value as Program.DevVariable))).ToArray();
            }
            set
            {
                ReferencesV.Clear();
                foreach (var o in value)
                {
                    ReferencesV.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectInstance>[] Objects
        {
            get
            {
                return ReferencesO.Where(p => p.Value is Program.DevObjectInstance).Select(p => new KeyValuePair<string, DevObjectInstance>(p.Key, new DevObjectInstance(p.Value as Program.DevObjectInstance))).ToArray();
            }
            set
            {
                foreach (var s in ReferencesO.Where(p => p.Value is Program.DevObjectInstance).ToArray())
                {
                    ReferencesO.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    ReferencesO.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectReference>[] References
        {
            get
            {
                return ReferencesO.Where(p => p.Value is Program.DevObjectReference).Where(p => p.Value is Program.DevObjectReference).Select(p => new KeyValuePair<string, DevObjectReference>(p.Key, new DevObjectReference(p.Value as Program.DevObjectReference))).ToArray();
            }
            set
            {
                foreach (var s in ReferencesO.Where(p => p.Value is Program.DevObjectReference).ToArray())
                {
                    ReferencesO.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    ReferencesO.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevFacet>[] Facets
        {
            get
            {
                return ReferencesF.Select(p => new KeyValuePair<string, DevFacet>(p.Key, new DevFacet(p.Value))).ToArray();
            }
            set
            {
                ReferencesF.Clear();

                foreach (var o in value)
                {
                    ReferencesF.Add(o.Key, o.Value.content);
                }
            }
        }
    }
}
