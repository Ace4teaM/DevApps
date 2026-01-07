using DevApps.Scripts;
using IronPython.Hosting;


namespace DevApps.PythonExtends
{
    public sealed class PythonScriptEngine : ScriptEngine
    {
        internal readonly Microsoft.Scripting.Hosting.ScriptEngine engine;

        public PythonScriptEngine()
        {
            engine = Python.CreateEngine();
        }

        public PythonScriptEngine(Microsoft.Scripting.Hosting.ScriptEngine engine)
        {
            this.engine = engine;
        }

        public override ScriptScope CreateScope()
            => new PythonScriptScope(engine.CreateScope());

        public override ScriptSource CreateStatementsFromString(string code)
            => new PythonScriptSource(engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Statements));

        public override ScriptSource CreateExpressionFromString(string code)
            => new PythonScriptSource(engine.CreateScriptSourceFromString(code, Microsoft.Scripting.SourceCodeKind.Expression));

        public override string FormatError(Exception ex)
        {
            var eo = engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
            return eo.FormatException(ex);
        }

        public override void Dispose() { }
    }

    public sealed class PythonScriptScope : ScriptScope
    {
        internal readonly Microsoft.Scripting.Hosting.ScriptScope scope;

        public PythonScriptScope(Microsoft.Scripting.Hosting.ScriptScope scope)
        {
            this.scope = scope;
        }

        public override void SetVariable(string name, object value)
            => scope.SetVariable(name, value);
        public override void RemoveVariable(string name)
            => scope.RemoveVariable(name);
    }

    public sealed class PythonScriptSource : ScriptSource
    {
        private readonly Microsoft.Scripting.Hosting.ScriptSource source;

        public PythonScriptSource(Microsoft.Scripting.Hosting.ScriptSource source)
        {
            this.source = source;
        }

        public override CompiledCode Compile()
            => new PythonCompiledCode(source.Compile());
    }

    public sealed class PythonCompiledCode : CompiledCode
    {
        private readonly Microsoft.Scripting.Hosting.CompiledCode code;

        public PythonCompiledCode(Microsoft.Scripting.Hosting.CompiledCode code)
        {
            this.code = code;
        }

        internal override ScriptEngine Engine => new PythonScriptEngine(code.Engine);

        public override object Execute(ScriptScope scope)
        {
            var pyScope = (PythonScriptScope)scope;
            return code.Execute(pyScope.scope);
        }
    }
}
