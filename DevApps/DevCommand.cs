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
        public abstract Func<DevCommand, int> Execute { get; }

        public static Dictionary<string, DevCommandDefinition> BuiltIn = new Dictionary<string, DevCommandDefinition>(){
            { "print", new DevPrintCommand() },
            { "build", new DevBuildCommand() },
            { "read", new DevReadCommand() },
            { "write", new DevWriteCommand() },
        };

        private static bool ParseString(ref string argument)
        {
            return true;
        }

        public class DevPrintCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Print"; } }
            public override Func<DevCommand, int> Execute { get { return Print; } }
            private static int Print(DevCommand cmd)
            {
                if (cmd.Arguments.Count < 1)
                    throw new ArgumentException();

                var message = cmd.Arguments[0]; // todo : validation format
                ParseString(ref message);

                Console.WriteLine(message);
                return 0;
            }
        }

        public class DevBuildCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Build object content"; } }
            public override Func<DevCommand, int> Execute { get { return Build; } }
            private static int Build(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                if (cmd.Arguments.Count < 1)
                    throw new ArgumentException();

                var name = cmd.Arguments[0]; // todo : validation format

                var obj = DevObject.Get(name);

                if (obj == null)
                    throw new Exception(@"L'objet {name} n'existe pas");

                DevObject.Build(new Dictionary<string, DevObject> { { name, obj } });

                var currentView = DevApps.GUI.Service.EditorWindow?.Content as DesignerDataView;

                if (currentView != null)
                {
                    currentView.InvalidateObjects();
                }

                return 0;
            }
        }

        public class DevWriteCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Write object content to file"; } }
            public override Func<DevCommand, int> Execute { get { return Run; } }
            private static int Run(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                if (cmd.Arguments.Count < 2)
                    throw new ArgumentException();

                var name = cmd.Arguments[0]; // todo : validation format
                var filename = cmd.Arguments[1]; // todo : validation format

                var obj = DevObject.Get(name);

                if (obj == null)
                    throw new Exception(@"L'objet {name} n'existe pas");

                if (obj.buildStream == null)
                    throw new Exception(@"L'objet {name} ne contient pas de données");

                var file = File.OpenWrite(filename);

                if (file == null)
                    throw new Exception(@"Le fichier {filename} ne peut pas être ouvert");

                obj.buildStream.CopyTo(file);

                obj.buildStream.Position = 0;

                file.Close();

                return 0;
            }
        }

        public class DevReadCommand : DevCommandDefinition
        {
            public override string? Description { get { return "Read object content to file"; } }
            public override Func<DevCommand, int> Execute { get { return Run; } }
            private static int Run(DevCommand cmd)
            {
                if (cmd.Arguments == null)
                    throw new ArgumentException();

                if (cmd.Arguments.Count < 2)
                    throw new ArgumentException();

                var name = cmd.Arguments[0]; // todo : validation format
                var filename = cmd.Arguments[1]; // todo : validation format

                var obj = DevObject.Get(name);

                if (obj == null)
                    throw new Exception(@"L'objet {name} n'existe pas");

                var file = File.OpenRead(filename);

                if (file == null)
                    throw new Exception(@"Le fichier {filename} ne peut pas être ouvert");

                if (obj.buildStream == null)
                    obj.buildStream = new MemoryStream((int)file.Length);

                obj.buildStream.Position = 0;

                file.CopyTo(obj.buildStream);

                obj.buildStream.Position = 0;
                obj.buildStream.SetLength(file.Length);

                file.Close();

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
            internal required string command;
            public override string Description { get { return description; } }
            public string Command { get { return command; } }
            public override Func<DevCommand, int> Execute { get { return RunShell; } }
            public static int RunShell(DevCommand cmd)
            {
                try
                {
                    if (!(DevCommandDefinition.BuiltIn.ContainsKey(cmd.Name) && DevCommandDefinition.BuiltIn[cmd.Name] is DevShellCommand))
                        throw new Exception($"La commande '{cmd.Name}' n'existe pas.");

                    var def = (DevShellCommand)DevCommandDefinition.BuiltIn[cmd.Name];

                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/C " + String.Format(def.Command, cmd.Arguments.ToArray());
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;

                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    Console.WriteLine(result);
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
            foreach (var cmd in Commands)
            {
                try
                {
                    Console.Write($"   Run {cmd.Name} => ");
                    var def = DevCommandDefinition.BuiltIn[cmd.Name];
                    var result = def.Execute(cmd);
                    if(result == 0)
                        Console.WriteLine();
                    else
                        Console.WriteLine($"... Failed with code ({result})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"... Failed with error ({ex.Message})");
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
            sb.AppendLine($"# {Label}");
            foreach (DevCommand command in Commands)
            {
                sb.AppendLine($"{command.Name} " + String.Join(" ", command.Arguments));
            }
            return sb.ToString();
        }

        public static DevCommandGroup FromString(string sb)
        {
            DevCommandGroup group = new DevCommandGroup();
            group.Content = sb;

            foreach (var line in sb.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;

                // label ?
                if(line.StartsWith("#") && String.IsNullOrWhiteSpace(group.Label))
                {
                    group.Label = line.Substring(1).Trim();
                }
                // commande ?
                else{
                    var match = Regex.Match(line, @"^(\w+)(?:\s+(\w+))*$");
                    if (match != null)
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

                        for (int i=0; i< match.Groups[2].Captures.Count; i++)
                        {
                            command.Arguments.Add(match.Groups[2].Captures[i].Value);
                        }
                        group.Commands.Add(command);
                    }
                }
            }
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
    }
}
