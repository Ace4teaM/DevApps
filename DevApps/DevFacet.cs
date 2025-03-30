using System.IO;
using System.Text;
using System.Windows;

internal partial class Program
{
    public class DevFacet
    {
        public class ObjectProperties
        {
            static double X = 10;
            static double Y = 10;

            public static Rect GenerateNextPosition(double width, double height)
            {
                var rect = new Rect(X, Y, width, height);

                X += width + 10;
                if (X > 500)
                {
                    X = 10;
                    Y += height + 10;
                }

                return rect;
            }

            public ObjectProperties()
            {
                zone = GenerateNextPosition(100, 100);
            }

            public System.Windows.Rect GetZone()
            {
                return zone;
            }

            public ObjectProperties SetZone(System.Windows.Rect rect)
            {
                zone = rect;
                return this;
            }

            public System.Windows.Rect zone;
        }
        public static Dictionary<string, DevFacet> References = new Dictionary<string, DevFacet>();
        internal Dictionary<string,ObjectProperties> Objects = new Dictionary<string, ObjectProperties>();

        /// <summary>
        /// trouve un nom unique
        /// </summary>
        /// <param name="name"></param>
        public static void MakeUniqueName(ref string name)
        {
            var newName = name;
            int n = 2;
            while (References.ContainsKey(newName))
            {
                newName = name + n;
                n++;
            }

            name = newName;
        }

        public IEnumerable<KeyValuePair<string, ObjectProperties?>> GetObjects()
        {
            return Objects.Select(p => new KeyValuePair<string, ObjectProperties?>(p.Key, p.Value));
        }

        public void SetObjects(IEnumerable<KeyValuePair<string, ObjectProperties?>> items)
        {
            Objects.Clear();
            foreach (var p in items)
            {
                if(p.Value != null)
                    Objects.Add(p.Key, p.Value);
            }
        }

        public static DevFacet Create(string name, string[] objectNames)
        {
            var o = new DevFacet();
            foreach(var obj in objectNames)
            {
                o.Objects.Add(obj, new ObjectProperties());
            }
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
