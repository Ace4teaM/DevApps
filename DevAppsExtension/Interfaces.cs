namespace DevApps.Scripts
{
    public abstract class ScriptEngine : IDisposable
    {
        public abstract string Name { get; }
        public abstract string HighlightName { get; }
        public abstract ScriptScope CreateScope();
        public abstract ScriptSource CreateStatementsFromString(string code);
        public abstract ScriptSource CreateExpressionFromString(string code);
        public abstract string FormatError(Exception ex);
        public abstract void Dispose();
    }

    public abstract class ScriptScope
    {
        public abstract void SetVariable(string name, object value);
        public abstract void RemoveVariable(string v);
    }

    public abstract class ScriptSource
    {
        public abstract CompiledCode Compile();
    }

    public abstract class CompiledCode
    {
        public abstract ScriptEngine Engine { get; }

        public abstract object Execute(ScriptScope scope);
    }
}
