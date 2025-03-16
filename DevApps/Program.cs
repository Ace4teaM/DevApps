//#define LOAD // sinon SAVE

using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Threading;

internal partial class Program
{
    internal static readonly string DevBranch = "devapps";
    internal static readonly string Filename = "devapps.json";
    internal static readonly string DataDir = ".devapps";
    internal static readonly string CommonDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Devapps", "Data");
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
        if (Directory.GetCurrentDirectory().EndsWith("CodeGen"))
            DevApps.Samples.CodeGen.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("InputCheck"))
            DevApps.Samples.InputCheck.Create();
        else if (Directory.GetCurrentDirectory().EndsWith("UI"))
            DevApps.Samples.UI.Create();
#endif

        if (GUI.Service.IsInitialized)
        {
            foreach (var o in DevObject.References)
            {
                GUI.Service.AddShape(o.Key, o.Value.Description, o.Value.GetZone());
            }
        }

        DevObject.MakeReferences();

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