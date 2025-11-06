using DevApps.GUI;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

internal partial class Program
{
    /// <summary>
    /// Représente une définition de commande concrète
    /// Les commandes sont utilisées pour interagir avec les documents externes (le projet final)
    /// </summary>
    public abstract class DevCommandDefinition
    {
        public abstract string? Description { get; }
        public abstract string? Syntaxe { get; }
        public abstract Func<DevCommand, int> Execute { get; }

        public static Dictionary<string, DevCommandDefinition> BuiltIn = new Dictionary<string, DevCommandDefinition>(){
            { "print", new DevPrintCommand() },
            { "build", new DevBuildCommand() },
            { "buildall", new DevBuildAllCommand() },
            { "copy", new DevCopyCommand() },
        };

        private static bool ParseString(ref string argument)
        {
            return true;
        }

        public class DevPrintCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Imprime un message"; } }
            public override string? Syntaxe { get { return "print [votre message]"; } }
            public override Func<DevCommand, int> Execute { get { return Print; } }
            private static int Print(DevCommand cmd)
            {
                if (cmd.Arguments.Count < 1)
                    throw new ArgumentException();

                var message = String.Join("", cmd.Arguments); // todo : validation format
                ParseString(ref message);

                Console.WriteLine(message);
                return 0;
            }
        }

