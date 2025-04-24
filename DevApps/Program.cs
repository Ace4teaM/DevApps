#if DEBUG
#define CREATE
#else
#define LOAD
#endif

using DevApps;
using DevApps.GUI;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Threading;
using static IronPython.Modules.PythonWeakRef;

internal partial class Program
{
    internal static string[] Keywords = { "class", "def", "if", "else", "elif", "while", "for", "in", "return", "break", "continue", "try", "except", "finally", "with", "as", "import", "from", "global", "nonlocal", "desc", "name", "out", "gui", "types" };

    internal static readonly string DevBranch = "devapps";
    internal static readonly string Filename = "devapps.json";
    internal static readonly string DataDir = ".devapps";
    internal static string CommonDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Devapps", "Shared");
    internal static readonly string CommonObjPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Devapps", "Objects");
    internal static ScriptEngine pyEngine = null;
    internal static ScriptRuntime pyRuntime = null;
    internal static ScriptScope pyScope = null;
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

    private static void SaveProject()
    {
        using TextWriter writer = new StreamWriter(Filename);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

        serializer.Serialize(writer, new Serializer.DevProject());
    }

    private static void LoadProject()
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

    private static void Main(string[] args)
    {
        // affiche un résumé
        if (args.Length > 0 && Path.Exists(args[0]))
        {
            Environment.CurrentDirectory = args[0];
        }

        // affiche un résumé
        if (args.Contains("-s"))
        {
            LoadProject();
            DevObject.LoadOutput();
            var pdf = ToPDF.Make();
            var tmpFile = Path.GetTempFileName()+".pdf";
            using var file = File.OpenWrite(tmpFile);
            pdf.CopyTo(file);

            Process.Start(new ProcessStartInfo(tmpFile) { UseShellExecute = true });

            return;
        }

        try
        {
            if (Directory.Exists(DataDir) == false)
                Directory.CreateDirectory(DataDir);
            if (Directory.Exists(Program.CommonDataPath) == false)
                Directory.CreateDirectory(Program.CommonDataPath);
            if (Directory.Exists(Program.CommonObjPath) == false)
                Directory.CreateDirectory(Program.CommonObjPath);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }

        pyEngine = Python.CreateEngine();
        pyScope = pyEngine.CreateScope();

        pyEngine.ImportModule("array");

        // on rétablie la sortie standard vers la console
        pyEngine.Runtime.IO.RedirectToConsole();

        var modules = pyEngine.GetModuleFilenames();
        
        var paths = pyEngine.GetSearchPaths().ToArray();


        pyScope.SetVariable("console", new DevApps.PythonExtends.Console());
        pyScope.SetVariable("requests", new DevApps.PythonExtends.Requests());
        pyScope.SetVariable("editor", new DevApps.PythonExtends.CSEditor(""));
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
                CommonDataPath = path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // ouvre l'éditeur
        if (args.Contains("-w"))
        {
            Service.OpenEditor();
            Service.WaitWindowLoaded();
        }

        LoadProject();/*
#if LOAD
        LoadProject();
#else
        if (Directory.GetCurrentDirectory().EndsWith("ERD"))
            DevApps.Samples.ERD.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("InputCheck"))
            DevApps.Samples.InputCheck.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("CodeTemplate"))
            DevApps.Samples.CodeTemplate.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("SocketExchange"))
            DevApps.Samples.SocketExchange.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("UI"))
            DevApps.Samples.UI.Create();
#endif*/

        Service.InvalidateFacets();

        DevObject.CompilObjects();

        DevObject.LoadOutput();

        DevObject.Init();

        DevObject.Start();

        Thread.Sleep(6000);

        DevObject.Stop();

        // Construit les données permanentes
        DevFacet.Get("Model")?.Build();

        // Attend la fermeture de la fenêtre
        Service.WaitWindowClosed();

        // Sauvegarde les données permanentes
        DevObject.SaveOutput();

        SaveProject();
    }
}