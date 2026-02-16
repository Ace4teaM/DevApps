using ComponentAce.Compression.Libs.ZLib;
using DevApps.Print;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerLogView.xaml
    /// </summary>
    public partial class DesignerLogView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class VisualHost : FrameworkElement
        {
            internal DevLog.Block _block;
            private DrawingVisual? _visual;
            private DesignerLogView _parent;
            private DrawingVisual _backgroundVisual = new DrawingVisual();
            internal Brush _backgroundColor = Brushes.White;

            public VisualHost(DevLog.Block block, DesignerLogView parent)
            {
                Unloaded += VisualHost_Unloaded;

                Margin = new Thickness(0, 6, 0, 6);
                
                _block = block;
                _parent = parent;

                Dispatcher.Invoke(() => RebuildVisual(DevLog.DefaultWidth));
            }

            public void UpdateVisual()
            {
                // Détache le visuel
                // important car il est conservé d'une vue à l'autre dans la variable _block.renderTask
                // si il n'est pas détaché une exception sera levé lors de l'appel à AddVisualChild(_block.renderTask.Result)
                if (_visual != null)
                {
                    RemoveVisualChild(_visual);
                    RemoveLogicalChild(_visual);
                    _visual = null;
                }

                _block.renderTask = null;

                InvalidateMeasure();
            }

            private void VisualHost_Unloaded(object sender, RoutedEventArgs e)
            {
                // Détache le visuel
                // important car il est conservé d'une vue à l'autre dans la variable _block.renderTask
                // si il n'est pas détaché une exception sera levé lors de l'appel à AddVisualChild(_block.renderTask.Result)
                if (_visual != null)
                {
                    RemoveVisualChild(_visual);
                    RemoveLogicalChild(_visual);
                    _visual = null;
                }
            }

            protected override int VisualChildrenCount => _visual == null ? 1 : 2;

            protected override Visual GetVisualChild(int index)
            {
                if(index == 0)
                    return _backgroundVisual;
                return _visual;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                double maxWidth = double.IsInfinity(availableSize.Width)
                    ? 10000   // fallback si StackPanel vertical
                    : availableSize.Width;

                Dispatcher.Invoke(() => RebuildVisual(maxWidth));

                Rect bounds = _visual?.ContentBounds ?? Rect.Empty;
                if (bounds.X != 0)
                {
                    bounds.Width += bounds.X;
                    bounds.X = 0;
                }
                if (bounds.Y != 0)
                {
                    bounds.Height += bounds.Y;
                    bounds.Y = 0;
                }

                // Rect bounds = _visual?.Drawing?.Bounds ?? Rect.Empty;
                bounds.Width = DevLog.DefaultWidth;//fixe la largeur

                using (var dc = _backgroundVisual.RenderOpen())
                {
                    dc.DrawRectangle(
                        _backgroundColor,   // background
                        null,                // pas de bordure
                        new Rect(0, 0, bounds.Width, bounds.Height)
                    );
                }

                if (_backgroundVisual.Parent == null)
                {
                    AddVisualChild(_backgroundVisual);
                    AddLogicalChild(_backgroundVisual);
                }

                return new Size(bounds.Width, bounds.Height);
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                return finalSize;
            }

            private void RebuildVisual(double maxWidth)
            {
                DrawingVisual? newVisual = null;

                if (_block.renderTask?.IsCompleted == true && _block.renderTask?.IsFaulted == false && _block.renderTask?.IsCanceled == false)
                {
                    //Réutilise le rendu
                    newVisual = _block.renderTask.Result;
                }
                else
                {
                    if (_block.renderTask?.IsFaulted == true)
                    {
                        //Le rendu a échoué
                    }

                    // la tache de rendu n'est pas encore créée
                    if (_block.makeRender != null && _block.renderTask == null)
                    {
                        //Création de la tache de rendu
                        _block.renderTask = _block.makeRender(_block);
                        _block.renderTask.ContinueWith(t =>
                        {
                            //Fin du rendu
                            if (_block.renderTask?.IsFaulted == true || _block.renderTask?.IsCanceled == true)
                            {
                                //Le rendu a échoué
                                return;
                            }
                            InvalidateMeasure(); // force l'appel à RebuildVisual via MeasureOverride mais cette fois ci avec un _block.renderTask terminé
                        }, _block.tokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion , TaskScheduler.FromCurrentSynchronizationContext());
                    }

                    // la tache de rendu est créée mais en attente d'execution
                    if (_block.renderTask?.Status == TaskStatus.Created)
                    {
                        //Démarrage de la tache de rendu
                        _block.renderTask.Start();
                    }

                    // Dessine juste le texte
                    newVisual = new DrawingVisual();
                    using (DrawingContext dc = newVisual.RenderOpen())
                    {
                        //dessine le texte
                        var ft = new FormattedText(
                            String.IsNullOrWhiteSpace(_block.code) ? _block.text : _block.code,
                            CultureInfo.CurrentUICulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Verdana"),
                            12,
                            Brushes.Black,
                            1.0);

                        ft.MaxTextWidth = DevLog.DefaultWidth;

                        dc.DrawText(ft, new Point(0, 0));
                    }
                }

                if (_visual != null)
                {
                    RemoveVisualChild(_visual);
                    RemoveLogicalChild(_visual);
                }

                _visual = newVisual;

                AddVisualChild(_visual);
                AddLogicalChild(_visual);
            }

            // Permet de forcer un redraw externe
            public void InvalidateDrawing()
            {
                InvalidateMeasure();
            }
        }

        public DesignerLogView()
        {
            InitializeComponent();

            if (DevLog.Current == null)
            {
                try
                {
                    // initialise le journal de développement
                    DevLog.Current = DevLog.ParseContent(File.ReadAllText(Program.JournalFilename));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            if (DevLog.Current != null)
            {
                foreach (var item in DevLog.Current)
                {
                    stackPanel.Children.Add(new VisualHost(item, this));
                }
            }
        }

        private VisualHost? MouseOver(System.Windows.Input.MouseEventArgs e)
        {
            Point point = e.GetPosition(this);

            VisualHost? child = null;

            VisualTreeHelper.HitTest(
                root,
                null, // filtre optionnel
                new HitTestResultCallback(result =>
                {
                    if (result.VisualHit is DrawingVisual element && element.Parent is VisualHost)
                    {
                        child = element.Parent as VisualHost;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue; // continue à chercher
                }),
                new PointHitTestParameters(point)
            );

            return child;
        }

        private void Button_MakeRender_Click(object sender, RoutedEventArgs e)
        {
            var block = overVisualHost?._block;
            if (block != null && block.renderTask?.IsCompleted == true && block.renderTask ?.IsFaulted == false && block.renderTask?.IsCanceled == false)
            {
                try
                {
                    var visual = block.renderTask?.Result!;

                    var bound = visual.ContentBounds;

                    var rtb = new RenderTargetBitmap(
                        (int)bound.Width,
                        (int)bound.Height,
                        96, 96,
                        PixelFormats.Pbgra32);

                    rtb.Render(visual);

                    var wnd = new NewObject();
                    wnd.Owner = Window.GetWindow(this);
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    if (wnd.ShowDialog() == true)
                    {
                        var obj = Program.DevObject.Create(wnd.Value, String.Empty, wnd.Tags);

                        byte[] pngBytes = RenderTargetBitmapToPngBytes(rtb);
                        obj.SetOutput(pngBytes);
                        obj.SetDrawCode("gui.image(out)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static byte[] RenderTargetBitmapToPngBytes(RenderTargetBitmap rtb)
        {
            if (rtb == null) throw new ArgumentNullException(nameof(rtb));

            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();          // encoder PNG
            encoder.Frames.Add(BitmapFrame.Create(rtb));   // ajouter le bitmap
            encoder.Save(stream);                          // écrire dans le MemoryStream
            return stream.ToArray();                       // récupérer le byte[]
        }

        private void Button_MakeContent_Click(object sender, RoutedEventArgs e)
        {
            var block = overVisualHost?._block;
            if (block?.makeContent != null)
            {
                block.contentTask = block.makeContent(block);
                if (block.contentTask?.Status == TaskStatus.Created)
                    block.contentTask?.Start();
                block.contentTask?.ContinueWith(t =>
                {
                    //Fin du rendu
                    if (block.contentTask?.IsFaulted == true || block.contentTask?.IsCanceled == true)
                    {
                        return;
                    }

                    // ajoute la variable au projet ...
                    var stream = block.contentTask.Result!;

                    if (stream != null && stream != Stream.Null)
                    {
                        var wnd = new NewObject();
                        wnd.Owner = Window.GetWindow(this);
                        wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        if (wnd.ShowDialog() == true)
                        {
                            var obj = Program.DevObject.Create(wnd.Value, String.Empty, wnd.Tags);

                            if (ToPDF.IsPNG(stream) || ToPDF.IsBMP(stream) || ToPDF.IsJPEG(stream))
                            {
                                obj.SetDrawCode("gui.image(out)");
                                obj.tags.Add("image");
                            }

                            if (ToPDF.IsSVG(stream))
                            {
                                obj.SetDrawCode("gui.svg(out)");
                                obj.tags.Add("svg");
                            }

                            if (ToPDF.IsUTF8(stream))
                            {
                                obj.SetDrawCode("gui.text(out)");
                                obj.tags.Add("text");
                            }

                            stream.CopyTo(obj.buildStream);
                            stream.Dispose();
                        }
                    }
                }, block.tokenSource.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void Button_CopyText_Click(object sender, RoutedEventArgs e)
        {
            var block = overVisualHost?._block;
            if (block?.text != null)
            {
                Clipboard.SetText(block.text);
            }
        }

        VisualHost? overVisualHost = null;
        private void Grid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var child = MouseOver(e);
            if (child != null)
            {
                if (optionsPopup.PlacementTarget != child)
                {
                    optionsPopup.Placement = PlacementMode.Relative;
                    optionsPopup.PlacementTarget = child;
                    optionsPopup.HorizontalOffset = child.ActualWidth;
                    optionsPopup.VerticalOffset = 0;

                    optionsPopup.IsOpen = true;

                    OnPropertyChanged(nameof(CanRenderCurrentBlock));
                    OnPropertyChanged(nameof(CanExecuteCurrentBlock));
                    OnPropertyChanged(nameof(CanCopyCurrentBlock));
                }
            }
            else
            {
                if (optionsPopup.IsOpen)
                {
                    optionsPopup.IsOpen = false;

                    OnPropertyChanged(nameof(CanRenderCurrentBlock));
                    OnPropertyChanged(nameof(CanExecuteCurrentBlock));
                    OnPropertyChanged(nameof(CanCopyCurrentBlock));
                }
            }

            if (overVisualHost != child)
            {
                if (overVisualHost != null)
                {
                    overVisualHost._backgroundColor = Brushes.White;
                    overVisualHost.InvalidateMeasure();
                }

                overVisualHost = child;

                if (overVisualHost != null)
                {
                    overVisualHost._backgroundColor = Brushes.LightGray;
                    overVisualHost.InvalidateMeasure();
                }
            }
        }

        public bool CanRenderCurrentBlock
        {
            get
            {
                return overVisualHost?._block?.makeRender != null;
            }
        }

        public bool CanExecuteCurrentBlock
        {
            get
            {
                return overVisualHost?._block?.makeContent != null;
            }
        }

        public bool CanCopyCurrentBlock
        {
            get
            {
                return overVisualHost?._block?.text != null;
            }
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (MouseOver(e) == null)
            {
                if (overVisualHost != null)
                {
                    overVisualHost._backgroundColor = Brushes.White;
                    overVisualHost.InvalidateMeasure();
                }

                optionsPopup.IsOpen = false;
            }
        }
    }
}
