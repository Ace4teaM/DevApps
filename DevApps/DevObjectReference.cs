using Microsoft.Scripting.Hosting;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using static Program;

internal partial class Program
{
    /// <summary>
    /// Référence d'objet
    /// Un objet de référence utilise le même code mais possède ses propres pointeurs d'objets
    /// </summary>
    public class DevObjectReference : DevObject
    {
        internal string? baseObjectName;

        internal DevObjectInstance? baseObject; //mettre en cache GetBaseObject

        /// </summary>
        public String? BaseObjectName
        {
            get
            {
                return baseObjectName;
            }
            set
            {
                baseObjectName = value;
                baseObject = GetBaseObject();
            }
        }
        public DevObjectReference()
        {
        }

        public DevObjectReference(string baseObjectName)
        {
            BaseObjectName = baseObjectName;
            Description = baseObject?.Description ?? String.Empty;
            pointers = new Dictionary<string, string>( baseObject?.Pointers );
        }

        internal DevObjectInstance? GetBaseObject()
        {
            DevObject.mutexCheckObjectList.WaitOne();
            var obj = DevObject.References.FirstOrDefault(p => p.Key == baseObjectName).Value as DevObjectInstance;
            DevObject.mutexCheckObjectList.ReleaseMutex();
            return obj;
        }

        /// <summary>
        /// Pointeurs vers des objets existants
        /// </summary>
        protected Dictionary<string, string> pointers = new Dictionary<string, string>(); // name, refName
        public override Dictionary<string, string> Pointers { get { return pointers; } }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public override Dictionary<string, (string, CompiledCode?)> Functions { get { return baseObject.functions; } }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public override Dictionary<string, (string, CompiledCode?)> Properties { get { return baseObject.properties; } }
        /// <summary>
        /// Commandes utilisateur
        /// </summary>
        public override (string, CompiledCode?) UserAction { get { return baseObject.userAction; } }
        /// <summary>
        /// Méthode de simulation (timer)
        /// </summary>
        public override (string, CompiledCode?) LoopMethod { get { return baseObject.loopMethod; } }
        /// <summary>
        /// Méthode de simulation (initialisation)
        /// </summary>
        public override (string, CompiledCode?) InitMethod { get { return baseObject.initMethod; } }
        /// <summary>
        /// Méthode de construction (generation code, ...)
        /// </summary>
        public override (string, CompiledCode?) BuildMethod { get { return baseObject.buildMethod; } }
        /// <summary>
        /// Code/Données de l'objet
        /// </summary>
        public override (string, CompiledCode?) ObjectCode { get { return baseObject.objectCode; } }
        /// <summary>
        /// Dessin de l'objet
        /// </summary>
        public override (string, CompiledCode?) DrawCode { get { return baseObject.drawCode; } }



        public override string? GetDrawCode()
        {
            return baseObject?.GetDrawCode();
        }

        public override DevObjectReference SetDrawCode(string? code)
        {
            baseObject?.SetDrawCode(code);
            return this;
        }

        public override DevObjectReference SetOutput(byte[] data)
        {
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObjectReference SetOutput(string text, bool removeIdent = false)
        {
            var data = Encoding.UTF8.GetBytes(removeIdent ? RemoveIdent(text) : text);
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObjectReference LoadOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            var data = File.ReadAllBytes(Path.Combine(path, name));
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public override DevObjectReference SaveOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            File.WriteAllBytes(Path.Combine(path, name), buildStream.GetBuffer());
            return this;
        }

        public override string? GetCode()
        {
            return baseObject?.GetCode();
        }

        public override DevObjectReference SetCode(string? code)
        {
            baseObject?.SetCode(code);
            return this;
        }

        public override string? GetLoopMethod()
        {
            return baseObject?.GetLoopMethod();
        }

        public override DevObjectReference SetLoopMethod(string? code)
        {
            baseObject?.SetLoopMethod(code);
            return this;
        }

        public override string? GetInitMethod()
        {
            return baseObject?.GetInitMethod();
        }

        public override DevObjectReference SetInitMethod(string? code)
        {
            baseObject?.SetInitMethod(code);
            return this;
        }

        public override string? GetBuildMethod()
        {
            return baseObject?.GetBuildMethod();
        }

        public override DevObjectReference SetBuildMethod(string? code)
        {
            baseObject?.SetBuildMethod(code);
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

        public override DevObjectReference AddProperty(string name, string? code)
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

        public override DevObjectReference AddFunction(string name, string code)
        {
            Functions[name] = (RemoveIdent(code), null);
            return this;
        }

        public override string GetUserAction()
        {
            return baseObject?.GetUserAction();
        }

        public override DevObjectReference SetUserAction(string code)
        {
            baseObject?.SetUserAction(code);
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

        public override DevObjectReference AddPointer(string name, string reference)
        {
            Pointers[name] = reference;
            return this;
        }

        public override void CompilDraw()
        {
            baseObject?.CompilDraw();
        }

        public override void CompilObject()
        {
            baseObject?.CompilObject();
        }

        public override void CompilFunctions()
        {
            baseObject?.CompilFunctions();
        }

        public override void CompilProperties()
        {
            baseObject?.CompilProperties();
        }

        public override void CompilUserAction()
        {
            baseObject?.CompilUserAction();
        }

        public override void CompilLoop()//periodic
        {
            baseObject?.CompilLoop();
        }

        public override void CompilInit()
        {
            baseObject?.CompilInit();
        }

        public override void CompilBuild()
        {
            baseObject?.CompilBuild();
        }
    }
}
