using DevApps.GUI;
using PdfSharp.Charting;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;

internal partial class Program
{
    public class DevFacet
    {
        public enum TitlePlacement
        {
            None,
            Center,
            TopLeft,
            TopRight
        }

        public class Text
        {
            public Text(double X, double Y, string text)
            {
                this.text = text;
                this.Y = Y;
                this.X = X;
            }

            /// <summary>
            /// Guid utilisé pour identifier l'objet dans l'éditeur
            /// La valeur n'est pas serialisée
            /// </summary>
            [Newtonsoft.Json.JsonIgnore]
            internal Guid guid = Guid.NewGuid();

            public string text;
            public double X, Y;
        }

        public class Geometry
        {
            public Geometry(double X, double Y, string path)
            {
                this.path = path;
                this.Y = Y;
                this.X = X;
            }

            /// <summary>
            /// Guid utilisé pour identifier l'objet dans l'éditeur
            /// La valeur n'est pas serialisée
            /// </summary>
            [Newtonsoft.Json.JsonIgnore]
            internal Guid guid = Guid.NewGuid();

            public string path;
            public double X, Y;
        }

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
                title = TitlePlacement.TopLeft;
            }

            public System.Windows.Rect GetZone()
            {
                return zone;
            }

            public ObjectProperties SetZone(System.Windows.Rect rect)
            {
                this.zone = rect;
                return this;
            }

            public string? GetBackground()
            {
                return background;
            }

            public ObjectProperties SetBackground(string bg)
            {
                this.background = bg;
                return this;
            }

            public TitlePlacement GetTitle()
            {
                return title;
            }

            public ObjectProperties SetTitle(TitlePlacement title)
            {
                this.title = title;
                return this;
            }

            public System.Windows.Rect zone;
            public string? background;
            internal TitlePlacement title;
            public string? TitlePos
            {
                get
                {
                    return Enum.GetName(typeof(TitlePlacement), title);
                }
                set
                {
                    Enum.TryParse<TitlePlacement>(value, out title);
                }
            }
        }

        public class CommandProperties
        {
            static double X = 10;
            static double Y = 10;

            public static System.Windows.Point GenerateNextPosition()
            {
                var pos = new System.Windows.Point(X, Y);

                X += 128 + 10;
                if (X > 500)
                {
                    X = 10;
                    Y += 128 + 10;
                }

                return pos;
            }

            public CommandProperties()
            {
                pos = GenerateNextPosition();
            }

            public System.Windows.Point GetPosition()
            {
                return pos;
            }

            public CommandProperties SetPosition(System.Windows.Point pos)
            {
                this.pos = pos;
                return this;
            }

            public System.Windows.Point pos;
        }

        public static Dictionary<string, DevFacet> References = new Dictionary<string, DevFacet>();
        internal Dictionary<string,ObjectProperties> Objects = new Dictionary<string, ObjectProperties>();
        internal Dictionary<string, CommandProperties> Commands = new Dictionary<string, CommandProperties>();
        internal List<Geometry> Geometries = new List<Geometry>();
        internal List<Text> Texts = new List<Text>();

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
                if (p.Value != null)
                    Objects.Add(p.Key, p.Value);
            }
        }

        public IEnumerable<KeyValuePair<string, CommandProperties?>> GetCommands()
        {
            return Commands.Select(p => new KeyValuePair<string, CommandProperties?>(p.Key, p.Value));
        }

        public void SetCommands(IEnumerable<KeyValuePair<string, CommandProperties?>> items)
        {
            Commands.Clear();
            foreach (var p in items)
            {
                if(p.Value != null)
                    Commands.Add(p.Key, p.Value);
            }
        }

        public IEnumerable<Geometry> GetGeometries()
        {
            return Geometries;
        }

        public void SetGeometries(IEnumerable<Geometry> items)
        {
            Geometries.Clear();
            foreach (var p in items)
            {
                Geometries.Add(p);
            }
        }

        public IEnumerable<Text> GetTexts()
        {
            return Texts;
        }

        public void SetTexts(IEnumerable<Text> items)
        {
            Texts.Clear();
            foreach (var p in items)
            {
                Texts.Add(p);
            }
        }

        /// <summary>
        /// Représente la zone d'impression de la page
        /// </summary>
        public System.Windows.Rect PrintLayout { get; set; } = new System.Windows.Rect(0,0,1000,1000);

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

        public string WindowsPathToLinuxPath(string path)
        {
            return path.Replace(@":\", @"/").Insert(0, @"/").Replace(@"\", @"/");
        }

        /// <summary>
        /// Execute le script de construction de la sortie standard des objets
        /// </summary>
        public void Build()
        {
            var handle = DevObject.mutexExecuteObjects.WaitOne();
            if (handle)
            {
                var refs = DevObject.References.Where(p => Objects.ContainsKey(p.Key)).ToArray();

                DevObject.Build(refs);

                // exécute l'environnement de commandes
                try
                {
                    var shellPath = "powershell.exe";
                    var shellSet = @"set {0} ""{1}""";
                    var shellEnv = @"$Env:PATH += "";{0}""";
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

                        // ajout les chemins d'accès aux outils
                        foreach (var o in GuiService.externalsTools)
                            ws.WriteLine(String.Format(shellEnv, o.Value.Replace("\"", "")));

                        // ajout lien vers les objets
                        foreach (var o in refs)
                            ws.WriteLine(String.Format(shellSet, o.Key, Path.GetFullPath(Path.Combine(DataDir, o.Key))));

                        // on execute les commandes
                        foreach (var c in Commands)
                        {
                            ws.WriteLine(c.Value);
                        }

                        ws.WriteLine(shellExit);
                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine("Facette: Erreur d'execution de la commande");
                    Program.Logger.WriteLine(ex.Message);
                }

                DevObject.mutexExecuteObjects.ReleaseMutex();
            }
        }

    }
}
