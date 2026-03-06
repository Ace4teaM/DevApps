using DevApps;
using DevApps.Extends;
using DevApps.GUI;
using DevApps.MCP;
using DevApps.Scripts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using static Program.DevCommandDefinition;

internal partial class Program
{
    internal static string[] Keywords = { "class", "def", "if", "else", "elif", "while", "for", "in", "return", "break", "continue", "try", "except", "finally", "with", "as", "import", "from", "global", "nonlocal", "desc", "name", "out", "gui", "types" };

    internal static readonly string IaProfile = "Profile.txt";
    internal static readonly string IaUserProfile = "Profile.user.txt";
    internal static readonly string DevBranch = "devapps";
    internal static readonly string Filename = "devapps.json";
    internal static readonly string DataDir = ".devapps";
    internal static readonly string LogFile = ".devapps.log";
    internal static readonly string JournalFilename = "devapps.md";
    internal static string ExecutablePath = System.AppDomain.CurrentDomain.BaseDirectory;
    internal static string CommonSharedPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Shared");
    internal static readonly string CommonObjPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Objects");
    internal static Thread MainThread = Thread.CurrentThread;
    internal static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;
    internal static string NamedPipePrefix
    {
        get
        {
            return $"devapps-{Process.GetCurrentProcess().Id}.";
        }
    }

    /// <summary>
    /// Log par défaut pour le projet en cours
    /// </summary>
    internal static ProgramLogger Logger { get; private set; }

    /// <summary>
    /// Moteur de script natif pour les objets du programme
    /// </summary>
    internal static ScriptEngine pythonEngine;
    internal static ScriptScope pythonScope;

    /// <summary>
    /// Autres moteurs de scripts pouvant être utilisés dans DevApps.md
    /// </summary>
    internal static Dictionary<string, ScriptEngine> othersEngine = new();
    internal static Dictionary<string, ExtendedComponent> extendedComponents = new();

