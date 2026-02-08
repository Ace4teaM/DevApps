using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DevApps.GUI
{
    /// <summary>
    /// Logique d'interaction pour DesignerLogView.xaml
    /// </summary>
    public partial class DesignerLogView : UserControl
    {
        private void Button_MakeRender_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_MakeVariable_Click(object sender, RoutedEventArgs e)
        {

        }

        public class VisualHost : FrameworkElement
        {
            private DevLog.Block _block;
            private DrawingVisual? _visual;
            private DesignerLogView _parent;

            public VisualHost(DevLog.Block block, DesignerLogView parent)
            {
                MouseMove += VisualHost_MouseMove;
                MouseLeave += VisualHost_MouseLeave;
                MouseEnter += VisualHost_MouseEnter;
                Unloaded += VisualHost_Unloaded;

                Margin = new Thickness(0, 6, 0, 6);
                
                _block = block;
                _parent = parent;

                Dispatcher.Invoke(() => RebuildVisual(DevLog.DefaultWidth));
            }

            private void VisualHost_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
            {
                _parent.optionsPopup.PlacementTarget = this;
                _parent.optionsPopup.IsOpen = true;
            }

            private void VisualHost_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            {
                _parent.optionsPopup.IsOpen = false;
            }

            private void VisualHost_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
            {
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
                }
            }

            protected override int VisualChildrenCount => _visual == null ? 0 : 1;

            protected override Visual GetVisualChild(int index)
            {
                return _visual;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                double maxWidth = double.IsInfinity(availableSize.Width)
                    ? 10000   // fallback si StackPanel vertical
                    : availableSize.Width;

                Dispatcher.Invoke(() => RebuildVisual(maxWidth));

                Rect bounds = _visual?.Drawing?.Bounds ?? Rect.Empty;
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
                        dc.DrawRectangle(Brushes.LightBlue, new Pen(Brushes.DarkTurquoise, 2), new Rect(0, 0, 10, 10));
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
                // initialise le journal de développement
                DevLog.Current = DevLog.ParseContent(File.ReadAllText(Program.JournalFilename));
            }

            foreach (var item in DevLog.Current)
            {
                stackPanel.Children.Add(new VisualHost(item, this));
            }
        }
    }
}
