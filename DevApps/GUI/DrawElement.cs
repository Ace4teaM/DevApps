using Microsoft.Scripting.Hosting;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Program;

namespace DevApps.GUI
{
    /// <summary>
    /// Fournit les fonctions nécessaires à l'affichage d'un objet DevObject dans un DesignerView
    /// </summary>
    public class DrawElement : DrawBase
    {
        internal FormattedText? Title;
        internal FormattedText? SubTitle;
        internal DevFacet? facet;

        internal static Typeface typeface = new Typeface("Arial");
        internal static Pen connectorPen = new Pen(Brushes.Linen, 3);
        internal System.Windows.Media.Brush? background = null;

        internal DrawElement(string objectName, DevFacet facet, Rect rect, string title, string subtitle)
        {
            this.facet = facet;
            this.Name = objectName;
            this.Width = rect.Width;
            this.Height = rect.Height;
            this.Y = rect.Top;
            this.X = rect.Left;

            this.Title = new FormattedText(title, CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, Service.typeface, 10, Brushes.Blue,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            this.SubTitle = new FormattedText(subtitle, CultureInfo.InvariantCulture,
                System.Windows.FlowDirection.LeftToRight, Service.typeface, 8, Brushes.DarkViolet,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        /// <summary>
        /// Execute l'action de double-clic sur l'objet
        /// </summary>
        /// <param name="position">Position du curseur dans le Canvas</param>
        internal void RunAction(Point position)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(this.Name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (reference != null)
                {
                    if (String.IsNullOrEmpty(reference.GetUserAction()) == false)
                    {
                        var handle2 = reference.mutexReadOutput.WaitOne();
                        if (handle2)
                        {
                            try
                            {
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
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine("******************************************");
                                System.Console.WriteLine("RunAction: " + this.Name);
                                ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                                string error = eo.FormatException(ex);
                                System.Console.WriteLine(error);
                                System.Console.WriteLine("******************************************");
                            }

                            reference.mutexReadOutput.ReleaseMutex();
                        }

                        this.InvalidateVisual();
                    }
                }
            }
        }

        /// <summary>
        /// Rendu de l'objet
        /// </summary>
        /// <param name="drawingContext">Contexte de dessin à utiliser</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var canvas = this.Parent as Canvas;

            base.OnRender(drawingContext);

            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(this.Name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (facet != null && reference != null)
                {
                    var ContentWidth = this.ActualWidth;
                    var ContentHeight = this.ActualHeight;

                    // Propriétés d'affichage de cet objet pour cette facette
                    var DrawProp = facet.Objects[this.Name];

                    // Affiche le titre (au dessus du rectangle du client)
                    if (Title != null)
                    {
                        switch (DrawProp.title)
                        {
                            case DevFacet.TitlePlacement.TopLeft:
                                drawingContext.PushTransform(new TranslateTransform(0, -Title.Height - 6));
                                drawingContext.DrawText(Title, new Point(0, 0));
                                drawingContext.Pop();
                                break;
                            case DevFacet.TitlePlacement.TopRight:
                                drawingContext.PushTransform(new TranslateTransform(Width - Title.Width, -Title.Height - 6));
                                drawingContext.DrawText(Title, new Point(0, 0));
                                drawingContext.Pop();
                                break;
                            case DevFacet.TitlePlacement.Center:
                                drawingContext.PushTransform(new TranslateTransform((Width / 2.0) - (Title.Width / 2.0), -Title.Height - 6));
                                drawingContext.DrawText(Title, new Point(0, 0));
                                drawingContext.Pop();
                                break;
                        }
                    }

                    // Affiche le sous-titre (en dessous du rectangle du client)
                    if (SubTitle != null)
                    {
                        drawingContext.PushTransform(new TranslateTransform(0, 6 + Height));
                        drawingContext.DrawText(SubTitle, new Point(0, 0));
                        drawingContext.Pop();
                    }

                    // Mise en cache de la couleur de fond
                    if (DrawProp.background != null && background == null)
                        background = (Brush?)(new BrushConverter().ConvertFromString(DrawProp.background)) ?? System.Windows.Media.Brushes.Transparent;

                    // Dessiner un rectangle pour illustrer la zone de dessin
                    Rect rect = new Rect(0, 0, ContentWidth, ContentHeight);
                    drawingContext.DrawRectangle(background, null, rect);

                    // Execute le script de dessin
                    if (reference.DrawCode.Item2 != null)
                    {
                        var handle2 = reference.mutexReadOutput.WaitOne();
                        if (handle2)
                        {
                            try
                            {
                                DrawProp.SetZone(new Rect(Canvas.GetLeft(this), Canvas.GetTop(this), ContentWidth, ContentHeight));
                                reference.gui.baseZone = new DevApps.PythonExtends.Zone { Rect = rect };

                                var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                                pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream, Path.Combine(Program.DataDir, this.Name)));// mise en cache dans l'objet ?
                                pyScope.SetVariable("gui", reference.gui);
                                pyScope.SetVariable("name", this.Name);
                                pyScope.SetVariable("dc", drawingContext);
                                pyScope.SetVariable("rect", rect);
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
                            catch (Exception ex)
                            {
                                System.Console.WriteLine("******************************************");
                                System.Console.WriteLine("OnRender: " + this.Name);
                                ExceptionOperations eo = Program.pyEngine.GetService<ExceptionOperations>();
                                string error = eo.FormatException(ex);
                                Console.WriteLine(error);
                                System.Console.WriteLine("******************************************");
                            }

                            reference.mutexReadOutput.ReleaseMutex();
                        }
                    }
                }
            }
        }
    }
}