    /// <summary>
    /// charge le moteur de script supplémentaire
    /// </summary>
    /// <param name="name">nom du moteur, correspond à la concatenation du nom + "Extends.dll"</param>
    /// <remarks>Le fichier doit se trouver dans le répertoire de l'executable</remarks>
    internal static ScriptEngine LoadEngine(string name)
    {
        if (String.Compare(name, Program.NativeEngine.Name, true) == 0)
        {
            return Program.NativeEngine;
        }

        if (othersEngine.TryGetValue(name.ToLower(), out var engine))
        {
            return engine;
        }

        var path = Path.Combine(ExecutablePath, name + "Extends.dll");
        if (!File.Exists(path))
            throw new Exception($"Impossible de trouver le module d'extension {path}");

        var assembly = Assembly.LoadFrom(path);

        var type = assembly.GetTypes()
            .First(t => typeof(ScriptEngine).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        engine = (ScriptEngine)Activator.CreateInstance(type)!;
        othersEngine[name.ToLower()] = engine;

        return engine;
    }

    /// <summary>
    /// charge le moteur de script supplémentaire
    /// </summary>
    /// <param name="name">nom du moteur, correspond à la concatenation du nom + "Extends.dll"</param>
    /// <remarks>Le fichier doit se trouver dans le répertoire de l'executable</remarks>
    internal static ExtendedComponent GetExtendedComponent(string name)
    {
        // convention de nommage
        name = char.ToUpper(name[0]) + name.Substring(1);

        if (extendedComponents.TryGetValue(name.ToLower(), out var component))
        {
            return component;
        }

        var path = Path.Combine(ExecutablePath, name + "Extends.dll");
        if (!File.Exists(path))
            throw new Exception($"Impossible de trouver le module d'extension {path}");

        var assembly = Assembly.LoadFrom(path);

        var type = assembly.GetTypes()
            .First(t => typeof(ExtendedComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        component = (ExtendedComponent)Activator.CreateInstance(type)!;
        extendedComponents[name.ToLower()] = component;

        return component;
    }

    internal static ScriptEngine NativeEngine
    {
        get
        {
            return pythonEngine;
        }
    }

    internal static ScriptScope NativeScope
    {
        get
        {
            return pythonScope;
        }
    }

    internal static ScriptScope GetGlobalScope(ScriptEngine engine)
    {
        return pythonScope;
    }

    static Program()
    {
    }

    public class DevFunction
    {

    }

    public static class DevLibraryUI
    {
        public static string GetText() { return "Hello"; }
    }

    internal static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString.EnumerateRunes())
        {
            var unicodeCategory = Rune.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Sauvegarde le projet courant.
    /// </summary>
    /// <remarks>Pour sécuriser la sauvegarde (éviter un crash après avoir tronqué le fichier), un fichier temporaire est construit puis la destination remplacée par copie de fichier</remarks>
    internal static void SaveProject()
    {
        try
        {
            var tmpFilename = Path.GetTempFileName();

            using (TextWriter writer = new StreamWriter(tmpFilename))
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                serializer.Serialize(writer, new Serializer.DevProject());
            }

            File.Move(tmpFilename, Filename, true);
        }
        catch (Exception ex)
        {
            Program.Logger.WriteLine("Erreur lors de la sauvegarde du projet.");
            Program.Logger.WriteLine(ex.Message);
        }
    }

    internal static void LoadProject()
    {
        if(File.Exists(Filename) == false)
        {
            return;
        }

        using StreamReader reader = new StreamReader(Filename);

        JsonSerializer serializer = JsonSerializer.CreateDefault();
        serializer.Error += (sender, e) =>
        {
            Program.Logger.WriteLine(e.ErrorContext.Error.ToString());
        };

        var proj = new Serializer.DevProject();

        serializer.Populate(reader, proj);
    }

    private static void Main(string[] args)
    {
        // définit le répertoire de travail
        if (args.Length > 0 && Path.Exists(args[0]))
        {
            Environment.CurrentDirectory = args[0];
        }

        // définit le répertoire de travail
        if (args.Contains("-d"))
        {
            var index = Array.FindIndex(args, p => p == "-d");
            if (index != -1 && args.Length > index+1 && args[index + 1].StartsWith("-") == false && Path.Exists(args[index + 1]))
            {
                Environment.CurrentDirectory = Path.GetFullPath(args[index + 1]);
            }
        }

        // initialise le logs
        Logger = new ProgramLogger(Program.LogFile);

        // affiche un résumé du projet
        if (args.Contains("-s"))
        {
            LoadProject();
            DevObject.LoadOutputs();

            DevApps.Print.Services.PrintAll();

            return;
        }

        // initialise les répertoires (si besoin)
        try
        {
            if (Directory.Exists(DataDir) == false)
                Directory.CreateDirectory(DataDir);
            if (Directory.Exists(Program.CommonSharedPath) == false)
                Directory.CreateDirectory(Program.CommonSharedPath);
            if (Directory.Exists(Program.CommonObjPath) == false)
                Directory.CreateDirectory(Program.CommonObjPath);
        }
        catch (Exception ex)
        {
            Program.Logger.WriteLine(ex.Message);
        }

        // énumère les commandes disponibles
        DevShellCommand.EnumPrivate();

        // initialise les moteurs de scripts
        DevApps.Scripts.PythonExtends.Engine.Initialize(out pythonEngine, out pythonScope);

        // change le chemin par défaut de la bibliothèque
        if (args.Contains("-b"))
        {
            try
            {
                var path = Path.GetFullPath(args[Array.FindIndex(args, p => p == "-b") + 1]);
                CommonSharedPath = path;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }
        else
        {
            var path = SharedServices.GetRegisterSharedPath();
            if (path != null)
                CommonSharedPath = path;
        }

        // extensions built-in
        extendedComponents["Javascript"] = new DevApps.Extends.JavaScriptComponent();
        extendedComponents["Mermaid"] = new DevApps.Extends.MermaidComponent();

        // ouvre l'éditeur
        if (args.Contains("-w"))
        {
            GuiService.OpenEditor();
            GuiService.WaitWindowLoaded();
        }

        // execute le connecteur d'IA (serveur MCP)
        if (args.Contains("-i"))
        {
            MCPService.Start();
        }

        LoadProject();

        GuiService.InvalidateFacets();

        DevObject.CompilObjects();

        DevObject.LoadOutputs();

        DevObject.Init();

        DevCommandGroup.Init();

        // Construit les données permanentes
        // todo : a supprimer ?
        DevFacet.Get("Model")?.Build();

        // todo annuler les taches en cours dans DevLog.Current

        // Attend la fermeture de la fenêtre
        GuiService.WaitWindowClosed();

        // Attend la fin du serveur MCP
        MCPService.Stop();

        DevObject.Stop();

        // Sauvegarde les données permanentes
        DevObject.SaveOutput();

        SaveProject();
    }
}