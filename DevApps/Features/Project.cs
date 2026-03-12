using DevApps.Commands;
using DevApps.Print;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Text;
using static Program;

namespace DevApps.Features
{
    internal static class Project
    {
        /// <summary>
        /// Retourne le contenu du journal de développement
        /// </summary>
        [Description("Retourne le contenu du journal de développement")]
        [RemoteCall]
        internal static async Task<string> ReadDevLog()
        {
            if (File.Exists(Program.JournalFilename))
            {
                return await File.ReadAllTextAsync(Program.JournalFilename);
            }
            return string.Empty;
        }
        /// <summary>
        /// Retourne une description textuelle du projet
        /// </summary>
        [Description("Retourne une description textuelle du projet")]
        [RemoteCall]
        public static async Task<string> Summary()
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                await DevObject._checkLock.WaitAsync();

                if (DevObject.References.Count == 0)
                {
                    sb.AppendLine($"It has no objects.");
                }
                else
                {
                    sb.AppendLine($"It has {DevObject.References.Count} objects:");
                    foreach (var obj in DevObject.References)
                    {
                        sb.AppendLine($"- {obj.Key}: {obj.Value.Description}");
                    }
                }
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Retourne une définition structuré du projet
        /// </summary>
        [Description("Retourne une définition structuré du projet")]
        [RemoteCall]
        public static async Task<dynamic> GetData()
        {
            dynamic data = new ExpandoObject();
            data.Name = Path.GetFileName(Environment.CurrentDirectory);

            try
            {
                await DevObject._checkLock.WaitAsync();

                data.ObjectsCount = DevObject.References.Count;
                data.Objects = DevObject.References.Select(p => new { p.Key, p.Value.Description });
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            try
            {
                await DevVariable._checkLock.WaitAsync();

                data.VariablesCount = DevVariable.References.Count;
                data.Variables = DevVariable.References.Select(p => new  { p.Key, p.Value.Description });
            }
            finally
            {
                DevVariable._checkLock.Release();
            }

            try
            {
                await DevFile._checkLock.WaitAsync();

                data.VariablesCount = DevFile.References.Count;
                data.Variables = DevFile.References.Select(p => new { p.Key, p.Value.Filename, p.Value.ObjectName });
            }
            finally
            {
                DevFile._checkLock.Release();
            }

            return data;
        }
    }
}
