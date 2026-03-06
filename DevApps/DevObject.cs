using DevApps;
using DevApps.GUI;
using DevApps.Scripts;
using System.IO;
using System.Text.RegularExpressions;

internal partial class Program
{
    /// <summary>
    /// Objet de base
    /// </summary>
    public abstract class DevObject
    {
        /// <summary>
        /// Appelé lorsque l'objet est créé
        /// </summary>
        public virtual void OnInit() { }
        /// <summary>
        /// Appelé lorsque l'objet est détruit
        /// </summary>
        public virtual void OnDelete() { }
        /// <summary>
        /// Appelé lorsque l'objet est construit
        /// </summary>
        public virtual void OnBuilt() { }

        /// <summary>
        /// True si l'objet doit être reconstruit
        /// </summary>
        public bool MustBeBuild { get { return BuildIndex > 0; } }

        /// <summary>
        /// Indice de reconstruction
        /// </summary>
        /// <remarks>
        /// Lorsque la méthode Build d'un objet lié est appelée, cet indice est incrémenté de 1 pour chaque profondeur de pointeur qui lie cette objet à l'objet reconstruit.
        /// </remarks>
        internal int buildIndex = 0;
        public int BuildIndex { get { return buildIndex; } }

        /// <summary>
        /// Incrément les builds des objets liés
        /// </summary>
        internal static void IncrementBuilt(string key)
        {
            if(TryGet(key, out var obj) == false)
                return;

            var tree = CreateDependancesTree(new KeyValuePair<string, DevObject>( key, obj ));
            foreach (var o in tree)
            {
                Program.Logger.WriteLine($"Increment {o.Item2}");
                if (TryGet(o.Item2, out var i))
                {
                    for (int j = 0; j < o.Item1; j++)
                        Interlocked.Increment(ref i.buildIndex);
                }
            }
        }

        public class Pointer
        {
            public string target = string.Empty;
            public HashSet<string> tags = new HashSet<string>();
        }

        /// <summary>
        /// Liste des objets définit comme modèle (IsModel)
        /// </summary>
        public static IEnumerable<KeyValuePair<string, DevObject>> Models { get{ return References.Where(p => p.Value.IsModel()); } }

        /// <summary>
        /// Enregistreur d'états des objets (pour l'historisation, l'annulation, la duplication, ...)
        /// </summary>
        public static DevApps.Record.Recorder<string, Serializer.DevObject, Program.DevObject> Recorder = new();

        /// <summary>
        /// Liste des objets en cours
        /// </summary>
        public static Dictionary<string, DevObject> References = new Dictionary<string, DevObject>();

        /// <summary>
        /// Synchronise l'accès à l'éxecution des objets (Worker())
        /// </summary>
        internal static readonly SemaphoreSlim _executeLock = new(1, 1);

        /// <summary>
        /// Synchronise l'accès à la liste (References)
        /// </summary>
        internal static readonly SemaphoreSlim _checkLock = new(1, 1);

        /// <summary>
        /// True si le thread périodique des objets est en cours d'exécution
        /// </summary>
        internal static bool run = false;

        /// <summary>
        /// Thread d'exécution des périodiques d'objets (voir la méthode Worker())
        /// </summary>
        internal static Thread? thread;

        /// <summary>
        /// Bloque l'accès en écriture pour Output (permet la lecture sans modification)
        /// </summary>
        internal readonly SemaphoreSlim _readOutput = new(1, 1);

        /// <summary>
        /// Accès aux données de l'objet
        /// </summary>
        public abstract Stream Content { get; }

        public DevApps.Scripts.GUI gui = new DevApps.Scripts.GUI();

        internal bool IsInitialized = false;

        /// <summary>
        /// Tags de l'objet
        /// </summary>
        public abstract String[] Tags { get; }

        /// <summary>
        /// Description de l'objet (optionnel)
        /// </summary>
        public String Description = String.Empty;

        /// <summary>
        /// Données persistantes de l'objet (Base64)
        /// </summary>
        public String InitialDataBase64 = String.Empty;

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
        public abstract Dictionary<string, Pointer> Pointers { get; }
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

