using Microsoft.ClearScript.V8;

namespace DevApps.Scripts.ClearScriptExtends
{
    public sealed class V8ScriptEngineAdapter : ScriptEngine
    {
        internal readonly V8ScriptEngine engine;

        public override string Name => "Javascript";
        public override string HighlightName => "JavaScript";

        public V8ScriptEngineAdapter()
        {
            engine = new();
        }

        public V8ScriptEngineAdapter(V8ScriptEngine engine)
        {
            this.engine = engine;
        }

        public override ScriptScope CreateScope()
            => new V8ScriptScope(engine);

        public override ScriptSource CreateStatementsFromString(string code)
            => new V8ScriptSource(engine, code, false);

        public override ScriptSource CreateExpressionFromString(string code)
            => new V8ScriptSource(engine, code, true);

        public override string FormatError(Exception ex)
        {
            return ex.Message;
        }

        public override void Dispose()
            => engine.Dispose();
    }

    public sealed class V8ScriptScope : ScriptScope
    {
        internal readonly V8ScriptEngine engine;

        public V8ScriptScope(V8ScriptEngine engine)
        {
            this.engine = engine;
        }

        public override void SetVariable(string name, object value)
            => engine.AddHostObject(name, value);
        public override void RemoveVariable(string name)
            => engine.Script.DeleteProperty(name);

        public override IEnumerable<Tuple<string, object>> GetVariables()
        {
            return Array.Empty<Tuple<string, object>>();
        }

        public override bool TryGetVariable(string name, out object value)
        {
            value = null;
            return false;
        }
    }

    public sealed class V8ScriptSource : ScriptSource
    {
        private readonly V8ScriptEngine engine;
        private readonly string code;
        private readonly bool isExpression;

        public V8ScriptSource(V8ScriptEngine engine, string code, bool isExpression)
        {
            this.engine = engine;
            this.code = code;
            this.isExpression = isExpression;
        }

        public override CompiledCode Compile()
        {
            var script = engine.Compile(code);
            return new V8CompiledCode(engine, script, isExpression);
        }
    }

    public sealed class V8CompiledCode : CompiledCode
    {
        private readonly V8ScriptEngine engine;
        private readonly V8Script script;
        private readonly bool isExpression;

        public override ScriptEngine Engine => new V8ScriptEngineAdapter(engine);

        public V8CompiledCode(V8ScriptEngine engine, V8Script script, bool isExpression)
        {
            this.engine = engine;
            this.script = script;
            this.isExpression = isExpression;
        }

        public override object Execute(ScriptScope scope)
        {
            if(isExpression)
                return engine.Evaluate(script);
            
            engine.Execute(script);
            return null;
        }
    }
}
