using DevApps.GUI;
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

        internal static void AddShape(string name, Rect position)
        {
            EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.MyCanvas);
     
                    var element = new DrawElement();
                    element.Title = new FormattedText(name, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, Brushes.Blue);
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
    }
}
