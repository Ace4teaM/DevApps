using static Program;

namespace DevApps.Features
{
    internal static class Facets
    {
        /// <summary>
        /// Execute le script de construction de la sortie standard des objets
        /// </summary>
        public static async Task Build(string name)
        {
            await DevObject._executeLock.WaitAsync();

            try
            {
                if (DevFacet.TryGet(name, out var facet))
                {
                    facet.Build();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

    }
}