        /// <summary>
        /// trouve un nom unique
        /// </summary>
        /// <param name="name"></param>
        public static void MakeUniqueName(ref string name, IEnumerable<string>? anotherNames = null)
        {
            var newName = Program.RemoveDiacritics(name);
            int n = 2;

            const string allowedChars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

            const string allowedFirstChars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";

            name = name.Replace(' ', '_');
            name = name.Replace('\t', '_');
            name = name.Replace('-', '_');

            name = Regex.Replace(name, "[^" + allowedChars + "]", "");

            // le nom doit commencer par un caractère valide (FrameworkElement.Name l'exige)
            if (allowedFirstChars.Contains(name[0]) == false)
                name = "_" + name;


            newName = name;

            if (anotherNames != null)
            {
                while (References.ContainsKey(newName) || Program.Keywords.Contains(newName) || anotherNames.Contains(newName))
                {
                    newName = name + n;
                    n++;
                }
            }
            else
            {
                while (References.ContainsKey(newName) || Program.Keywords.Contains(newName))
                {
                    newName = name + n;
                    n++;
                }
            }

            name = newName;
        }

        public static DevObjectInstance Create(string name, string desc, string[] tags)
        {
            var o = new DevObjectInstance();
            o.Description = desc;
            o.tags = new HashSet<string>(tags);
            References.Add(name, o);

            return o;
        }


        public static DevObjectReference CreateReference(string name, string refname)
        {
            var o = new DevObjectReference(refname);
            References.Add(name, o);

            return o;
        }

        public static void DeleteObject(string name)
        {
            var obj = References.GetValueOrDefault(name);
            if (obj != null)
            {
                References.Remove(name);
                foreach (var o in DevFacet.References)
                {
                    if (o.Value.Objects.ContainsKey(name) == false)
                        continue;
                    o.Value.Objects.Remove(name);
                }
                obj.OnDelete();
            }
        }

