using Microsoft.Scripting.Hosting;
using static IronPython.Modules._ast;
using static Program;
using static Program.DevFacet;

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
        public string Description { get { return content.Description; } set { content.Description = value; } }
        /// <summary>
        /// Valeur de l'objet
        /// </summary>
        public object Value { get { return content.Value; } set { content.Value = value; } }
    }
    internal class DevObjectInstance
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectInstance content;

        public DevObjectInstance()
        {
            this.content = new Program.DevObjectInstance();
        }
        public DevObjectInstance(Program.DevObjectInstance content)
        {
            this.content = content;
        }

        public Guid? BaseGuid { get { return content.baseGuid; } set { content.baseGuid = value; } }
        public Guid? Guid { get { return content.guid; } set { content.guid = value; } }
        public HashSet<string> Tags { get { return content.tags; } set { content.tags = value; } }
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String InitialDataBase64 { get { return content.InitialDataBase64; } set { content.InitialDataBase64 = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, Program.DevObjectInstance.Pointer>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, DevObject.Pointer>(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.functions.Select(p=>new KeyValuePair<string,string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.functions = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty,null)))); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.properties = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public string UserAction { get { return content.userAction.Item1; } set { content.userAction = (value,null); } }
        public string LoopMethod { get { return content.loopMethod.Item1; } set { content.loopMethod = (value, null); ; } }
        public string InitMethod { get { return content.initMethod.Item1; } set { content.initMethod = (value, null); ; } }
        public string BuildMethod { get { return content.buildMethod.Item1; } set { content.buildMethod = (value, null); } }
        public string ObjectCode { get { return content.objectCode.Item1; } set { content.objectCode = (value, null); } }
        public string DrawCode { get { return content.drawCode.Item1; } set { content.drawCode = (value, null); } }
    }

    internal class DevObjectReference
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectReference content;

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
        public KeyValuePair<string, string>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, string>(value); } }
    }

    internal class DevObjectFile
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectFile content;

        public DevObjectFile()
        {
            this.content = new Program.DevObjectFile();
        }
        public DevObjectFile(Program.DevObjectFile content)
        {
            this.content = content;
        }

        public HashSet<string> Tags { get { return content.tags; } set { content.tags = value; } }
        public String? Filename { get { return content.filename; } set { content.filename = value; } }
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, Program.DevObjectFile.Pointer>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, DevObject.Pointer>(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.functions.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.functions = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.properties = new Dictionary<string, (string, Microsoft.Scripting.Hosting.CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public string UserAction { get { return content.userAction.Item1; } set { content.userAction = (value, null); } }
        public string LoopMethod { get { return content.loopMethod.Item1; } set { content.loopMethod = (value, null); ; } }
        public string InitMethod { get { return content.initMethod.Item1; } set { content.initMethod = (value, null); ; } }
        public string BuildMethod { get { return content.buildMethod.Item1; } set { content.buildMethod = (value, null); } }
        public string ObjectCode { get { return content.objectCode.Item1; } set { content.objectCode = (value, null); } }
        public string DrawCode { get { return content.drawCode.Item1; } set { content.drawCode = (value, null); } }
    }

    internal class DevCommands
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevCommandGroup content;

        public DevCommands()
        {
            this.content = new Program.DevCommandGroup();
        }
        public DevCommands(Program.DevCommandGroup content)
        {
            this.content = content;
        }
        public String Label { get { return content.Label; } set { content.Label = value; } }
        public String Output { get { return content.Output; } set { content.Output = value; } }
        public String Commands { get { return content.Content; } set { content = Program.DevCommandGroup.FromString(content.Label, content.Output, value); } }
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

        public System.Windows.Rect Description { get { return content.PrintLayout; } set { content.PrintLayout = value; } }
        public KeyValuePair<string, Program.DevFacet.ObjectProperties?>[] Objects { get { return content.GetObjects().ToArray(); } set { content.SetObjects(value); } }
        public KeyValuePair<string, Program.DevFacet.CommandProperties?>[] Commands { get { return content.GetCommands().ToArray(); } set { content.SetCommands(value); } }
        public Program.DevFacet.Geometry[] Geometries { get { return content.GetGeometries().ToArray(); } set { content.SetGeometries(value); } }
        public Program.DevFacet.Text[] Texts { get { return content.GetTexts().ToArray(); } set { content.SetTexts(value); } }
    }

    internal class DevProject
    {
        public DevProject()
        {
        }
        public KeyValuePair<string, string>[] Files
        {
            get
            {
                return Program.DevFile.References.Select(p => new KeyValuePair<string, string>(p.Key, p.Value.ObjectName)).ToArray();
            }
            set
            {
                Program.DevFile.References.Clear();
                foreach (var o in value)
                {
                    Program.DevFile.References.Add(o.Key, new Program.DevFile(o.Key, o.Value));
                }
            }
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
        public KeyValuePair<string, DevObjectFile>[] ObjectsFiles
        { 
            get {
                return Program.DevObjectFile.References.Where(p => p.Value is Program.DevObjectFile).Select(p=>new KeyValuePair<string, DevObjectFile>(p.Key, new DevObjectFile((Program.DevObjectFile)p.Value))).ToArray();
            }
            set
            {
                foreach (var s in Program.DevObjectFile.References.Where(p => p.Value is Program.DevObjectFile).ToArray())
                {
                    Program.DevObjectFile.References.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    Program.DevObjectFile.References.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectInstance>[] Objects { 
            get {
                return Program.DevObject.References.Where(p => p.Value is Program.DevObjectInstance).Select(p=>new KeyValuePair<string, DevObjectInstance>(p.Key, new DevObjectInstance((Program.DevObjectInstance)p.Value))).ToArray();
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
                return Program.DevObject.References.Where(p=>p.Value is Program.DevObjectReference).Select(p => new KeyValuePair<string, DevObjectReference>(p.Key, new DevObjectReference((Program.DevObjectReference)p.Value))).ToArray();
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
        public KeyValuePair<string, DevCommands>[] Commands
        {
            get
            {
                return Program.DevCommandGroup.References.Select(p => new KeyValuePair<string, DevCommands>(p.Key, new DevCommands(p.Value))).ToArray();
            }
            set
            {
                Program.DevCommandGroup.References.Clear();
                foreach (var o in value)
                {
                    Program.DevCommandGroup.References.Add(o.Key, o.Value.content);
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
        public Dictionary<string, string> ReferencesFiles = new Dictionary<string, string>();

        public DevExternalProject()
        {
        }
        public KeyValuePair<string, string>[] Files
        {
            get
            {
                return ReferencesFiles.Select(p => new KeyValuePair<string, string>(p.Key, p.Value)).ToArray();
            }
            set
            {
                ReferencesFiles.Clear();
                foreach (var o in value)
                {
                    ReferencesFiles.Add(o.Key, o.Value);
                }
            }
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
        public KeyValuePair<string, DevObjectFile>[] ObjectsFiles
        {
            get
            {
                return ReferencesO.Where(p => p.Value is Program.DevObjectFile).Select(p => new KeyValuePair<string, DevObjectFile>(p.Key, new DevObjectFile((Program.DevObjectFile)p.Value))).ToArray();
            }
            set
            {
                foreach (var s in ReferencesO.Where(p => p.Value is Program.DevObjectFile).ToArray())
                {
                    ReferencesO.Remove(s.Key);
                }

                foreach (var o in value)
                {
                    ReferencesO.Add(o.Key, o.Value.content);
                }
            }
        }
        public KeyValuePair<string, DevObjectInstance>[] Objects
        {
            get
            {
                return ReferencesO.Where(p => p.Value is Program.DevObjectInstance).Select(p => new KeyValuePair<string, DevObjectInstance>(p.Key, new DevObjectInstance((Program.DevObjectInstance)p.Value))).ToArray();
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
                return ReferencesO.Where(p => p.Value is Program.DevObjectReference).Where(p => p.Value is Program.DevObjectReference).Select(p => new KeyValuePair<string, DevObjectReference>(p.Key, new DevObjectReference((Program.DevObjectReference)p.Value))).ToArray();
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
