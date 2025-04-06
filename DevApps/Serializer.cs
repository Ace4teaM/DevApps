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
        public String? Editor { get { return content.Editor; } set { content.Editor = value; } }
        public KeyValuePair<string, string?>[] Pointers { get { return content.GetPointers().ToArray(); } set { content.SetPointers(value); } }
        public KeyValuePair<string, string?>[] Functions { get { return content.GetFunctions().ToArray(); } set { content.SetFunctions(value); } }
        public KeyValuePair<string, string?>[] Properties { get { return content.GetProperties().ToArray(); } set { content.SetProperties(value); } }
        public string? UserAction { get { return content.GetUserAction(); } set { content.SetUserAction(value); } }
        public string? LoopMethod { get { return content.GetLoopMethod(); } set { content.SetLoopMethod(value); } }
        public string? InitMethod { get { return content.GetInitMethod(); } set { content.SetInitMethod(value); } }
        public string? BuildMethod { get { return content.GetBuildMethod(); } set { content.SetBuildMethod(value); } }
        public string? ObjectCode { get { return content.GetCode(); } set { content.SetCode(value); } }
        public string? DrawCode { get { return content.GetDrawCode(); } set { content.SetDrawCode(value); } }
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

        public DevExternalProject()
        {
        }
        public KeyValuePair<string, DevObject>[] Objects
        {
            get
            {
                return ReferencesO.Select(p => new KeyValuePair<string, DevObject>(p.Key, new DevObject(p.Value))).ToArray();
            }
            set
            {
                ReferencesO.Clear();

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
