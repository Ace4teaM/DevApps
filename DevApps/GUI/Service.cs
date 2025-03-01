using DevApps.GUI;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using static IronPython.Modules._ast;
using static System.Net.Mime.MediaTypeNames;

namespace GUI
{
    public class DrawElement : FrameworkElement
    {
        // Cette méthode gère le rendu
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Program.DevObject.mutexCheckObjectList.WaitOne();
            Program.DevObject.References.TryGetValue(this.Name, out var reference);
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();

            if(reference != null)
            {
                var ContentWidth = this.ActualWidth;
                var ContentHeight = this.ActualHeight;

                // Dessiner un rectangle pour illustrer
                Rect rect = new Rect(0, 0, ContentWidth, ContentHeight);
                drawingContext.DrawRectangle(/*reference.gui.GetBackground()*/Brushes.LightGray, null, rect);
                if (reference.DrawCode.Item2 != null)
                {
                    reference.mutexReadOutput.WaitOne();

                    reference.gui.baseZone = new DevApps.PythonExtends.Zone { Rect = new Rect(0, 0, ContentWidth, ContentHeight) };

                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream));// mise en cache dans l'objet ?
                    pyScope.SetVariable("gui", reference.gui);
                    pyScope.SetVariable("name", this.Name);
                    pyScope.SetVariable("desc", reference.Description);
                    foreach (var pointer in reference.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream :  new MemoryStream()));// mise en cache dans l'objet ?
                    }

                    reference.gui.Begin(drawingContext);
                    reference.DrawCode.Item2?.Execute(pyScope);
                    reference.gui.End();
                    reference.mutexReadOutput.ReleaseMutex();
                }
            }
        }
    }

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
                    var canvas = (WindowContent?.Content as Canvas);

                    var host = canvas.Children.OfType<DrawElement>().FirstOrDefault(p => p.Name == name);
                    if (host != null)
                    {
                        host.InvalidateVisual();
                    }
                })));

        }

        /*internal static void Draw(string name, Func<DrawingContext,bool> func)
        {
            dispatcherOperations.Add(EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.Content as Canvas);

                    var host = canvas.Children.OfType<MyVisualHost>().FirstOrDefault(p => p.Name == name);
                    if (host != null)
                    {
                        var drawContext = host.drawingVisual.RenderOpen();
                        func.Invoke(drawContext);
                        drawContext.Close();
                        host.InvalidateVisual();
                        host.InvalidateMeasure();
                    }

        }*/

        static double X = 10;
        static double Y = 10;
        internal static void AddShape(string name)
        {
            EditorWindow?.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                new Action(() => {
                    var canvas = (WindowContent?.Content as Canvas);

                    var element = new DrawElement();
                    element.Name = name;
                    element.Width = 100;
                    element.Height = 100;
                    Canvas.SetLeft(element, X);
                    Canvas.SetTop(element, Y);
                    X += element.ActualWidth + 10;
                    if(X > 500)
                    {
                        X = 10;
                        Y += element.ActualHeight + 10;
                    }
                    canvas.Children.Add(element);
                    

                    /*
                    var host = new MyVisualHost();
                    host.Name = name;
                    Canvas.SetTop(host, Y);
                    Y += host.Height + 10;
                    //host.CreateDrawingVisualRectangle();

                    canvas.Children.Add(host);*/

                    // render the visual on a bitmap
                    /*var bmp = new RenderTargetBitmap(
                        pixelWidth: (int)200,
                        pixelHeight: (int)200,
                        dpiX: 0, dpiY: 0, pixelFormat: PixelFormats.Pbgra32);
                    //bmp.Render(drawingVisual);

                    // create a new Image to display the bitmap, then add it to the canvas
                    Image image = new Image();
                    image.Source = bmp;
                    image.Name = name;
                    image.Width = 200;
                    image.Height = 200;
                    image.Visibility = Visibility.Visible;
                    Canvas.SetTop(image, Y);
                    Y += image.Height + 10;

                    canvas.Children.Add(image);*/
                }));
        }

        // Fonction qui crée un DrawingContext et dessine un rectangle
        private static void Draw(DrawingVisual drawingVisual)
        {
            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                // Dessiner un rectangle
                dc.DrawRectangle(Brushes.Blue, null, new Rect(50, 50, 200, 100));
                // Dessiner une ligne
                dc.DrawLine(new Pen(Brushes.Red, 2), new Point(50, 50), new Point(250, 150));
            }

            // Rafraîchir le Canvas pour afficher les nouveaux dessins
            //drawingVisual.InvalidateVisual();
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