        public class DevBuildCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Construit le contenu d'un objet"; } }
            public override string? Syntaxe { get { return "build [NomObjet]"; } }
            public override Func<DevCommand, int> Execute { get { return Build; } }
            private static int Build(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                if (cmd.Arguments.Count < 1)
                    throw new ArgumentException();

                var name = cmd.Arguments[0]; // todo : validation format
                if (DevObject.TryGet(name, out var obj) == false)
                    throw new Exception(@"L'objet {name} n'existe pas");

                DevObject.Build(new KeyValuePair<string, DevObject>( name, obj ));

                var currentView = DevApps.GUI.GuiService.EditorWindow?.Content as DesignerDataView;

                if (currentView != null)
                {
                    currentView.InvalidateObjects();
                }

                return 0;
            }
        }

        public class DevBuildAllCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Construit le contenu de tous les objets"; } }
            public override string? Syntaxe { get { return "buildall"; } }
            public override Func<DevCommand, int> Execute { get { return Run; } }
            private static int Run(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                var handle = false;

                try
                {
                    handle = DevObject.mutexCheckObjectList.WaitOne();

                    if (handle)
                    {
                        DevObject.Build(DevObject.References);

                        var currentView = DevApps.GUI.GuiService.EditorWindow?.Content as DesignerDataView;

                        if (currentView != null)
                        {
                            currentView.InvalidateObjects();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    if (handle)
                    {
                        DevObject.mutexCheckObjectList.ReleaseMutex();
                    }
                }

                return 0;
            }
        }

        public class DevCopyCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Copie le contenu d'un objet dans un autre"; } }
            public override string? Syntaxe { get { return "copy [ObjetSource] [ObjetDestination]"; } }
            public override Func<DevCommand, int> Execute { get { return Run; } }
            private static int Run(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                if (cmd.Arguments.Count < 2)
                    throw new ArgumentException();

                var name1 = cmd.Arguments[0]; // todo : validation format
                var name2 = cmd.Arguments[1]; // todo : validation format

                if (DevObject.TryGet(name1, out var obj1) == false)
                    throw new Exception(@"L'objet {name1} n'existe pas");

                if (DevObject.TryGet(name1, out var obj2) == false)
                    throw new Exception(@"L'objet {name2} n'existe pas");

                if (obj1.Content == null)
                    throw new Exception(@"L'objet {name1} ne contient pas de données");

                if (obj2.Content == null)
                    throw new Exception(@"L'objet {name2} ne contient pas de données");

                obj1.Content.CopyTo(obj2.Content);

                return 0;
            }
        }

        public class DevShellCommand : DevCommandDefinition
        {
            /// <summary>
            /// Enumère les commandes autorisées par le registre
            /// </summary>
            internal static void EnumPrivate()
            {
                try
                {
                    var registryKey = @"SOFTWARE\DevApps\Commands\RunShell";

                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetValueNames())
                            {
                                var var = new DevCommand();
                                var value = key?.GetValue(subKeyName);
                                if(value != null)
                                {
                                    try
                                    {
                                        var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(value?.ToString() ?? "{}");

                                        BuiltIn.Add(subKeyName, new DevShellCommand
                                        {
                                            description = arguments["description"].ToString().Trim(),
                                            syntaxe = arguments["syntaxe"].ToString().Trim(),
                                            command = arguments["command"].ToString().Trim(),
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine(subKeyName+" Invalid Command Definition :");
                                        Console.Error.WriteLine("Erreur : " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Enum Commands Definitions error:");
                    Console.Error.WriteLine("Erreur : " + ex.Message);
                }
            }

            internal required string description;
            internal required string syntaxe;
            internal required string command;
            public override string Description { get { return description; } }
            public override string? Syntaxe { get { return syntaxe; } }
            public string Command { get { return command; } }
            public override Func<DevCommand, int> Execute { get { return RunShell; } }
            public static int RunShell(DevCommand cmd)
            {
                try
                {
                    if (!(DevCommandDefinition.BuiltIn.ContainsKey(cmd.Name) && DevCommandDefinition.BuiltIn[cmd.Name] is DevShellCommand))
                        throw new Exception($"La commande '{cmd.Name}' n'existe pas.");

                    var def = (DevShellCommand)DevCommandDefinition.BuiltIn[cmd.Name];

                    var args = cmd.Arguments.ToArray();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].StartsWith("$"))
                        {
                            if (DevVariable.References.TryGetValue(args[i].Substring(1), out var variable))
                            {
                                args[i] = variable.Value.ToString() ?? String.Empty;
                            }
                        }
                    }

                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/C " + String.Format(def.Command, args);
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.EnvironmentVariables["PATH"] += $";{GuiService.ExternalToolsPaths}";

                    if (cmd.Input != null)
                    {
                        process.StartInfo.RedirectStandardInput = true;
                    }

                    process.Start();

                    if (cmd.Input != null)
                    {
                        if (!process.HasExited)
                            process.StandardInput.BaseStream.Write(cmd.Input.ToArray(), 0, (int)cmd.Input.Length);
                        if(!process.HasExited)
                            process.StandardInput.BaseStream.Flush();
                        if (!process.HasExited)
                            process.StandardInput.Close();
                    }

                    if (cmd.Output != null)
                    {
                        process.StandardOutput.BaseStream.CopyTo(cmd.Output);
                        cmd.Output.Position = 0;
                    }
                    process.WaitForExit();
                    return process.ExitCode;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Run Shell Command error:");
                    Console.Error.WriteLine("Erreur : " + ex.Message);
                    return -1;
                }
            }
        }
    }
    /// <summary>
    /// Représente une commande exécutable avec arguments
    /// Les commandes sont utilisées pour interagir avec les documents et les outils externes
    /// </summary>
    public class DevCommand
    {
        public string Name { get; set; } = "";
        public List<string> Arguments = new List<string>();
        public MemoryStream? Input;
        public MemoryStream? Output;
    }
    /// <summary>
    /// Représente un groupe de commandes exécutables 
    /// Chaque commande est exécutée dans l'ordre, si une commande échoue, les suivantes ne sont pas exécutées
    /// </summary>
    public class DevCommandGroup
    {
        /// <summary>
        /// Liste des commandes en cours
        /// </summary>
        public static Dictionary<string, DevCommandGroup> References = new Dictionary<string, DevCommandGroup>();

        static DevCommandGroup()
        {
        }

        public static DevCommandGroup Create(string name, string desc)
        {
            var o = new DevCommandGroup();
            o.Label = desc;
            References.Add(name, o);

            return o;
        }

        public void Execute()
        {
            Console.WriteLine($"Execute Command group '{Label}'...");
            if (this.IO.Length != 0)
            {
                this.IO.Close();
                this.IO = new MemoryStream();
            }
            foreach (var cmd in Commands)
            {
                try
                {
                    Console.Write($"   Run {cmd.Name} => ");
                    var def = DevCommandDefinition.BuiltIn[cmd.Name];

                    // si une entrée est présente, la copie dans la commande
                    if (this.IO.Length > 0)
                    {
                        cmd.Input = new MemoryStream();
                        this.IO.Position = 0;
                        this.IO.CopyTo(cmd.Input);
                        cmd.Input.SetLength(this.IO.Length);
                    }

                    // prépare la sortie
                    cmd.Output = new MemoryStream();

                    // exécute la commande
                    var result = def.Execute(cmd);
                    if (result == 0)
                        Console.WriteLine("... OK");
                    else
                    {
                        Console.WriteLine($"... Failed with code ({result})");
                        return;
                    }

                    // récupère la sortie
                    if (cmd.Output.Length > 0)
                    {
                        cmd.Output.Position = 0;
                        this.IO.Position = 0;
                        cmd.Output.CopyTo(this.IO);
                        this.IO.SetLength(cmd.Output.Length);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"... Failed with error ({ex.Message})");
                }
            }

            // copie la sortie dans l'objet de destination
            if(String.IsNullOrEmpty(this.Output) == false)
            {
                var handle = DevObject.mutexExecuteObjects.WaitOne();
                if (handle && DevObject.TryGet(this.Output, out var obj))
                {
                    var handle2 = DevObject.mutexExecuteObjects.WaitOne();
                    if (handle2 && obj.Content != null && obj.Content.CanWrite == true)
                    {
                        obj.Content.Position = 0;
                        this.IO.Position = 0;
                        this.IO.CopyTo(obj.Content);
                        obj.Content.SetLength(this.IO.Length);
                        obj.Content.Position = 0;
                        this.IO.Position = 0;
                        DevObject.mutexExecuteObjects.ReleaseMutex();
                    }

                    DevObject.mutexExecuteObjects.ReleaseMutex();
                }
            }

            Console.WriteLine();
        }

        public static void Delete(string name)
        {
            References.Remove(name);
            foreach (var o in DevFacet.References)
            {
                if (o.Value.Commands.ContainsKey(name) == false)
                    continue;
                o.Value.Commands.Remove(name);
            }
        }

        /// <summary>
        /// Libellé du code
        /// </summary>
        public string Label { get; set; } = "";
        /// <summary>
        /// Le script des commandes
        /// </summary>
        /// <remarks>
        /// Est utilisé pour conserver le script original car certaines commandes peuvent ne pas être convertie en objet
        /// </remarks>
        public string Content { get; set; } = "";

        /// <summary>
        /// Flux d'entrée/sortie utilisé par les commandes
        /// </summary>
        public MemoryStream IO = new MemoryStream();

        /// <summary>
        /// Nom de l'objet de sortie, si vide aucun
        /// </summary>
        public string Output = "";

        /// <summary>
        /// Liste des commandes à exécuter
        /// </summary>
        public List<DevCommand> Commands = new List<DevCommand>();

        /// <summary>
        /// Génère un nom unique pour le groupe de commandes
        /// </summary>
        public static string GenerateName()
        {
            int index = 1;
            string name;
            do
            {
                name = $"CommandGroup{index}";
                index++;
            } while (References.ContainsKey(name));
            return name;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (DevCommand command in Commands)
            {
                sb.AppendLine($"{command.Name} " + String.Join(" ", command.Arguments));
            }
            return sb.ToString();
        }

        public static DevCommandGroup FromString(string label, string output, string sb)
        {
            DevCommandGroup group = new DevCommandGroup()
            {
                Content = sb,
                Label = label,
                Output = output,
            };

            group.MakeCommands();

            return group;
        }

        public void Run()
        {

            foreach (var cmd in Commands)
            {
                if (DevCommandDefinition.BuiltIn.TryGetValue(cmd.Name, out var commandDef))
                {
                    try
                    {
                        commandDef.Execute(cmd);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"La commande '{cmd.Name}' a échouée." + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine($"La commande '{cmd.Name}' n'existe pas.");
                }
            }
        }

        internal void MakeCommands()
        {
            Commands.Clear();

            foreach (var line in Content.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                var match = Regex.Match(line, @"^(\w+)(?:\s+(\${0,1}[\w]+))*\s*$");
                if (match.Success)
                {
                    DevCommand command = new DevCommand();
                    command.Name = match.Groups[1].Value;

                    // obtient la définition de la commande (avec les arguments)
                    DevCommandDefinition.BuiltIn.TryGetValue(command.Name, out var commandDef);
                    if (commandDef == null)
                    {
                        Console.WriteLine($"La commande '{command.Name}' n'existe pas.");
                        continue;
                    }

                    for (int i = 0; i < match.Groups[2].Captures.Count; i++)
                    {
                        command.Arguments.Add(match.Groups[2].Captures[i].Value);
                    }

                    Commands.Add(command);
                }
            }
        }

        internal static void Init()
        {
            foreach (var cmd in References)
            {
                cmd.Value.MakeCommands();
            }
        }
    }
}