        public static DevObject? CreateFromFile(string file, out string name)
        {
            var cp = StringComparison.InvariantCultureIgnoreCase;

            name = Path.GetFileNameWithoutExtension(file);
            DevObject.MakeUniqueName(ref name);
            var obj = DevObject.Create(name, Path.GetFileNameWithoutExtension(file), new string[] { });
            obj.SetOutput(File.ReadAllBytes(file));

            if (file.EndsWith(".svg", cp))
            {
                obj.SetDrawCode(@"gui.svg(out)");
                obj.tags.Add("#image");
            }
            else if (file.EndsWith(".png", cp) || file.EndsWith(".bmp", cp) || file.EndsWith(".jpg", cp) || file.EndsWith(".jpeg", cp) || file.EndsWith(".gif", cp))
            {
                obj.SetDrawCode(@"gui.image(out)");
                obj.tags.Add("#image");
            }
            else if (file.EndsWith(".cs", cp) || file.EndsWith(".cpp", cp) || file.EndsWith(".h", cp) || file.EndsWith(".c", cp) || file.EndsWith(".txt", cp) || file.EndsWith(".erd", cp))
            {
                obj.SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");
                obj.tags.Add("#script");
            }

            if(file.LastIndexOf('.') != -1)
            {
                var tag = "#" + file.Substring(file.LastIndexOf('.'));
                if(TagService.TagFormat.IsMatch(tag))
                    obj.tags.Add(tag);
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

        public static bool TryGet(string name, out DevObject obj)
        {
            obj = References.GetValueOrDefault(name) ?? NullObject;
            return obj != NullObject;
        }

        public static readonly DevObject NullObject = new DevObjectInstance();

        public static bool IsRunning
        {
            get
            {
                return thread?.IsAlive == true;
            }
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
                // Signal la fin du thread
                GuiService.SignalWorkerStatusChange();

                int i = 0;
                while (run)
                {
                    try
                    {
                        _executeLock.Wait();

                        Program.Logger.WriteLine(i++);
                        if (run == true)
                            DevObject.Timer();
                        if (run == true)
                            DevObject.Draw();
                        if (run == true)
                            Thread.Sleep(1000);
                    }
                    catch
                    { 
                    }
                    finally
                    {
                        _executeLock.Release();
                    }

                    // Attend la fin des opérations de dessin
                    GuiService.WaitDrawOperations();
                }

                // Signal la fin du thread
                GuiService.SignalWorkerStatusChange();
            }
            catch (Exception e )
            {
                Program.Logger.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Execute le script d'actualisation périodique des objets
        /// </summary>
        private static void Timer()
        {
            KeyValuePair<string, DevObject>[]? list = null;
            try
            {
                DevObject._checkLock.Wait(); // nécessaire sachant que _executeLock est déjà verrouillé ?
                list = References.ToArray();
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            if (list != null)
            {
                foreach (var o in list)
                {
                    try
                    {
                        o.Value._readOutput.Wait();

                        var engine = o.Value.LoopMethod.Item2?.Engine;
                        if (engine != null)
                        {
                            try
                            {
                                var scope = GetGlobalScope(engine);
                                scope.SetVariable("out", new DevApps.Scripts.Output(o.Value.Content, Path.Combine(Program.DataDir, o.Key)));
                                scope.RemoveVariable("gui");
                                o.Value.LoopMethod.Item2?.Execute(scope);
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.WriteLine("******************************************");
                                Program.Logger.WriteLine("Timer: " + o.Key);
                                Program.Logger.WriteLine(engine.FormatError(ex));
                                Program.Logger.WriteLine("******************************************");
                            }
                        }
                    }
                    finally
                    {
                        o.Value._readOutput.Release();
                    }
                }
            }
        }

        /// <summary>
        /// Execute le script de dessin des objets
        /// </summary>
        private static void Draw()
        {
            if (GuiService.IsInitialized == false)
                return;

            KeyValuePair<string, DevObject>[]? list = null;

            try
            {
                DevObject._checkLock.Wait(); // nécessaire sachant que _executeLock est déjà verrouillé ?
                list = References.Where(p => p.Value.DrawCode.Item2 != null).ToArray();
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            if (list != null)
            {
                foreach (var o in list)
                {
                    GuiService.Invalidate(o.Key); // appeler uniquement si le contenu de out a changé
                }
            }
        }

        /// <summary>
        /// Obtient le nom de fichier de l'objet
        /// </summary>
        public static string? GetContentFileName(DevObject obj)
        {
            try
            {
                var contentFileName = DevObject.References.Where(p => p.Value == obj).Select(p => p.Key).FirstOrDefault();

                if (contentFileName != null)
                    return Path.Combine(DataDir, contentFileName);
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Force la création du contenu à partir de son stockage permanent
        /// </summary>
        public abstract void LoadContent();
        /// <summary>
        /// Force l'écriture du contenu dans son stockage permanent
        /// </summary>
        public abstract void FlushContent();

        /// <summary>
        /// Charge le contenu des objets
        /// </summary>
        public static void LoadOutputs()
        {
            foreach (var o in References)
            {
                o.Value.LoadContent();
            }
        }

        /// <summary>
        /// Sauvegarde le contenu des objets
        /// </summary>
        public static void SaveOutput()
        {
            foreach (var o in References)
            {
                o.Value.FlushContent();
            }
        }

        /// <summary>
        /// Execute le script d'initialisation
        /// </summary>
        /// <remarks>Initialise uniquement les objets non initialisé</remarks>
        public static void Init()
        {
            foreach (var o in References.Where(p => p.Value.IsInitialized == false))
            {
                // Initialisation interne
                try
                {
                    o.Value.OnInit();
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine("******************************************");
                    Program.Logger.WriteLine("Init: " + o.Key);
                    Program.Logger.WriteLine(ex.Message);
                    Program.Logger.WriteLine("******************************************");
                }

                // Execute le script d'initialisation
                var engine = o.Value.InitMethod.Item2?.Engine;
                if (engine != null)
                {
                    try
                    {
                        var pyScope = engine.CreateScope();//lock Program.pyEngine !
                        pyScope.SetVariable("types", new DevApps.Scripts.NetTypes());
                        pyScope.SetVariable("out", new DevApps.Scripts.Output(o.Value.Content, Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                        pyScope.SetVariable("name", o.Key);
                        pyScope.SetVariable("desc", o.Value.Description);

                        foreach (var variable in DevVariable.References)
                        {
                            pyScope.SetVariable(variable.Key, variable.Value);
                        }

                        foreach (var variable in DevVariable.EnumPrivate())
                        {
                            pyScope.SetVariable(variable.Key, variable.Value);
                        }

                        foreach (var pointer in o.Value.Pointers)
                        {
                            Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                            pyScope.SetVariable(pointer.Key, new DevApps.Scripts.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, o.Key)));// mise en cache dans l'objet ?
                        }
                        foreach (var property in o.Value.Properties)
                        {
                            pyScope.SetVariable(property.Key, property.Value.Item2?.Execute(pyScope));
                        }
                        var result = o.Value.InitMethod.Item2?.Execute(pyScope);

                        o.Value.IsInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine("******************************************");
                        Program.Logger.WriteLine("Init: " + o.Key);
                        Program.Logger.WriteLine(engine.FormatError(ex));
                        Program.Logger.WriteLine("******************************************");
                    }
                }
            }
        }

        /// <summary>
        /// Construit l'objet et son arbre de dépendances en commençant par le plus profond
        /// </summary>
        /// <remarks>Seul les objets ayant un buildIndex > 0 sont construit</remarks>
        public static void BuildTree(KeyValuePair<string, DevObject> item)
        {
            var tree = Program.DevObject.CreateBuildTree(item);

            foreach (var t in tree)
            {
                if (References.TryGetValue(t.Item2, out var obj))
                {
                    if (obj.MustBeBuild)
                    {
                        Build(new KeyValuePair<string, DevObject>(t.Item2, obj));
                        obj.buildIndex = 0;
                    }
                }
            }

            Program.DevObject.Build(item);
            item.Value.buildIndex = 0;
        }

        /// <summary>
        /// Construit la sortie des objets
        /// </summary>
        public static void Build(KeyValuePair<string, DevObject> obj)
        {
            Build([obj]);
        }

        /// <summary>
        /// Construit la sortie des objets
        /// </summary>
        public static void Build()
        {
            Build(DevObject.References);
        }

        /// <summary>
        /// Construit la sortie des objets
        /// </summary>
        public static void Build(IEnumerable<KeyValuePair<string, DevObject>> objects)
        {
            foreach (var o in objects)
            {
                if (o.Value.BuildMethod.Item2 == null)
                    continue;

                var engine = o.Value.BuildMethod.Item2?.Engine;
                if (engine != null)
                {
                    try
                    {
                        var pyScope = engine.CreateScope();//lock Program.pyEngine !
                        pyScope.SetVariable("interpreter", DevApps.Scripts.Interpreter.Instance);
                        pyScope.SetVariable("types", new DevApps.Scripts.NetTypes());
                        pyScope.SetVariable("out", new DevApps.Scripts.Output(o.Value.Content, Path.Combine(Program.DataDir, o.Key)));
                        pyScope.SetVariable("name", o.Key);
                        pyScope.SetVariable("desc", o.Value.Description);

                        foreach (var variable in DevVariable.References)
                        {
                            pyScope.SetVariable(variable.Key, variable.Value.Value);
                        }

                        foreach (var variable in DevVariable.EnumPrivate())
                        {
                            pyScope.SetVariable(variable.Key, variable.Value.Value);
                        }

                        foreach (var pointer in o.Value.Pointers)
                        {
                            References.TryGetValue(pointer.Value.target, out var pointerRef);
                            pyScope.SetVariable(pointer.Key, new DevApps.Scripts.Output(pointerRef != null ? pointerRef.Content : new MemoryStream(), Path.Combine(Program.DataDir, pointer.Value.target)));
                        }
                        foreach (var property in o.Value.Properties)
                        {
                            pyScope.SetVariable(property.Key, property.Value.Item2?.Execute(pyScope));
                        }
                        var result = o.Value.BuildMethod.Item2?.Execute(pyScope);

                        // Evènement de build
                        o.Value.OnBuilt();

                        GuiService.Invalidate(o.Key);
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.WriteLine("******************************************");
                        Program.Logger.WriteLine("Build: " + o.Key);
                        Program.Logger.WriteLine(engine.FormatError(ex));
                        Program.Logger.WriteLine("******************************************");
                    }
                }
            }
        }

        /// <summary>
        /// Retourne l'arbre de construction d'un objet
        /// </summary>
        /// <returns>
        /// Liste des objets dans l'ordre de profondeur.
        /// La liste est trié du plus petit au plus grand (le plus éloigné)
        /// </returns>
        /// <remarks>Les boucles et les répétitions de pointeurs sont gérées</remarks>
        internal static List<Tuple<int, string>> CreateBuildTree(KeyValuePair<string, DevObject> obj)
        {
            var explored = new List<string>([obj.Key]);

            var tuples = new List<Tuple<int, string>>();

            void Explore(string key, int level)
            {
                explored.Add(key);
                tuples.Add(new Tuple<int, string>(level, key));

                level++;
                if (TryGet(key, out var obj))
                {
                    foreach (var o in obj.Pointers)
                    {
                        if (explored.Contains(o.Value.target))
                            continue;
                        Explore(o.Value.target, level);
                    }
                }
            }

            foreach (var o in obj.Value.Pointers)
            {
                if (explored.Contains(o.Value.target))
                    continue;
                Explore(o.Value.target, 1);
            }

            return tuples.OrderBy(t => t.Item1).ToList();
        }

        /// <summary>
        /// Retourne l'arbre des dépendances d'un objet
        /// </summary>
        /// <returns>
        /// Liste des objets dans l'ordre de profondeur.
        /// La liste est trié du plus petit au plus grand (le plus éloigné)
        /// </returns>
        /// <remarks>Les boucles et les répétitions de pointeurs sont gérées</remarks>
        internal static List<Tuple<int, string>> CreateDependancesTree(KeyValuePair<string, DevObject> obj)
        {
            var explored = new List<string>([obj.Key]);

            var tuples = new List<Tuple<int, string>>();

            void Explore(string key, int level)
            {
                explored.Add(key);

                level++;
                if (TryGet(key, out var obj))
                {
                    foreach (var o in References.Where(p => p.Value.Pointers.Any(q => q.Value.target == key)))
                    {
                        if (explored.Contains(o.Key))
                            continue;
                        tuples.Add(new Tuple<int, string>(level, o.Key));
                        Explore(o.Key, level);
                    }
                }
            }

            Explore(obj.Key, 0);

            return tuples.OrderBy(t => t.Item1).ToList();
        }

        /// <summary>
        /// Execute une fonction
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="func"></param>
        public static void Function(string obj, string func)
        {
            if (References.ContainsKey(obj))
            {
                var o = References[obj];
                if (o != null && o.Functions.ContainsKey(func))
                {
                    var f = o.Functions[func];

                    var engine = f.Item2?.Engine;
                    if (engine != null)
                    {
                        try
                        {
                            var result = f.Item2?.Execute(GetGlobalScope(engine));
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.WriteLine("******************************************");
                            Program.Logger.WriteLine("Function: " + func + " to " + obj);
                            Program.Logger.WriteLine(engine.FormatError(ex));
                            Program.Logger.WriteLine("******************************************");
                        }
                    }
                }
            }
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

            if (References.ContainsKey(obj))
            {
                var o = References[obj];
                if (o != null && o.Properties.ContainsKey(prop))
                {
                    var p = o.Properties[prop];
                    var engine = p.Item2?.Engine;
                    if (engine != null)
                    {
                        try
                        {
                            ret = p.Item2?.Execute(GetGlobalScope(engine));
                        }
                        catch (Exception ex)
                        {
                            Program.Logger.WriteLine("******************************************");
                            Program.Logger.WriteLine("Property: " + prop + " to " + obj);
                            Program.Logger.WriteLine(engine.FormatError(ex));
                            Program.Logger.WriteLine("******************************************");
                        }
                    }
                }
            }
            return ret;
        }

        public abstract void CompilDraw();
        public abstract void CompilObject();
        public abstract void CompilFunctions();
        public abstract void CompilProperties();
        public abstract void CompilUserAction();
        public abstract void CompilLoop();
        public abstract void CompilInit();
        public abstract void CompilBuild();

        /// <summary>
        /// Lie les objets externes par leurs noms
        /// </summary>
        public static void CompilObjects(IEnumerable<DevObject>? objects = null)
        {
            foreach (var o in (objects ?? References.Values).Where(p=>p is DevObjectInstance || p is DevObjectFile))
            {
                try
                {
                    o.CompilDraw();
                    o.CompilObject();
                    o.CompilFunctions();
                    o.CompilProperties();
                    o.CompilUserAction();
                    o.CompilLoop();
                    o.CompilInit();
                    o.CompilBuild();
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine($"Erreur de compilation sur l'objet {o.Description}");
                    Program.Logger.WriteLine(ex.Message);
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


        public abstract DevObject Clone();
        public abstract bool AsModel();
        public abstract bool IsModel();
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
        public abstract string? GetProperty(string name);
        public abstract DevObject AddProperty(string name, string? code);
        public abstract IEnumerable<KeyValuePair<string, string?>> GetFunctions();
        public abstract void SetFunctions(IEnumerable<KeyValuePair<string, string?>> items);
        public abstract string? GetFunction(string name);
        public abstract DevObject AddFunction(string name, string code);
        public abstract string GetUserAction();
        public abstract DevObject SetUserAction(string code);
        public abstract Pointer? GetPointer(string name);
        public abstract DevObject AddPointer(string name, string reference, string[] tags);

        internal static (string, CompiledCode?) DrawCodeFromExt(string ext)
        {
            switch (ext)
            {
                case ".md":
                    return ("gui.md(out)", null);
                case ".txt":
                case ".log":
                case ".csv":
                case ".json":
                case ".xml":
                    return ("gui.text(out)", null);
                default:
                    return ("gui.icon('file')", null);
            }
        }
    }
}