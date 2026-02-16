using DevApps.Print;
using Microsoft.Win32;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DevApps.GUI
{
    /// <summary>
    /// Implémente les fonctions de gestion lié à l'interface utilisateur
    /// </summary>
    internal static class GuiService
    {
        internal static ManualResetEvent? ShowWindowEvent;
        internal static ManualResetEvent? CloseWindowEvent;
        internal static DesignerWindow? EditorWindow;
        internal static Thread? WindowThread;
        internal static List<DispatcherOperation> dispatcherOperations = new List<DispatcherOperation>();

        /// <summary>
        /// Liste des commandes d'éditions avec leurs applications associées
        /// </summary>
        internal static Dictionary<string, string> associatedEditors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Liste des applications avec leurs lignes de commandes
        /// </summary>
        internal static Dictionary<string, string> externalsEditors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        internal static Dictionary<string, string> externalsTools = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal static string ExternalToolsPaths
        {
            get
            {
                return string.Join(";", externalsTools.Values);
            }
        }

        internal static string ExternalEditorsPaths
        {
            get
            {
                return string.Join(";", externalsEditors.Values);
            }
        }

        static GuiService()
        {
            InitEditors();
        }

        /// <summary>
        /// Initialise la liste des éditeurs en se basant sur les informations du registre Windows ou en résolvant automatiquement
        /// </summary>
        internal static void InitEditors()
        {
            // charge la liste des editeurs
            LoadEditors();
            LoadTools();

            if (externalsEditors.ContainsKey("cmd") == false)
                externalsEditors.Add("cmd", "cmd");

            // detection
            string[] editors =
            {
                "Typora.exe",
                "notepad.exe",
                "devenv.exe",
                "Code.exe",
                "sublime_text.exe",
                "paint.exe",
                "7zFM.exe",
            };

            ResolveApplicationNames(editors, externalsEditors);

            string[] tools =
            {
                "canvas2pdf.exe",
                "db2erd.exe",
            };

            ResolveApplicationNames(tools, externalsTools);

            if (associatedEditors.ContainsKey("code") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur éditeur de code
                if (externalsEditors.TryGetValue("code", out var name))
                    associatedEditors["code"] = name;
                else if (externalsEditors.TryGetValue("Visual Studio", out name))
                    associatedEditors["code"] = name;
                else if (externalsEditors.TryGetValue("sublime text", out name))
                    associatedEditors["code"] = name;
                else if (externalsEditors.TryGetValue("notepad", out name))
                    associatedEditors["code"] = name;
            }

            if (associatedEditors.ContainsKey("text") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur éditeur de texte
                if (externalsEditors.TryGetValue("notepad", out var name))
                    associatedEditors["text"] = name;
            }

            if (associatedEditors.ContainsKey("cmd") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur terminal de commandes
                if (externalsEditors.TryGetValue("cmd", out var name))
                    associatedEditors["cmd"] = name;
            }

            if (associatedEditors.ContainsKey("image") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur éditeur d'image
                if (externalsEditors.TryGetValue("paint", out var name))
                    associatedEditors["image"] = name;
            }

            if (associatedEditors.ContainsKey("archive") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur archiveur
                if (externalsEditors.TryGetValue("7-Zip", out var name))
                    associatedEditors["archive"] = name;
            }

            if (associatedEditors.ContainsKey("markdown") == false)
            {
                // dans l'ordre, essai de résoudre le meilleur éditeur MarkDown
                if (externalsEditors.TryGetValue("Typora", out var name))
                    associatedEditors["markdown"] = name;
            }
        }

        /// <summary>
        /// Résoud le nom des applications en chemin d'accès vers l'exécutable
        /// </summary>
        /// <param name="editors">Noms à rechercher</param>
        /// <param name="paths">Liste à initialiser</param>
        internal static void ResolveApplicationNames(string[] editors, Dictionary<string,string> paths)
        {
            // possibilité pour l'utilisateur de renseigner plus de mots clés puis choisir les éditeurs à lier aux mots clés

            // Recherche dans les applications installées (Local Machine)
            try
            {
                var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    var displayName = subKey.GetValue("DisplayName") as string;
                                    var displayIcon = subKey.GetValue("DisplayIcon") as string;

                                    if (displayName != null && displayIcon != null && paths.ContainsKey(displayName) == false)
                                    {
                                        if (!string.IsNullOrEmpty(displayIcon) && editors.Contains(Path.GetFileName(displayIcon), StringComparer.OrdinalIgnoreCase))
                                        {
                                            paths.Add(displayName, displayIcon);
                                        }

                                        if (!string.IsNullOrEmpty(displayName) && editors.Count(p => displayName.ToLower().Contains(p.ToLower()) == true) > 0)
                                        {
                                            paths.Add(displayName, displayIcon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Recherche dans les applications installées (Local Machine 64bits)

                registryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                            {
                                if (subKey != null)
                                {
                                    var displayName = subKey.GetValue("DisplayName") as string;
                                    var displayIcon = subKey.GetValue("DisplayIcon") as string;

                                    if (displayName != null && displayIcon != null && paths.ContainsKey(displayName) == false)
                                    {
                                        if (!string.IsNullOrEmpty(displayIcon) && editors.Contains(Path.GetFileName(displayIcon), StringComparer.OrdinalIgnoreCase))
                                        {
                                            paths.Add(displayName, displayIcon);
                                        }

                                        if (!string.IsNullOrEmpty(displayName) && editors.Count(p => displayName.ToLower().Contains(p.ToLower()) == true) > 0)
                                        {
                                            paths.Add(displayName, displayIcon);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Recherche dans les applications enregistrées (ClassesRoot)

                registryKey = @"Applications";

                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            if (editors.Contains(subKeyName, StringComparer.OrdinalIgnoreCase) || editors.Contains(Path.GetFileNameWithoutExtension(subKeyName), StringComparer.OrdinalIgnoreCase))
                            {
                                using (RegistryKey? subKey = key.OpenSubKey(subKeyName + @"\shell\open\command"))
                                {
                                    if (subKey != null)
                                    {
                                        var path = subKey.GetValue("") as string;
                                        var name = subKeyName.Replace(".exe", null, StringComparison.OrdinalIgnoreCase);
                                        if (path != null && paths.ContainsKey(name) == false)
                                        {
                                            paths.Add(name, path);
                                        }
                                    }
                                    else
                                    {
                                        using (RegistryKey? subKey2 = key.OpenSubKey(subKeyName + @"\shell\edit\command"))
                                        {
                                            if (subKey2 != null)
                                            {
                                                var path = subKey2.GetValue("") as string;
                                                var name = subKeyName.Replace(".exe", null, StringComparison.OrdinalIgnoreCase);

                                                if (path != null && paths.ContainsKey(name) == false)
                                                {
                                                    paths.Add(name, path);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        internal static void SaveEditors()
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps";

                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(registryKey))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("Editors", false);

                        using (RegistryKey? subKey = key.CreateSubKey(@"Editors\Apps"))
                        {
                            foreach (var item in externalsEditors)
                            {
                                subKey.SetValue(item.Key, item.Value);
                            }
                        }
                        using (RegistryKey? subKey = key.CreateSubKey(@"Editors\Assoc"))
                        {
                            foreach (var item in associatedEditors)
                            {
                                subKey.SetValue(item.Key, item.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        internal static void SaveTools()
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps";

                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(registryKey))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree("Tools", false);

                        using (RegistryKey? subKey = key.CreateSubKey(@"Tools\Apps"))
                        {
                            foreach (var item in externalsTools)
                            {
                                subKey.SetValue(item.Key, item.Value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        internal static void LoadEditors()
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(@"Editors\Apps"))
                        {
                            if (subKey != null)
                            {
                                externalsEditors.Clear();
                                foreach (var name in subKey.GetValueNames())
                                {
                                    externalsEditors[name] = subKey?.GetValue(name)?.ToString() ?? String.Empty;
                                }
                            }
                        }
                        using (RegistryKey? subKey = key.OpenSubKey(@"Editors\Assoc"))
                        {
                            if (subKey != null)
                            {
                                associatedEditors.Clear();
                                foreach (var name in subKey.GetValueNames())
                                {
                                    associatedEditors[name] = subKey?.GetValue(name)?.ToString() ?? String.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        internal static void LoadTools()
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(@"Tools\Apps"))
                        {
                            if (subKey != null)
                            {
                                externalsTools.Clear();
                                foreach (var name in subKey.GetValueNames())
                                {
                                    externalsTools[name] = subKey?.GetValue(name)?.ToString() ?? String.Empty;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        public static bool IsInitialized { get { return EditorWindow != null; } }

        public static bool IsObjectsView { get { return EditorWindow?.Content is DesignerDataView; } }
        public static bool IsFacetsView { get { return EditorWindow?.Content is DesignerView; } }

        internal static void ThreadStartingPoint()
        {
            try
            {
                EditorWindow = new DesignerWindow();
                EditorWindow.Closed += EditorWindow_Closed;
                EditorWindow.Loaded += EditorWindow_Loaded;
                EditorWindow.Show();
                //Crée le dispatcher permettant aux appels async de revenir sur le thread UI
                System.Windows.Threading.Dispatcher.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                throw;
            }
        }

        internal static void OpenEditor()
        {
            if (EditorWindow == null)
            {
                ShowWindowEvent = new ManualResetEvent(false);
                CloseWindowEvent = new ManualResetEvent(false);
                WindowThread = new Thread(new ThreadStart(ThreadStartingPoint));
                WindowThread.Name = "Window UI";
                WindowThread.SetApartmentState(ApartmentState.STA);
                WindowThread.IsBackground = true;
                WindowThread.Start();
            }
        }

        internal static void WaitDrawOperations()
        {
            foreach (DispatcherOperation operation in dispatcherOperations)
                operation.Wait();
            dispatcherOperations.Clear();
        }

        internal static void SignalWorkerStatusChange()
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    EditorWindow?.WorkerChange();
                })));
        }

        /// <summary>
        /// Invalide le visuel d'un objet dans la vue designer
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        internal static void Invalidate(string name)
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    if (EditorWindow?.Content is DesignerView)
                    {
                        var canvas = ((EditorWindow?.Content as DesignerView)?.MyCanvas);

                        var host = canvas?.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                        if (host != null)
                        {
                            host.InvalidateVisual();
                        }
                    }
                })));
        }

        /// <summary>
        /// Invalide le visuel des états des objets dans la vue des objets
        /// </summary>
        internal static void InvalidateObjectsStatus()
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() =>
                {
                    if (EditorWindow?.Content is DesignerDataView)
                    {
                        var editor = (EditorWindow?.Content as DesignerDataView);
                        if (editor != null)
                        {
                            editor.InvalidateObjectsStatus();
                        }
                    }
                })));
        }

        internal static void SetStatusText(string text)
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    EditorWindow.StatusText = text;
                })));
        }

        internal static string? GetStatusText()
        {
            return EditorWindow?.StatusText;
        }

        internal static void InvalidateFacets()
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    EditorWindow?.InvalidateFacets();
                })));
        }

        internal static Typeface typeface = new Typeface("Verdana");

        internal static void AddShape(Program.DevFacet facet, string name, string? desc, Rect position)
        {
            EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    if (EditorWindow?.Content is DesignerView)
                    {
                        var canvas = ((EditorWindow?.Content as DesignerView)?.MyCanvas);

                        if (canvas != null)
                        {
                            var element = new DrawElement(name, facet, position, desc ?? name, string.Empty);
                            canvas.Children.Add(element);
                        }
                    }
                }));
        }

        internal static void SetRect(string name, Rect rect)
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    if (EditorWindow?.Content is DesignerView)
                    {
                        var canvas = ((EditorWindow?.Content as DesignerView)?.MyCanvas);

                        var host = canvas?.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                        if (host != null)
                        {
                            Canvas.SetLeft(host, rect.Left);
                            Canvas.SetTop(host, rect.Top);
                            host.Width = rect.Width;
                            host.Height = rect.Height;
                            host.InvalidateVisual();
                        }
                    }
                })));
        }

        internal static void SetDescription(string name, string desc)
        {
            if (EditorWindow == null)
                return;

            dispatcherOperations.Add(EditorWindow.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    if (EditorWindow?.Content is DesignerView)
                    {
                        var canvas = ((EditorWindow?.Content as DesignerView)?.MyCanvas);


                        var host = canvas?.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                        if (host != null)
                        {
                            host.Title = new FormattedText(desc, CultureInfo.InvariantCulture,
                                System.Windows.FlowDirection.LeftToRight, typeface, 10, Brushes.Blue,
                                VisualTreeHelper.GetDpi(canvas).PixelsPerDip);
                            host.InvalidateVisual();
                        }
                    }
                })));
        }

        internal static void CloseEditor()
        {
            if (EditorWindow != null)
            {
                if (EditorWindow.Dispatcher.CheckAccess())
                    EditorWindow.Close();
                else
                    EditorWindow.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(EditorWindow.Close));
            }
        }

        internal static void WaitWindowClosed()
        {
            if (CloseWindowEvent != null && WindowThread != null)
            {
                CloseWindowEvent.WaitOne();
                WindowThread.Join();
            }
        }

        private static void EditorWindow_Closed(object? sender, EventArgs e)
        {
            CloseWindowEvent?.Set();
            EditorWindow?.Dispatcher.InvokeShutdown();
        }

        internal static void WaitWindowLoaded()
        {
            if (ShowWindowEvent != null && WindowThread != null)
            {
                ShowWindowEvent.WaitOne();
            }
        }

        private static void EditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowWindowEvent?.Set();
        }

        internal static void OpenExternalEditor(string name, string filename)
        {
            //todo maintenir un event sur changement de fichiers pour chaque objet
            var exePath = String.Empty;

            if (externalsEditors.ContainsKey(name) == true)
            {
                exePath = externalsEditors[name];
            }
            else if(name.Contains("."))
            {
                name = name.Substring(name.IndexOf("."));

                if (externalsEditors.ContainsKey(name) == true)
                {
                    exePath = externalsEditors[name];
                }
            }

            if(String.IsNullOrEmpty(exePath) == false)
            {
                Process process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = filename;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.Start();
            }
        }

        internal static Program.DevFacet? GetSelectedFacet()
        {
            return EditorWindow?.Dispatcher.Invoke(
                DispatcherPriority.Render,
                new Func<Program.DevFacet?>(() => {
                    return (EditorWindow?.Content as DesignerView)?.facette;
                })) as Program.DevFacet;
        }

        internal static bool OpenEditorOrDefault(Stream stream, string? editorKey = null, bool waitForUpdate = true)
        {
            string? fileExt = null;
            string? editorPath = null;

            if (editorKey == null)
            {
                if (ToPDF.IsPNG(stream))
                {
                    editorKey = "image";
                    fileExt = ".png";
                }
                else if (ToPDF.IsBMP(stream))
                {
                    editorKey = "image";
                    fileExt = ".bmp";
                }
                else if (ToPDF.IsJPEG(stream))
                {
                    editorKey = "image";
                    fileExt = ".jpeg";
                }
                else if (ToPDF.IsUTF8(stream))
                {
                    editorKey = "text";
                    fileExt = ".txt";
                }
            }

            // récupère l'éditeur associé
            if (editorKey != null)
            {
                var editor = GuiService.associatedEditors.Where(p => p.Key == editorKey).Select(p => p.Value).FirstOrDefault();
                if (editor != null)
                    editorPath = GuiService.externalsEditors[editor];
                else
                {
                    MessageBox.Show("L'éditeur \"" + editorKey + "\" est introuvable, veuillez spécifier l'éditeur associé à cet objet ou renseigner l'éditeur dans les préférences", "Edition des données", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // exécute l'environnement de commandes
            if (editorPath == null)
            {
                MessageBox.Show("Le type de donnée n'est pas reconnu ou l'éditeur est introuvable, veuillez spécifier l'éditeur associé à cet objet", "Edition des données", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                // si l'éditeur spécifie une extension, l'utiliser pour aider l'éditeur à formater le contenu
                if(fileExt == null && editorKey != null && editorKey.Contains('.'))
                    fileExt = Path.GetExtension(editorKey).ToLowerInvariant();

                // crée un fichier temporaire
                var tmpFile = Path.GetTempFileName() + fileExt;
                var file = File.OpenWrite(tmpFile);
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(file);
                stream.Seek(0, SeekOrigin.Begin);
                file.Close();

                // ouvre l'éditeur
                using System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;//System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C \"" + ((editorPath.Contains("%1") == false) ? editorPath + " \"" + tmpFile + "\"" : editorPath.Replace("%1", tmpFile)) + "\"";
                process.StartInfo = startInfo;
                process.Start();

                if (waitForUpdate)
                {
                    process.WaitForExit();

                    // Si aucune différence, ne rien faire
                    if (stream.IsDifferent(tmpFile) == false)
                        return false;

                    // 
                    if (MessageBox.Show("Voulez vous appliquer les modifications ?", "Edition des données", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        // récupère les données
                        file = File.OpenRead(tmpFile);
                        stream.Seek(0, SeekOrigin.Begin);
                        file.CopyTo(stream);
                        stream.SetLength(file.Length);
                        stream.Seek(0, SeekOrigin.Begin);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }

            return false;
        }
    }
}
