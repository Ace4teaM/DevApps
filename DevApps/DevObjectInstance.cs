using Microsoft.Scripting;
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

        public override Pointer? GetPointer(string name)
        {
            return Pointers.ContainsKey(name) ? Pointers[name] : null;
        }

        public override DevObject AddPointer(string name, string reference, string[] tags)
        {
            Pointers[name] = new Pointer { target=reference, tags=new HashSet<string>(tags) };
            return this;
        }

        public override void CompilDraw()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(drawCode.Item1) == false)
                {
                    string sourceCode = drawCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
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

        public override void CompilObject()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(objectCode.Item1) == false)
                {
                    string sourceCode = objectCode.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
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

        public override void CompilFunctions()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                foreach (var f in functions.ToArray())
                {
                    string functionCode = f.Value.Item1;
                    if (String.IsNullOrWhiteSpace(functionCode) == false)
                    {
                        ScriptSource functionScript = pyEngine.CreateScriptSourceFromString(functionCode, SourceCodeKind.Statements);
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

        public override void CompilProperties()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                foreach (var f in properties.ToArray())
                {
                    string propertyCode = f.Value.Item1;
                    if (String.IsNullOrWhiteSpace(propertyCode) == false)
                    {
                        ScriptSource propertyScript = pyEngine.CreateScriptSourceFromString(propertyCode, SourceCodeKind.Expression);
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

        public override void CompilUserAction()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(userAction.Item1) == false)
                {
                    string sourceCode = userAction.Item1;
                    ScriptSource source = pyEngine.CreateScriptSourceFromString(sourceCode, SourceCodeKind.Statements);
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

        public override void CompilLoop()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(loopMethod.Item1) == false)
                {
                    string sampleCode = loopMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
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

        public override void CompilInit()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(initMethod.Item1) == false)
                {
                    string sampleCode = initMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
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

        public override void CompilBuild()
        {
            mutexExecuteObjects.WaitOne();
            try
            {
                if (String.IsNullOrWhiteSpace(buildMethod.Item1) == false)
                {
                    string sampleCode = buildMethod.Item1;
                    ScriptSource sampleScript = pyEngine.CreateScriptSourceFromString(sampleCode, SourceCodeKind.Statements);
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