//#define LOAD // sinon SAVE

using GUI;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Utils;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Threading;

internal partial class Program
{
    internal static readonly string DevBranch = "devapps";
    internal static readonly string Filename = "devapps.json";
    internal static readonly string DataDir = ".devapps";
    internal static string CommonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Devapps", "Shared");
    internal static readonly string CommonObjDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Devapps", "Objects");
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
        try
        {
            if (Directory.Exists(DataDir) == false)
                Directory.CreateDirectory(DataDir);
            if (Directory.Exists(Program.CommonDataDir) == false)
                Directory.CreateDirectory(Program.CommonDataDir);
            if (Directory.Exists(Program.CommonObjDir) == false)
                Directory.CreateDirectory(Program.CommonObjDir);
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

        //pyScope.ImportModule("openai");
        //pyScope.ImportModule("requests");
        pyScope.ImportModule("json");

        // change le chemin par défaut de la bibliothèque
        if (args.Contains("-b"))
        {
            try
            {
                var path = Path.GetFullPath(args[args.FindIndex(p => p == "-b") + 1]);
                CommonDataDir = path;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        // ouvre l'éditeur
        if (args.Contains("-w"))
        {
            GUI.Service.OpenEditor();
            GUI.Service.WaitWindowLoaded();
        }

#if LOAD
        LoadProject();

        // Sauvegarde les données permanentes
        DevObject.LoadOutput();
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
#endif

        GUI.Service.InvalidateFacets();

        DevObject.MakeReferences();

        DevObject.LoadOutput();

        DevObject.Init();

        DevObject.Start();

        Thread.Sleep(6000);

        DevObject.Stop();

        // Construit les données permanentes
        DevFacet.Get("Model")?.Build();

        // Attend la fermeture de la fenêtre
        GUI.Service.WaitWindowClosed();

        // Sauvegarde les données permanentes
        DevObject.SaveOutput();

        SaveProject();
    }
}