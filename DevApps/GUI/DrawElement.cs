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
        internal FormattedText? SubTitle;
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
                    pyScope.SetVariable("editor", reference.Editor);
                    foreach (var pointer in reference.Pointers)
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
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

            Program.DevObject.mutexCheckObjectList.WaitOne();
            Program.DevObject.References.TryGetValue(this.Name, out var reference);
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();

            if (facet != null && reference != null)
            {
                var ContentWidth = this.ActualWidth;
                var ContentHeight = this.ActualHeight;
                var DrawProp = facet.Objects[this.Name];

                if (Title != null)
                {
                    switch(DrawProp.title)
                    {
                        case DevFacet.TitlePlacement.TopLeft:
                            drawingContext.PushTransform(new TranslateTransform(0, -Title.Height - 6));
                            drawingContext.DrawText(Title, new Point(0, 0));
                            drawingContext.Pop();
                            break;
                        case DevFacet.TitlePlacement.TopRight:
                            drawingContext.PushTransform(new TranslateTransform(Width-Title.Width, -Title.Height - 6));
                            drawingContext.DrawText(Title, new Point(0, 0));
                            drawingContext.Pop();
                            break;
                        case DevFacet.TitlePlacement.Center:
                            drawingContext.PushTransform(new TranslateTransform((Width / 2.0) - (Title.Width/2.0), -Title.Height - 6));
                            drawingContext.DrawText(Title, new Point(0, 0));
                            drawingContext.Pop();
                            break;
                    }
                }

                if (SubTitle != null)
                {
                    drawingContext.PushTransform(new TranslateTransform(0, 6 + Height));
                    drawingContext.DrawText(SubTitle, new Point(0, 0));
                    drawingContext.Pop();
                }

                if (DrawProp.background != null && background == null)
                    background = (Brush?)(new BrushConverter().ConvertFromString(DrawProp.background)) ?? System.Windows.Media.Brushes.Transparent;

                // Dessiner un rectangle pour illustrer
                Rect rect = new Rect(0, 0, ContentWidth, ContentHeight);
                drawingContext.DrawRectangle(background, null, rect);
                if (reference.DrawCode.Item2 != null)
                {
                    reference.mutexReadOutput.WaitOne();

                    try
                    {
                        facet.Objects[this.Name].SetZone(new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), ContentWidth, ContentHeight));
                        reference.gui.baseZone = new DevApps.PythonExtends.Zone { Rect = rect };

                        var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                        pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream, Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                        pyScope.SetVariable("gui", reference.gui);
                        pyScope.SetVariable("name", this.Name);
                        pyScope.SetVariable("desc", reference.Description);

                        foreach (var pointer in reference.Pointers)
                        {
                            Program.DevObject.References.TryGetValue(pointer.Value.target, out var pointerRef);
                            pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream(), Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                        }

                        reference.gui.Begin(drawingContext);
                        reference.DrawCode.Item2?.Execute(pyScope);
                        reference.gui.End();
                    }
                    catch (Exception)
                    {
                    }

                    reference.mutexReadOutput.ReleaseMutex();
                }
            }
        }
    }
}
