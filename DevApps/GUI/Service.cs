using DevApps.GUI;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace GUI
{
    internal static class Service
    {
        internal static ManualResetEvent? ShowWindowEvent;
        internal static ManualResetEvent? CloseWindowEvent;
        internal static Window? EditorWindow;
        internal static Thread? WindowThread;
        internal static DesignerView? WindowContent;
        internal static List<DispatcherOperation> dispatcherOperations = new List<DispatcherOperation>();

        /// <summary>
        /// Liste commandes d'éditions avec leurs applications associées
        /// </summary>
        internal static Dictionary<string, string> associatedEditors = new Dictionary<string, string>();

        /// <summary>
        /// Liste des applications avec leurs lignes de commandes
        /// </summary>
        internal static Dictionary<string, string> externalsEditors = new Dictionary<string, string>();

        static Service()
        {
            // charge la liste des editeurs
            LoadEditors();

            // detection
            if (externalsEditors.Count == 0)
            {
                string[] editors =
                {
                    "Typora.exe",
                    "notepad.exe",
                    "devenv.exe",
                    "Code.exe",
                    "sublime_text.exe",
                    "cmd.exe",
                    "paint.exe",
                    "7zFM.exe",
                };

                ResolveExternalEditors(editors);
            }

            if (associatedEditors.Count == 0)
            {
                string? name;

                if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("Code", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["code"] = name;
                else if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("Visual Studio", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["code"] = name;
                else if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("sublime text", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["code"] = name;
                else if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("notepad", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["code"] = name;

                if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("cmd", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["cmd"] = name;

                if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("paint", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["image"] = name;

                if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("7-Zip", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["archive"] = name;

                if ((name = externalsEditors.Keys.FirstOrDefault(p => p.Contains("Typora", StringComparison.InvariantCultureIgnoreCase))) != null)
                    associatedEditors["markdown"] = name;
            }
        }

        internal static void ResolveExternalEditors(string[] editors)
        {
            // possibilité pour l'utilisateur de renseigner plus de mots clés puis choisir les éditeurs à lier aux mots clés

            try
            {
                var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                string displayName = subKey.GetValue("DisplayName") as string;
                                string displayIcon = subKey.GetValue("DisplayIcon") as string;

                                if (!string.IsNullOrEmpty(displayIcon) && editors.Contains(Path.GetFileName(displayIcon)))
                                {
                                    externalsEditors.Add(displayName, displayIcon);
                                }
                            }
                        }
                    }
                }

                registryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                string displayName = subKey.GetValue("DisplayName") as string;
                                string displayIcon = subKey.GetValue("DisplayIcon") as string;

                                if (!string.IsNullOrEmpty(displayIcon) && editors.Contains(Path.GetFileName(displayIcon)))
                                {
                                    externalsEditors.Add(displayName, displayIcon);
                                }
                            }
                        }
                    }
                }

                registryKey = @"Applications";

                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            if (editors.Contains(subKeyName))
                            {
                                using (RegistryKey? subKey = key.OpenSubKey(subKeyName + @"\shell\open\command"))
                                {
                                    if (subKey != null)
                                    {
                                        var path = subKey.GetValue("") as string;

                                        if (path != null)
                                        {
                                            externalsEditors.Add(subKeyName.Replace(".exe",null), path);
                                            Console.WriteLine($"Path : {path}");
                                            Console.WriteLine();
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

        public static bool IsInitialized { get { return EditorWindow != null; } }

        internal static void ThreadStartingPoint()
        {
            try
            {
                EditorWindow = new Window();
                WindowContent = new DesignerView();
                EditorWindow.Content = WindowContent;
                EditorWindow.Closed += EditorWindow_Closed;
                EditorWindow.Loaded += EditorWindow_Loaded;
                EditorWindow.Show();
                System.Windows.Threading.Dispatcher.Run();
            }
            catch (Exception ex)
            {
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

        internal static void Invalidate(string name, MemoryStream output)
        {
            dispatcherOperations.Add(EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.MyCanvas);

                    var host = canvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                    if (host != null)
                    {
                        host.InvalidateVisual();
                    }
                })));
        }

        static double X = 10;
        static double Y = 10;

        internal static Rect GenerateNextPosition(double width, double height)
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

        internal static Typeface typeface = new Typeface("Verdana");

        internal static void AddShape(string name, string? desc, Rect position)
        {
            EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.MyCanvas);
     
                    var element = new DrawElement();
                    element.Title = new FormattedText(desc ?? name, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Blue);
                    element.Name = name;
                    element.Width = position.Width;
                    element.Height = position.Height;
                    Canvas.SetLeft(element, position.Left);
                    Canvas.SetTop(element, position.Top);
                    canvas.Children.Add(element);
                }));
        }

        internal static void SetRect(string name, Rect rect)
        {
            dispatcherOperations.Add(EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.MyCanvas);

                    var host = canvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                    if (host != null)
                    {
                        Canvas.SetLeft(host, rect.Left);
                        Canvas.SetTop(host, rect.Top);
                        host.Width = rect.Width;
                        host.Height = rect.Height;
                        host.InvalidateVisual();
                    }
                })));
        }

        internal static void SetDescription(string name, string desc)
        {
            dispatcherOperations.Add(EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.MyCanvas);

                    var host = canvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                    if (host != null)
                    {
                        host.Title = new FormattedText(desc, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Blue);
                        host.InvalidateVisual();
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

    }
}
