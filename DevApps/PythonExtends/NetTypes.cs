namespace DevApps.PythonExtends
{
    /// <summary>
    /// Fournit des méthodes au langage Python pour convertir des types en .NET
    /// </summary>
    public class NetTypes
    {
        /// <summary>
        /// Méthode Python: converti un Bytes (python) en Array<paramref name="_bytes"/> (.net)
        /// </summary>
        public byte[] array(IronPython.Runtime.Bytes _bytes)
        {
            return _bytes.ToArray();
        }
    }
}
