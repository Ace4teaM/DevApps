using DevApps;
using DevApps.GUI;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    /// <summary>
    /// Objet de base
    /// </summary>
    public abstract class DevObject
    {
        public static Dictionary<string, DevObject> References = new Dictionary<string, DevObject>();
        internal static Mutex mutexExecuteObjects = new Mutex();
        internal static Mutex mutexCheckObjectList = new Mutex();
        internal static bool run = false;
        internal static Thread? thread;

        internal Mutex mutexReadOutput = new Mutex();
        public MemoryStream buildStream = new MemoryStream();

        public DevApps.PythonExtends.GUI gui = new DevApps.PythonExtends.GUI();

        internal bool IsInitialized = false;

        /// <summary>
        /// Description de l'objet (optionnel)
        /// </summary>
        public String Description = String.Empty;

        /// <summary>
        /// Editeur de l'objet (optionnel)
        /// </summary>
        public String? Editor = null;

        /// <summary>
        /// true si l'objet est de type DevObjectReference
        /// </summary>
        public bool IsReference { get { return this is DevObjectReference; } }

        /// <summary>
        /// Pointeurs vers des objets existants
        /// </summary>
        public abstract Dictionary<string, string> Pointers { get; }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public abstract Dictionary<string, (string, CompiledCode?)> Functions { get; }
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public abstract Dictionary<string, (string, CompiledCode?)> Properties { get; }
        /// <summary>
        /// Commandes utilisateur
        /// </summary>
        public abstract  (string, CompiledCode?) UserAction { get; }
        /// <summary>
        /// Méthode de simulation (timer)
        /// </summary>
        public abstract (string, CompiledCode?) LoopMethod { get; }
        /// <summary>
        /// Méthode de simulation (initialisation)
        /// </summary>
        public abstract (string, CompiledCode?) InitMethod { get; }
        /// <summary>
        /// Méthode de construction (generation code, ...)
        /// </summary>
        public abstract (string, CompiledCode?) BuildMethod { get; }
        /// <summary>
        /// Code/Données de l'objet
        /// </summary>
        public abstract (string, CompiledCode?) ObjectCode { get; }
        /// <summary>
        /// Dessin de l'objet
        /// </summary>
        public abstract (string, CompiledCode?) DrawCode { get; }

        static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString.EnumerateRunes())
            {
                var unicodeCategory = Rune.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// trouve un nom unique
        /// </summary>
        /// <param name="name"></param>
        public static void MakeUniqueName(ref string name)
        {
            var newName = RemoveDiacritics(name);
            int n = 2;

            var allowedChars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

            newName = newName.Replace(' ', '_');
            newName = newName.Replace('\t', '_');
            newName = newName.Replace('-', '_');

            newName = Regex.Replace(newName, "[^" + allowedChars + "]", "");

            while (References.ContainsKey(newName))
            {
                newName = name + n;
                n++;
            }

            name = newName;
        }


        public static DevObjectInstance Create(string name, string desc)
        {
            var o = new DevObjectInstance();
            o.Description = desc;
            References.Add(name, o);

            return o;
        }

        public static DevObjectReference CreateReference(string name, string refname)
        {
            var o = new DevObjectReference(refname);
            References.Add(name, o);

            return o;
        }

        public static DevObject? CreateFromFile(string file, out string name)
        {
            var cp = StringComparison.InvariantCultureIgnoreCase;

            name = Path.GetFileNameWithoutExtension(file);
            DevObject.MakeUniqueName(ref name);
            var obj = DevObject.Create(name, Path.GetFileNameWithoutExtension(file));
            obj.SetOutput(File.ReadAllBytes(file));

            if (file.EndsWith(".svg", cp))
            {
                obj.SetDrawCode(@"gui.svg(out)");
            }
            else if (file.EndsWith(".png", cp) || file.EndsWith(".bmp", cp) || file.EndsWith(".jpg", cp) || file.EndsWith(".jpeg", cp) || file.EndsWith(".gif", cp))
            {
                obj.SetDrawCode(@"gui.image(out)");
            }
            else if (file.EndsWith(".cs", cp) || file.EndsWith(".cpp", cp) || file.EndsWith(".h", cp) || file.EndsWith(".c", cp) || file.EndsWith(".txt", cp) || file.EndsWith(".erd", cp))
            {
                obj.SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");
            }

            return obj;
        }

        public static DevSelect Select(params string[] names)
        {
            return new DevSelect { devObjects = References.Where(p => names.Contains(p.Key)).Select(p=>p.Value).ToList() };
        }

        public static DevSelect SelectAll()
        {
            return new DevSelect { devObjects = References.Select(p => p.Value).ToList() };
        }

        public static DevObject? Get(string name)
        {
            return References.GetValueOrDefault(name);
        }

        /// <summary>
        /// Execute le thread périodique des objets
        /// </summary>
        public static void Start()
        {
            if (run == true)
                return;
            run = true;
            thread = new Thread(Worker);
            thread?.Start();
        }

        /// <summary>
        /// Termine le thread périodique des objets
        /// </summary>
        public static void Stop()
        {
            if (run == false)
                return;
            run = false;
            thread?.Join();
        }

        /// <summary>
        /// Thread périodique des objets
        /// </summary>
        private static void Worker()
        {
            try
            {
                int i = 0;
                while (run)
                {
                    mutexExecuteObjects.WaitOne();
                    System.Console.WriteLine(i++);
                    if (run == true)
                        DevObject.Timer();
                    if (run == true)
                        DevObject.Draw();
                    if (run == true)
                        Thread.Sleep(1000);

                    mutexExecuteObjects.ReleaseMutex();

                    // Attend la fin des opérations de dessin
                    Service.WaitDrawOperations();
                }
            }
            catch (Exception e )
            {
                System.Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Execute le script d'actualisation périodique des objets
        /// </summary>
        private static void Timer()
        {
            mutexCheckObjectList.WaitOne();
            var list = References.ToArray();
            mutexCheckObjectList.ReleaseMutex();

            foreach (var o in list)
            {
                o.Value.mutexReadOutput.WaitOne();
                pyScope.SetVariable("out", o.Value.buildStream.GetBuffer());
                pyScope.RemoveVariable("gui");
                o.Value.LoopMethod.Item2?.Execute(pyScope);
                o.Value.mutexReadOutput.ReleaseMutex();
            }
        }

        /// <summary>
        /// Execute le script de dessin des objets
        /// </summary>
        private static void Draw()
        {
            if (Service.IsInitialized == false)
                return;

            mutexCheckObjectList.WaitOne();
            var list = References.Where(p => p.Value.DrawCode.Item2 != null).ToArray();
            mutexCheckObjectList.ReleaseMutex();

            foreach (var o in list)
            {
                Service.Invalidate(o.Key); // appeler uniquement si le contenu de out a changé
            }
        }

        /// <summary>
        /// Charge la sortie standard des objets
        /// </summary>
        public static void LoadOutput()
        {
            mutexExecuteObjects.WaitOne();
            foreach (var o in References)
            {
                try
                {
                    var path = Path.Combine(DataDir, o.Key);

                    if (File.Exists(path))
                    {
                        using var file = File.Open(path, FileMode.Open);
                        o.Value.buildStream.Seek(0, SeekOrigin.Begin);
                        file.CopyTo(o.Value.buildStream);
                        o.Value.buildStream.SetLength(file.Length);
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("load object data " + o.Key + " failed");
                    System.Console.WriteLine(ex.Message);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
        }

        /// <summary>
        /// Sauvegarde la sortie standard des objets
        /// </summary>
        public static void SaveOutput()
        {
            mutexExecuteObjects.WaitOne();
            foreach (var o in References)
            {
                if (o.Value.buildStream.Length == 0)
                    continue;

                try
                {
                    var path = Path.Combine(DataDir, o.Key);
                    using var file = File.Open(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.Write);
                    o.Value.buildStream.Seek(0, SeekOrigin.Begin);
                    o.Value.buildStream.CopyTo(file);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("save object data " + o.Key + " failed");
                    System.Console.WriteLine(ex.Message);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
        }

        /// <summary>
        /// Execute le script d'initialisation
        /// </summary>
        /// <remarks>Initialise uniquement les objets non initialisé</remarks>
        public static void Init()
        {
            mutexExecuteObjects.WaitOne();
            foreach (var o in References.Where(p=>p.Value.IsInitialized == false))
            {
                try
                {
                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("types", new DevApps.PythonExtends.NetTypes());
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(o.Value.buildStream, Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                    pyScope.SetVariable("name", o.Key);
                    pyScope.SetVariable("desc", o.Value.Description);
                    foreach (var pointer in o.Value.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                    }
                    var result = o.Value.InitMethod.Item2?.Execute(pyScope);

                    o.Value.IsInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(o.Key);
                    ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                    string error = eo.FormatException(ex);
                    Console.WriteLine(error);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
        }

        /// <summary>
        /// Construit la sortie des objets
        /// </summary>
        public static void Build(IEnumerable<KeyValuePair<string,DevObject>>? objects = null)
        {
            mutexExecuteObjects.WaitOne();
            foreach (var o in objects ?? References)
            {
                try
                {
                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("types", new DevApps.PythonExtends.NetTypes());
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(o.Value.buildStream, Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                    pyScope.SetVariable("name", o.Key);
                    pyScope.SetVariable("desc", o.Value.Description);
                    foreach (var pointer in o.Value.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                    }
                    var result = o.Value.BuildMethod.Item2?.Execute(pyScope);

                    Service.Invalidate(o.Key);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(o.Key);
                    ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                    string error = eo.FormatException(ex);
                    Console.WriteLine(error);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
        }

        /// <summary>
        /// Execute une fonction
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="func"></param>
        public static void Function(string obj, string func)
        {
            mutexExecuteObjects.WaitOne();
            if (References.ContainsKey(obj))
            {
                var o = References[obj];
                if (o != null && o.Functions.ContainsKey(func))
                {
                    var f = o.Functions[func];
                    var result = f.Item2?.Execute(pyScope);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
        }

        /// <summary>
        /// Execute une propriété et retourne son résultat
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static dynamic? Property(string obj, string prop)
        {
            dynamic? ret = null;
            mutexExecuteObjects.WaitOne();
            if (References.ContainsKey(obj))
            {
                var o = References[obj];
                if (o != null && o.Properties.ContainsKey(prop))
                {
                    var p = o.Properties[prop];
                    ret = p.Item2?.Execute(pyScope);
                }
            }
            mutexExecuteObjects.ReleaseMutex();
            return ret;
        }

        /// <summary>
        /// Lie les objets externes par leurs noms
        /// </summary>
        public static void MakeReferences(IEnumerable<DevObject>? objects = null)
        {
            foreach (var o in (objects ?? References.Values).OfType<DevObjectInstance>())
            {
                if (String.IsNullOrWhiteSpace(o.DrawCode.Item1) == false)
                {
                    string sourceCode = o.DrawCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                    CompiledCode compiled = source.Compile();
                    o.drawCode = (sourceCode, compiled);
                }

                if (String.IsNullOrWhiteSpace(o.ObjectCode.Item1) == false)
                {
                    string sourceCode = o.ObjectCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                    CompiledCode compiled = source.Compile();
                    o.objectCode = (sourceCode, compiled);
                }

                foreach (var f in o.Functions.ToArray())
                {
                    string functionCode = f.Value.Item1;
                    if (String.IsNullOrWhiteSpace(functionCode) == false)
                    {
                        ScriptSource functionScript = pyEngine.CreateScriptSourceFromString(functionCode, SourceCodeKind.Statements);
                        CompiledCode functionCompiled = functionScript.Compile();
                        o.Functions[f.Key] = (functionCode, functionCompiled);
                    }
                }

                foreach (var f in o.Properties.ToArray())
                {
                    string propertyCode = f.Value.Item1;
                    if (String.IsNullOrWhiteSpace(propertyCode) == false)
                    {
                        ScriptSource propertyScript = pyEngine.CreateScriptSourceFromString(propertyCode, SourceCodeKind.Expression);
                        CompiledCode propertyCompiled = propertyScript.Compile();
                        o.Properties[f.Key] = (propertyCode, propertyCompiled);
                    }
                }

                if (String.IsNullOrWhiteSpace(o.UserAction.Item1) == false)
                {
                    string sourceCode = o.UserAction.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                    CompiledCode compiled = source.Compile();
                    o.userAction = (sourceCode, compiled);
                }

                if (String.IsNullOrWhiteSpace(o.LoopMethod.Item1) == false)
                {
                    string sampleCode = o.LoopMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.loopMethod = (sampleCode, sampleCompiled);
                }

                if (String.IsNullOrWhiteSpace(o.InitMethod.Item1) == false)
                {
                    string sampleCode = o.InitMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.initMethod = (sampleCode, sampleCompiled);
                }

                if (String.IsNullOrWhiteSpace(o.BuildMethod.Item1) == false)
                {
                    string sampleCode = o.BuildMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.buildMethod = (sampleCode, sampleCompiled);
                }
            }
        }

        /// <summary>
        /// Supprime l'indentation de base (espace commun entre les lignes) du code en vue d'être compilé
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        protected static string RemoveIdent(string? code)
        {
            if (code != null)
            {
                List<string> lines = new List<string>();
                int? baseIndent = null;
                using (StringReader reader = new StringReader(code))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (String.IsNullOrWhiteSpace(line))
                            continue;

                        bool alreadyFound = true;
                        int count = line.Count(delegate (char p) { if (p == ' ' && alreadyFound) return true; else alreadyFound = false; return false; });
                        if (baseIndent == null || count < baseIndent)
                            baseIndent = count;

                        lines.Add(line);
                    }
                }
                return baseIndent == null ? String.Join(Environment.NewLine, lines) : String.Join(Environment.NewLine, lines.Select(p => p.Substring(baseIndent.Value)));
            }
            return String.Empty;
        }



        public abstract string? GetDrawCode();
        public abstract DevObject SetDrawCode(string? code);
        public abstract DevObject SetOutput(byte[] data);
        public abstract DevObject SetOutput(string text, bool removeIdent = false);
        public abstract DevObject LoadOutput(string name, string? path = null);
        public abstract DevObject SaveOutput(string name, string? path = null);
        public abstract string? GetCode();
        public abstract DevObject SetCode(string? code);
        public abstract string? GetLoopMethod();
        public abstract DevObject SetLoopMethod(string? code);
        public abstract string? GetInitMethod();
        public abstract DevObject SetInitMethod(string? code);
        public abstract string? GetBuildMethod();
        public abstract DevObject SetBuildMethod(string? code);
        public abstract IEnumerable<KeyValuePair<string, string?>> GetProperties();
        public abstract void SetProperties(IEnumerable<KeyValuePair<string, string?>> items);
        public abstract string? GetProperty(string name);
        public abstract DevObject AddProperty(string name, string? code);
        public abstract IEnumerable<KeyValuePair<string, string?>> GetFunctions();
        public abstract void SetFunctions(IEnumerable<KeyValuePair<string, string?>> items);
        public abstract string? GetFunction(string name);
        public abstract DevObject AddFunction(string name, string code);
        public abstract string GetUserAction();
        public abstract DevObject SetUserAction(string code);
        public abstract IEnumerable<KeyValuePair<string, string?>> GetPointers();
        public abstract void SetPointers(IEnumerable<KeyValuePair<string, string?>> items);
        public abstract string? GetPointer(string name);
        public abstract DevObject AddPointer(string name, string reference);
    }
}