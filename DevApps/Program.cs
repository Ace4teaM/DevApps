//#define LOAD // sinon SAVE
//#define Sample_CodeGen
//#define Sample_InputCheck
#define Sample_UI

using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Threading;
using static System.Net.Mime.MediaTypeNames;

internal partial class Program
{
    internal static readonly string DevBranch = "devapps";
    internal static readonly string Filename = "devapps.json";
    internal static readonly string DataDir = ".devapps";
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

        JsonSerializer serializer = JsonSerializer.CreateDefault();

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
#if Sample_CodeGen
        Directory.SetCurrentDirectory(@"CodeGen");
#elif Sample_InputCheck
        Directory.SetCurrentDirectory(@"InputCheck");
#elif Sample_UI
        Directory.SetCurrentDirectory(@"UI");
#endif
        try
        {
            if (Directory.Exists(DataDir) == false)
                Directory.CreateDirectory(DataDir);
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
#else
#if Sample_CodeGen
        DevApps.Samples.CodeGen.Create();
#elif Sample_InputCheck
        DevApps.Samples.InputCheck.Create();
#elif Sample_UI
        DevApps.Samples.UI.Create();
#endif
#endif

        if (GUI.Service.IsInitialized)
        {
            foreach (var o in DevObject.References)
            {
                GUI.Service.AddShape(o.Key);
            }
        }

        DevObject.MakeReferences();

        DevObject.Init();

        DevObject.Start();

        Thread.Sleep(6000);

        DevObject.Stop();

        // Construit les données permanentes
        DevFacet.Get("Model")?.Build();

        // Sauvegarde les données permanentes
        DevObject.SaveOutput();

        // Attend la fermeture de la fenêtre
        GUI.Service.WaitWindowClosed();

#if !LOAD
        SaveProject();
#endif

    }
}