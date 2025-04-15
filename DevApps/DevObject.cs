using DevApps.GUI;
using IronPython.Runtime.Exceptions;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    public class DevObject
    {
        public static Dictionary<string, DevObject> References = new Dictionary<string, DevObject>();
        internal static Mutex mutexExecuteObjects = new Mutex();
        internal static Mutex mutexCheckObjectList = new Mutex();
        internal Mutex mutexReadOutput = new Mutex();
        internal static bool run = false;
        internal static Thread? thread;
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
        /// Pointeurs vers des objets existants
        /// </summary>
        public Dictionary<string, string> Pointers { get; } = new Dictionary<string, string>(); // name, refName
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public Dictionary<string, (string, CompiledCode?)> Functions { get; } = new Dictionary<string, (string, CompiledCode?)>(); // name, (code, compiledCode)
        /// <summary>
        /// Fonctions internes
        /// </summary>
        public Dictionary<string, (string, CompiledCode?)> Properties { get; } = new Dictionary<string, (string, CompiledCode?)>(); // name, (code, compiledCode)
        /// <summary>
        /// Commandes utilisateur
        /// </summary>
        public (string, CompiledCode?) UserAction { get; set; } = (String.Empty, null);
        /// <summary>
        /// Méthode de simulation (timer)
        /// </summary>
        public (string, CompiledCode?) LoopMethod { get; set; } = (String.Empty, null);
        /// <summary>
        /// Méthode de simulation (initialisation)
        /// </summary>
        public (string, CompiledCode?) InitMethod { get; set; } = (String.Empty, null);
        /// <summary>
        /// Méthode de construction (generation code, ...)
        /// </summary>
        public (string, CompiledCode?) BuildMethod { get; set; } = (String.Empty, null);
        /// <summary>
        /// Code/Données de l'objet
        /// </summary>
        public (string, CompiledCode?) ObjectCode { get; set; } = (String.Empty, null);
        /// <summary>
        /// Dessin de l'objet
        /// </summary>
        public (string, CompiledCode?) DrawCode { get; set; } = (String.Empty, null);

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

            name = newName.ToLower();
        }


        public static DevObject Create(string name, string desc)
        {
            var o = new DevObject();
            o.Description = desc;
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
            foreach (var o in objects ?? References.Values)
            {
                if (String.IsNullOrWhiteSpace(o.DrawCode.Item1) == false)
                {
                    string sourceCode = o.DrawCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                    CompiledCode compiled = source.Compile();
                    o.DrawCode = (sourceCode, compiled);
                }

                if (String.IsNullOrWhiteSpace(o.ObjectCode.Item1) == false)
                {
                    string sourceCode = o.ObjectCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                    CompiledCode compiled = source.Compile();
                    o.ObjectCode = (sourceCode, compiled);
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
                    o.UserAction = (sourceCode, compiled);
                }

                if (String.IsNullOrWhiteSpace(o.LoopMethod.Item1) == false)
                {
                    string sampleCode = o.LoopMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.LoopMethod = (sampleCode, sampleCompiled);
                }

                if (String.IsNullOrWhiteSpace(o.InitMethod.Item1) == false)
                {
                    string sampleCode = o.InitMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.InitMethod = (sampleCode, sampleCompiled);
                }

                if (String.IsNullOrWhiteSpace(o.BuildMethod.Item1) == false)
                {
                    string sampleCode = o.BuildMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                    CompiledCode sampleCompiled = sampleScript.Compile();
                    o.BuildMethod = (sampleCode, sampleCompiled);
                }
            }
        }

        /// <summary>
        /// Supprime l'indentation de base (espace commun entre les lignes) du code en vue d'être compilé
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private static string RemoveIdent(string? code)
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

        public string Output
        {
            get
            {
                return Encoding.UTF8.GetString(buildStream.GetBuffer());
            }
        }

        public string? GetDrawCode()
        {
            return DrawCode.Item1;
        }

        public DevObject SetDrawCode(string? code)
        {
            DrawCode = (RemoveIdent(code), null);
            return this;
        }

        public DevObject SetOutput(byte[] data)
        {
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public DevObject SetOutput(string text, bool removeIdent = false)
        {
            var data = Encoding.UTF8.GetBytes(removeIdent ? RemoveIdent(text) : text);
            buildStream.Seek(0, SeekOrigin.Begin);
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public DevObject LoadOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            var data = File.ReadAllBytes(Path.Combine(path, name));
            buildStream.Write(data);
            buildStream.SetLength(data.Length);
            return this;
        }

        public DevObject SaveOutput(string name, string? path = null)
        {
            if (path == null)
                path = DataDir;

            File.WriteAllBytes(Path.Combine(path, name), buildStream.GetBuffer());
            return this;
        }

        public string? GetCode()
        {
            return ObjectCode.Item1;
        }

        public DevObject SetCode(string? code)
        {
            ObjectCode = (RemoveIdent(code), null);
            return this;
        }

        public string? GetLoopMethod()
        {
            return LoopMethod.Item1;
        }

        public DevObject SetLoopMethod(string? code)
        {
            LoopMethod = (RemoveIdent(code), null);
            return this;
        }

        public string? GetInitMethod()
        {
            return InitMethod.Item1;
        }

        public DevObject SetInitMethod(string? code)
        {
            InitMethod = (RemoveIdent(code), null);
            return this;
        }

        public string? GetBuildMethod()
        {
            return BuildMethod.Item1;
        }

        public DevObject SetBuildMethod(string? code)
        {
            BuildMethod = (RemoveIdent(code), null);
            return this;
        }

        public IEnumerable<KeyValuePair<string, string?>> GetProperties()
        {
            return Properties.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1));
        }

        public void SetProperties(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Properties.Clear();
            foreach (var p in items)
            {
                AddProperty(p.Key, p.Value);
            }
        }

        public string? GetProperty(string name)
        {
            return Properties.ContainsKey(name) ? Properties[name].Item1 : String.Empty;
        }

        public DevObject AddProperty(string name, string? code)
        {
            Properties[name] = (code != null ? code.Trim() : String.Empty, null);
            return this;
        }

        public IEnumerable<KeyValuePair<string, string?>> GetFunctions()
        {
            return Functions.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value.Item1));
        }

        public void SetFunctions(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Functions.Clear();
            foreach (var p in items)
            {
                AddFunction(p.Key, p.Value);
            }
        }

        public string? GetFunction(string name)
        {
            return Functions.ContainsKey(name) ? Functions[name].Item1 : String.Empty;
        }

        public DevObject AddFunction(string name, string code)
        {
            Functions[name] = (RemoveIdent(code), null);
            return this;
        }

        public string GetUserAction()
        {
            return UserAction.Item1;
        }

        public DevObject SetUserAction(string code)
        {
            UserAction = (RemoveIdent(code), null);
            return this;
        }

        public IEnumerable<KeyValuePair<string, string?>> GetPointers()
        {
            return Pointers.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value));
        }

        public void SetPointers(IEnumerable<KeyValuePair<string, string?>> items)
        {
            Pointers.Clear();
            foreach (var p in items)
            {
                AddPointer(p.Key, p.Value);
            }
        }

        public string? GetPointer(string name)
        {
            return Pointers.ContainsKey(name) ? Pointers[name] : String.Empty;
        }

        public DevObject AddPointer(string name, string reference)
        {
            Pointers[name] = reference;
            return this;
        }

    }
}