using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IronPython.Modules._ast;

internal partial class Program
{
    internal class DevFacet
    {
        public static Dictionary<string, DevFacet> References = new Dictionary<string, DevFacet>();
        public DevSelect Objects { get; set; } = new DevSelect();

        public static DevFacet Create(string name, DevSelect select)
        {
            var o = new DevFacet();
            o.Objects = select;
            References.Add(name, o);
            return o;
        }

        public static DevFacet? Get(string name)
        {
            return References.GetValueOrDefault(name);
        }

        /// <summary>
        /// Commandes systèmes
        /// </summary>
        /// <remarks>
        /// $out = chemin vers le fichier contenant la sortie standard de cet objet
        /// $dir = dossier du projet
        /// $<obj> = la valeur de la sortie standard d'un objet. <obj> est le nom de l'objet ciblé
        /// </remarks>
        public List<string> BuildCommands { get; } = new List<string>();

        public DevFacet AddBuildCommand(string command)
        {
            BuildCommands.Add(command);
            return this;
        }

        public string WindowsPathToLinuxPath(string path)
        {
            return path.Replace(@":\", @"/").Insert(0, @"/").Replace(@"\", @"/");
        }

        /// <summary>
        /// Execute le script de construction de la sortie standard des objets
        /// </summary>
        public void Build()
        {
            DevObject.mutexExecuteObjects.WaitOne();

            var refs = DevObject.References.ToArray();

            // génère les variables
            // note les variables sont générées avec un build de retard
            foreach (var o in refs)
            {
                // ajoute la sortie aux variables
                var value = o.Value.buildStream.ToString();
                pyScope.SetVariable(o.Key.ToLower(), value);
            }


            // génère le contenu de chaque objet
            foreach (var o in refs)
            {
                // la phase de build utilise la sortie standard pour récupérer les données
                pyEngine.Runtime.IO.SetOutput(o.Value.buildStream, Encoding.UTF8);
                var result = o.Value.BuildMethod.Item2?.Execute(pyScope);
            }

            // on rétablie la sortie standard vers la console
            pyEngine.Runtime.IO.RedirectToConsole();

            // exécute l'environnement de commandes
            try
            {
                var shellPath = "powershell.exe";
                var shellSet = @"set {0} ""{1}""";
                var shellExit = @"exit";

                // creation de l'environnement de commandes
                using System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;//System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = shellPath;
                startInfo.UseShellExecute = false;
                //startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardInput = true;
                startInfo.CreateNoWindow = false;
                process.StartInfo = startInfo;

                if (process.Start())
                {
                    // ajout des variables locales
                    StreamWriter ws = process.StandardInput;

                    ws.WriteLine(String.Format(shellSet, "dir", Path.GetFullPath(".")));

                    // ajout lien vers les objets
                    foreach (var o in refs)
                        ws.WriteLine(String.Format(shellSet, o.Key, Path.GetFullPath(Path.Combine(DataDir, "data", o.Key))));

                    // on execute les commandes
                    foreach (var c in BuildCommands)
                    {
                        ws.WriteLine(c);
                    }

                    ws.WriteLine(shellExit);
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            finally
            {
            }

            DevObject.mutexExecuteObjects.ReleaseMutex();
        }

    }
}
