using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Program;

namespace DevApps.GUI
{
    public class DrawElement : DrawBase
    {
        internal FormattedText? Title;
        internal DevFacet? facet;

        internal DrawElement(DevFacet facet)
        {
            this.facet = facet;
        }

        public System.Windows.Media.Brush? background = null;

        internal void RunAction(Point position)
        {
            Program.DevObject.mutexCheckObjectList.WaitOne();
            Program.DevObject.References.TryGetValue(this.Name, out var reference);
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();
            
            if (reference != null)
            {
                if (String.IsNullOrEmpty(reference.GetUserAction()) == false)
                {
                    reference.mutexReadOutput.WaitOne();

                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream, Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                    pyScope.SetVariable("gui", reference.gui);
                    pyScope.SetVariable("name", this.Name);
                    pyScope.SetVariable("desc", reference.Description);
                    foreach (var pointer in reference.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                    }

                    reference.UserAction.Item2?.Execute(pyScope);

                    reference.mutexReadOutput.ReleaseMutex();

                    this.InvalidateVisual();
                }
            }
        }

        internal static Typeface typeface = new Typeface("Arial");
        internal static Pen connectorPen = new Pen(Brushes.Linen, 3);

        // Cette méthode gère le rendu
        protected override void OnRender(DrawingContext drawingContext)
        {
            var canvas = this.Parent as Canvas;

            base.OnRender(drawingContext);

            if(Title != null)
                drawingContext.DrawText(Title, new Point(0, -Title.Height - 6));

            Program.DevObject.mutexCheckObjectList.WaitOne();
            Program.DevObject.References.TryGetValue(this.Name, out var reference);
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();

            if (reference != null)
            {
                var ContentWidth = this.ActualWidth;
                var ContentHeight = this.ActualHeight;

                var bg = facet.Objects[this.Name].background;
                if (bg != null && background == null)
                    background = (Brush?)(new BrushConverter().ConvertFromString(bg)) ?? System.Windows.Media.Brushes.Transparent;

                // Dessiner un rectangle pour illustrer
                Rect rect = new Rect(0, 0, ContentWidth, ContentHeight);
                drawingContext.DrawRectangle(background, null, rect);
                if (reference.DrawCode.Item2 != null)
                {
                    reference.mutexReadOutput.WaitOne();

                    facet.Objects[this.Name].SetZone(new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), ContentWidth, ContentHeight));
                    reference.gui.baseZone = new DevApps.PythonExtends.Zone { Rect = rect };

                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream, Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                    pyScope.SetVariable("gui", reference.gui);
                    pyScope.SetVariable("name", this.Name);
                    pyScope.SetVariable("desc", reference.Description);

                    foreach (var pointer in reference.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                    }

                    reference.gui.Begin(drawingContext);
                    reference.DrawCode.Item2?.Execute(pyScope);
                    reference.gui.End();
                    reference.mutexReadOutput.ReleaseMutex();
                }
            }
        }
    }
}
