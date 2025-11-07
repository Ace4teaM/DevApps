using DevApps.GUI;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
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
    internal static string ExecutablePath = System.AppDomain.CurrentDomain.BaseDirectory;
    internal static string CommonSharedPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Shared");
    internal static readonly string CommonObjPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Objects");
    internal static ScriptEngine pyEngine;
    internal static ScriptScope pyScope;
    internal static Thread MainThread = Thread.CurrentThread;
    internal static Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;

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
            Console.WriteLine("Erreur lors de la sauvegarde du projet.");
            Console.WriteLine(ex.Message);
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
            System.Console.WriteLine(e.ErrorContext.Error.ToString());
        };

        var proj = new Serializer.DevProject();

        serializer.Populate(reader, proj);
    }
    static Program()
    {
        pyEngine = Python.CreateEngine();
        pyScope = pyEngine.CreateScope();
    }

    private static void Main(string[] args)
    {
        // affiche un résumé
        if (args.Length > 0 && Path.Exists(args[0]))
        {
            Environment.CurrentDirectory = args[0];
        }

        // affiche un résumé du projet
        if (args.Contains("-s"))
        {
            LoadProject();
            DevObject.LoadOutput();

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
            System.Console.WriteLine(ex.Message);
        }

        // énumère les commandes disponibles
        DevShellCommand.EnumPrivate();

        // initialise le moteur IronPython
        pyEngine.Runtime.LoadAssembly(typeof(Scriban.Template).Assembly);

        pyEngine.ImportModule("array");

        // on rétablie la sortie standard vers la console
        pyEngine.Runtime.IO.RedirectToConsole();

        var modules = pyEngine.GetModuleFilenames();
        
        var paths = pyEngine.GetSearchPaths().ToArray();


        pyScope.SetVariable("interpreter", DevApps.PythonExtends.Interpreter.Instance);
        pyScope.SetVariable("console", new DevApps.PythonExtends.Console());
        pyScope.SetVariable("requests", new DevApps.PythonExtends.Requests());
        pyScope.SetVariable("types", new DevApps.PythonExtends.NetTypes());

        //pyScope.ImportModule("openai");
        //pyScope.ImportModule("requests");
        pyScope.ImportModule("json");

        // change le chemin par défaut de la bibliothèque
        if (args.Contains("-b"))
        {
            try
            {
                var path = Path.GetFullPath(args[args.FindIndex(p => p == "-b") + 1]);
                CommonSharedPath = path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // ouvre l'éditeur
        if (args.Contains("-w"))
        {
            GuiService.OpenEditor();
            GuiService.WaitWindowLoaded();
        }

        LoadProject();

        GuiService.InvalidateFacets();

        DevObject.CompilObjects();

        DevObject.LoadOutput();

        DevObject.Init();

        DevCommandGroup.Init();

        // Construit les données permanentes
        DevFacet.Get("Model")?.Build();

        // Attend la fermeture de la fenêtre
        GuiService.WaitWindowClosed();

        DevObject.Stop();

        // Sauvegarde les données permanentes
        DevObject.SaveOutput();

        SaveProject();
    }
}