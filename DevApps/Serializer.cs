using DevApps.Scripts;
using Newtonsoft.Json;

namespace Serializer
{
    internal interface ISerialisable
    {
        [Newtonsoft.Json.JsonIgnore]
        public object Content { get; set; }
    }

    internal class DevVariable : ISerialisable
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
        public object Content
        {
            get => content;
            set => content = (Program.DevVariable)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static implicit operator DevVariable(Program.DevVariable content)
        {
            return new DevVariable(content);
        }

        /// <summary>
        /// Description de l'objet (optionnel)
        /// </summary>
        public string Description { get { return content.Description; } set { content.Description = value; } }
        /// <summary>
        /// Valeur de l'objet
        /// </summary>
        public string Value { get { return content.Value.ToString(); } set { content.Value = Program.DevVariable.Variant.Parse(value); } }
    }

    internal class DevObject : ISerialisable
    {
        public static implicit operator DevObject(Program.DevObject content)
        {
            if (content.IsReference)
                return new DevObjectReference((Program.DevObjectReference)content);
            if (content.IsFile)
                return new DevObjectFile((Program.DevObjectFile)content);
            return new DevObjectInstance((Program.DevObjectInstance)content);
        }
        public virtual object Content { get; set; }
    }

    internal class DevObjectInstance : DevObject, ISerialisable
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
        public override object Content
        {
            get => content;
            set => content = (Program.DevObjectInstance)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public Guid? BaseGuid { get { return content.baseGuid; } set { content.baseGuid = value; } }
        public Guid? Guid { get { return content.guid; } set { content.guid = value; } }
        public string[] Tags { get { return content.tags.ToArray(); } set { content.tags = new HashSet<string>(value); } }
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String InitialDataBase64 { get { return content.InitialDataBase64; } set { content.InitialDataBase64 = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, Program.DevObjectInstance.Pointer>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, Program.DevObject.Pointer>(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.functions.Select(p=>new KeyValuePair<string,string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.functions = new Dictionary<string, (string, CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty,null)))); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.properties = new Dictionary<string, (string, CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public string UserAction { get { return content.userAction.Item1; } set { content.userAction = (value,null); } }
        public string LoopMethod { get { return content.loopMethod.Item1; } set { content.loopMethod = (value, null); ; } }
        public string InitMethod { get { return content.initMethod.Item1; } set { content.initMethod = (value, null); ; } }
        public string BuildMethod { get { return content.buildMethod.Item1; } set { content.buildMethod = (value, null); } }
        public string ObjectCode { get { return content.objectCode.Item1; } set { content.objectCode = (value, null); } }
        public string DrawCode { get { return content.drawCode.Item1; } set { content.drawCode = (value, null); } }
    }

    internal class DevObjectReference : DevObject, ISerialisable
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
        public override object Content
        {
            get => content;
            set => content = (Program.DevObjectReference)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String InitialDataBase64 { get { return content.InitialDataBase64; } set { content.InitialDataBase64 = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public String? BaseObjectName { get { return content.BaseObjectName; } set { content.BaseObjectName = value; } }
        public KeyValuePair<string, string>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, string>(value); } }
    }

    internal class DevObjectFile : DevObject, ISerialisable
    {
        [Newtonsoft.Json.JsonIgnore]
        internal Program.DevObjectFile content;

        public override object Content
        {
            get => content;
            set => content = (Program.DevObjectFile)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public DevObjectFile()
        {
            this.content = new Program.DevObjectFile();
        }
        public DevObjectFile(Program.DevObjectFile content)
        {
            this.content = content;
        }

        public static implicit operator DevObjectFile(Program.DevObjectFile content)
        {
            return new DevObjectFile(content);
        }

        public string[] Tags { get { return content.tags.ToArray(); } set { content.tags = new HashSet<string>(value); } }
        public String? Filename { get { return content.filename; } set { content.filename = value; } }
        public String Description { get { return content.Description; } set { content.Description = value; } }
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, Program.DevObjectFile.Pointer>[] Pointers { get { return content.pointers.ToArray(); } set { content.pointers = new Dictionary<string, Program.DevObject.Pointer>(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.functions.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.functions = new Dictionary<string, (string, CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1)).ToArray(); } set { content.properties = new Dictionary<string, (string, CompiledCode?)>(value.Select(p => new KeyValuePair<string, (string, CompiledCode?)>(p.Key, (p.Value ?? string.Empty, null)))); } }
        public string UserAction { get { return content.userAction.Item1; } set { content.userAction = (value, null); } }
        public string LoopMethod { get { return content.loopMethod.Item1; } set { content.loopMethod = (value, null); ; } }
        public string InitMethod { get { return content.initMethod.Item1; } set { content.initMethod = (value, null); ; } }
        public string BuildMethod { get { return content.buildMethod.Item1; } set { content.buildMethod = (value, null); } }
        public string ObjectCode { get { return content.objectCode.Item1; } set { content.objectCode = (value, null); } }
        public string DrawCode { get { return content.drawCode.Item1; } set { content.drawCode = (value, null); } }
    }

    internal class DevCommands : ISerialisable
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

        public object Content
        {
            get => content;
            set => content = (Program.DevCommandGroup)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static implicit operator DevCommands(Program.DevCommandGroup content)
        {
            return new DevCommands(content);
        }

        public String Label { get { return content.Label; } set { content.Label = value; } }
        public String Output { get { return content.Output; } set { content.Output = value; } }
        public String Commands { get { return content.Content; } set { content = Program.DevCommandGroup.FromString(content.Label, content.Output, value); } }
    }

    internal class DevFacet : ISerialisable
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

        public object Content
        {
            get => content;
            set => content = (Program.DevFacet)value;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static implicit operator DevFacet(Program.DevFacet content)
        {
            return new DevFacet(content);
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
        internal Dictionary<string, Program.DevObject> ReferencesO = new Dictionary<string, Program.DevObject>();
        internal Dictionary<string, Program.DevFacet> ReferencesF = new Dictionary<string, Program.DevFacet>();
        internal Dictionary<string, Program.DevVariable> ReferencesV = new Dictionary<string, Program.DevVariable>();
        internal Dictionary<string, string> ReferencesFiles = new Dictionary<string, string>();

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
