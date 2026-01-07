using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.IO;
using System.Text;

internal partial class Program
{
    /// <summary>
    /// Objet de fichier
    /// Un objet pointant son contenu directement sur un flux de fichier du répertoire de données
    /// Ce type d'objet est généralement utilisé pour les fichiers volumineux ou au besoin persistant
    /// </summary>
    public class DevObjectFile : DevObject
    {
        public DevObjectFile()
        {
        }

        public DevObjectFile(string filename)
        {
            this.filename = filename;
        }

        public override void OnInit()
        {
            if(filename != null)
            {
                if(Path.GetFullPath(filename).StartsWith(Path.GetFullPath(DataDir)) == false)
                    throw new Exception("L'Accès aux fichiers en dehors du répertoire de données est interdit");
            }
        }

        public override void OnDelete()
        {
            if (this.fileStream != null)
            {
                this.fileStream.Close();
                this.fileStream = null;
            }
        }

        /// <summary>
        /// Force la création du contenu à partir de son stockage permanent
        /// </summary>
        public override void LoadContent()
        {
            try
            {
                if (this.filename != null && this.fileStream == null)
                {
                    this.fileStream = new FileStream(this.filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("load object data " + this.filename + " failed");
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Force l'écriture du contenu dans son stockage permanent
        /// </summary>
        public override void FlushContent()
        {
            if (Content == null)
                return;

            try
            {
                if (this.fileStream != null)
                {
                    this.fileStream.Flush();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("save object data " + this.fileStream?.Name + " failed");
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Accès aux données de l'objet
        /// </summary>
        internal string? filename;
        internal FileStream? fileStream;
        public override Stream Content { get { return fileStream ?? MemoryStream.Null; } }

        /// <summary>
        /// Pointeurs vers des objets existants
        /// </summary>
        internal Dictionary<string, Pointer> pointers = new Dictionary<string, Pointer>(); // name, refName
        public override Dictionary<string, Pointer> Pointers { get { return pointers; } }
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

        /// <summary>
        /// Tags de l'objet
        /// </summary>
        internal HashSet<string> tags = new HashSet<string>();
        public override string[] Tags { get { return tags.ToArray(); } }

        public string Output
        {
            get
            {
                if (fileStream != null && fileStream.CanRead)
                {
                    using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, 1024, true))//encoding a détecter
                    {
                        fileStream.Position = 0;
                        var text = reader.ReadToEnd();
                        fileStream.Position = 0;
                        return text;
                    }
                }
                return String.Empty;
            }
        }


        public override DevObjectFile Clone()
        {
            throw new Exception("Un objet de fichier ne peut pas être cloné");
        }

        public override bool IsModel()
        {
            return false;
        }

        public override bool AsModel()
        {
            return false;
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
            if(fileStream == null || fileStream.CanWrite == false)
                return this;
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Write(data);
            fileStream.SetLength(data.Length);
            return this;
        }

        public override DevObject SetOutput(string text, bool removeIdent = false)
        {
            if (fileStream == null || fileStream.CanWrite == false)
                return this;
            var data = Encoding.UTF8.GetBytes(removeIdent ? RemoveIdent(text) : text);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Write(data);
            fileStream.SetLength(data.Length);
            return this;
        }

        public override DevObject LoadOutput(string name, string? path = null)
        {
            throw new Exception("Un objet de fichier ne contient pas de données locales");
        }

        public override DevObject SaveOutput(string name, string? path = null)
        {
            throw new Exception("Un objet de fichier ne contient pas de données locales");
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

        public override string? GetProperty(string name)
        {
            if (Properties.TryGetValue(name, out var value))
                return value.Item1;
            return null;
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
                if (p.Value == null)
                {
                    Console.WriteLine($"Fonction {p.Key} sans code ignoré");
                    continue;
                }
                AddFunction(p.Key, p.Value);
            }
        }

        public override string? GetFunction(string name)
        {
            if (Functions.TryGetValue(name, out var value))
                return value.Item1;
            return null;
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

        public override Pointer? GetPointer(string name)
        {
            if (Pointers.TryGetValue(name, out var value))
                return value;
            return null;
        }

        public override DevObject AddPointer(string name, string reference, string[] tags)
        {
            Pointers[name] = new Pointer { target = reference, tags = new HashSet<string>(tags) };
            return this;
        }

        public override void CompilDraw()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(drawCode.Item1) == false)
                    {
                        string sourceCode = drawCode.Item1;
                        var sourceEngine = Program.GetScriptEngine(sourceCode);
                        ScriptSource source = sourceEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                        CompiledCode compiled = source.Compile();
                        drawCode = (sourceCode, compiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilObject()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(objectCode.Item1) == false)
                    {
                        string sourceCode = objectCode.Item1;
                        var sourceEngine = Program.GetScriptEngine(sourceCode);
                        ScriptSource source = sourceEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                        CompiledCode compiled = source.Compile();
                        objectCode = (sourceCode, compiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilFunctions()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    foreach (var f in functions.ToArray())
                    {
                        string functionCode = f.Value.Item1;
                        if (String.IsNullOrWhiteSpace(functionCode) == false)
                        {
                            var sourceEngine = Program.GetScriptEngine(functionCode);
                            ScriptSource functionScript = sourceEngine.CreateScriptSourceFromString(functionCode, SourceCodeKind.Statements);
                            CompiledCode functionCompiled = functionScript.Compile();
                            functions[f.Key] = (functionCode, functionCompiled);
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilProperties()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    foreach (var f in properties.ToArray())
                    {
                        string propertyCode = f.Value.Item1;
                        if (String.IsNullOrWhiteSpace(propertyCode) == false)
                        {
                            var sourceEngine = Program.GetScriptEngine(propertyCode);
                            ScriptSource propertyScript = sourceEngine.CreateScriptSourceFromString(propertyCode, SourceCodeKind.Expression);
                            CompiledCode propertyCompiled = propertyScript.Compile();
                            properties[f.Key] = (propertyCode, propertyCompiled);
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilUserAction()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(userAction.Item1) == false)
                    {
                        string sourceCode = userAction.Item1;
                        var sourceEngine = Program.GetScriptEngine(sourceCode);
                        ScriptSource source = sourceEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
                        CompiledCode compiled = source.Compile();
                        userAction = (sourceCode, compiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilLoop()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(loopMethod.Item1) == false)
                    {
                        string sampleCode = loopMethod.Item1;
                        var sourceEngine = Program.GetScriptEngine(sampleCode);
                        ScriptSource sampleScript = sourceEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                        CompiledCode sampleCompiled = sampleScript.Compile();
                        loopMethod = (sampleCode, sampleCompiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilInit()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(initMethod.Item1) == false)
                    {
                        string sampleCode = initMethod.Item1;
                        var sourceEngine = Program.GetScriptEngine(sampleCode);
                        ScriptSource sampleScript = sourceEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                        CompiledCode sampleCompiled = sampleScript.Compile();
                        initMethod = (sampleCode, sampleCompiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }

        public override void CompilBuild()
        {
            var handle = mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(buildMethod.Item1) == false)
                    {
                        string sampleCode = buildMethod.Item1;
                        var sourceEngine = Program.GetScriptEngine(sampleCode);
                        ScriptSource sampleScript = sourceEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
                        CompiledCode sampleCompiled = sampleScript.Compile();
                        buildMethod = (sampleCode, sampleCompiled);
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    mutexExecuteObjects.ReleaseMutex();
                }
            }
        }
    }
}