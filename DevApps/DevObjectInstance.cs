using Microsoft.Scripting.Hosting;
using System.IO;
using System.Text;

internal partial class Program
{
    /// <summary>
    /// Instance d'objet
    /// Un objet d'instance possède ses propres données et codes
    /// </summary>
    public class DevObjectInstance : DevObject
    {
        /// <summary>
        /// Pointeurs vers des objets existants
        /// </summary>
        internal Dictionary<string, string> pointers = new Dictionary<string, string>(); // name, refName
        public override Dictionary<string, string> Pointers { get { return pointers; } }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        internal Dictionary<string, (string, CompiledCode?)> functions = new Dictionary<string, (string, CompiledCode?)>(); // name, (code, compiledCode)
        public override Dictionary<string, (string, CompiledCode?)> Functions { get { return functions; } }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        internal Dictionary<string, (string, CompiledCode?)> properties = new Dictionary<string, (string, CompiledCode?)>(); // name, (code, compiledCode)
        public override Dictionary<string, (string, CompiledCode?)> Properties { get { return properties; } }
        /// <summary>
        /// Commandes utilisateur
        /// </summary>
        internal (string, CompiledCode?) userAction = (String.Empty, null);
        public override (string, CompiledCode?) UserAction { get { return userAction; } }
        /// <summary>
        /// Méthode de simulation (timer)
        /// </summary>
        internal (string, CompiledCode?) loopMethod = (String.Empty, null);
        public override (string, CompiledCode?) LoopMethod { get { return loopMethod; } }
        /// <summary>
        /// Méthode de simulation (initialisation)
        /// </summary>
        internal (string, CompiledCode?) initMethod = (String.Empty, null);
        public override (string, CompiledCode?) InitMethod { get { return initMethod; } }
        /// <summary>
        /// Méthode de construction (generation code, ...)
        /// </summary>
        internal (string, CompiledCode?) buildMethod = (String.Empty, null);
        public override (string, CompiledCode?) BuildMethod { get { return buildMethod; } }
        /// <summary>
        /// Code/Données de l'objet
        /// </summary>
        internal (string, CompiledCode?) objectCode = (String.Empty, null);
        public override (string, CompiledCode?) ObjectCode { get { return objectCode; } }
        /// <summary>
        /// Dessin de l'objet
        /// </summary>
        internal (string, CompiledCode?) drawCode = (String.Empty, null);
        public override (string, CompiledCode?) DrawCode { get { return drawCode; } }


        public string Output
        {
            get
            {
                return Encoding.UTF8.GetString(buildStream.GetBuffer());
            }
        }

        public override string? GetDrawCode()
        {
            return DrawCode.Item1;
        }

        public override DevObject SetDrawCode(string? code)
        {
            drawCode = (RemoveIdent(code), null);
            return this;
        }

        public override DevObject SetOutput(byte[] data)
        {
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObject SetOutput(string text, bool removeIdent = false)
        {
            var data = Encoding.UTF8.GetBytes(removeIdent ? RemoveIdent(text) : text);
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObject LoadOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            var data = File.ReadAllBytes(Path.Combine(path, name));
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObject SaveOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            File.WriteAllBytes(Path.Combine(path, name), buildStream.GetBuffer());
            return this;
        }

        public override string? GetCode()
        {
            return ObjectCode.Item1;
        }

        public override DevObject SetCode(string? code)
        {
            objectCode = (RemoveIdent(code), null);
            return this;
        }

        public override string? GetLoopMethod()
        {
            return LoopMethod.Item1;
        }

        public override DevObject SetLoopMethod(string? code)
        {
            loopMethod = (RemoveIdent(code), null);
            return this;
        }

        public override string? GetInitMethod()
        {
            return InitMethod.Item1;
        }

        public override DevObject SetInitMethod(string? code)
        {
            initMethod = (RemoveIdent(code), null);
            return this;
        }

        public override string? GetBuildMethod()
        {
            return BuildMethod.Item1;
        }

        public override DevObject SetBuildMethod(string? code)
        {
            buildMethod = (RemoveIdent(code), null);
            return this;
        }

        public override IEnumerable<KeyValuePair<string, string?>> GetProperties()
        {
            return Properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1));
        }

        public override void SetProperties(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Properties.Clear();
            foreach (var p in items)
            {
                AddProperty(p.Key, p.Value);
            }
        }

        public override string? GetProperty(string name)
        {
            return Properties.ContainsKey(name) ? Properties[name].Item1 : String.Empty;
        }

        public override DevObject AddProperty(string name, string? code)
        {
            Properties[name] = (code != null ? code.Trim() : String.Empty, null);
            return this;
        }

        public override IEnumerable<KeyValuePair<string, string?>> GetFunctions()
        {
            return Functions.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1));
        }

        public override void SetFunctions(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Functions.Clear();
            foreach (var p in items)
            {
                AddFunction(p.Key, p.Value);
            }
        }

        public override string? GetFunction(string name)
        {
            return Functions.ContainsKey(name) ? Functions[name].Item1 : String.Empty;
        }

        public override DevObject AddFunction(string name, string code)
        {
            Functions[name] = (RemoveIdent(code), null);
            return this;
        }

        public override string GetUserAction()
        {
            return UserAction.Item1;
        }

        public override DevObject SetUserAction(string code)
        {
            userAction = (RemoveIdent(code), null);
            return this;
        }

        public override IEnumerable<KeyValuePair<string, string?>> GetPointers()
        {
            return Pointers.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value));
        }

        public override void SetPointers(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Pointers.Clear();
            foreach (var p in items)
            {
                AddPointer(p.Key, p.Value);
            }
        }

        public override string? GetPointer(string name)
        {
            return Pointers.ContainsKey(name) ? Pointers[name] : String.Empty;
        }

        public override DevObject AddPointer(string name, string reference)
        {
            Pointers[name] = reference;
            return this;
        }
    }
}