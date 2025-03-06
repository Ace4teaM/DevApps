using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace DevApps.GUI
{
    public class DrawElement : FrameworkElement
    {
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
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream));// mise en cache dans l'objet ?
                    pyScope.SetVariable("gui", reference.gui);
                    pyScope.SetVariable("name", this.Name);
                    pyScope.SetVariable("desc", reference.Description);
                    foreach (var pointer in reference.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream()));// mise en cache dans l'objet ?
                    }

                    reference.UserAction.Item2?.Execute(pyScope);

                    reference.mutexReadOutput.ReleaseMutex();

                    this.InvalidateVisual();
                }
            }
        }

        // Cette méthode gère le rendu
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            Program.DevObject.mutexCheckObjectList.WaitOne();
            Program.DevObject.References.TryGetValue(this.Name, out var reference);
            Program.DevObject.mutexCheckObjectList.ReleaseMutex();

            if (reference != null)
            {
                var ContentWidth = this.ActualWidth;
                var ContentHeight = this.ActualHeight;

                // Dessiner un rectangle pour illustrer
                Rect rect = new Rect(0, 0, ContentWidth, ContentHeight);
                drawingContext.DrawRectangle(/*reference.gui.GetBackground()*/Brushes.LightGray, null, rect);
                if (reference.DrawCode.Item2 != null)
                {
                    reference.mutexReadOutput.WaitOne();

                    reference.zone = new Rect(System.Windows.Controls.Canvas.GetLeft(this), System.Windows.Controls.Canvas.GetTop(this), ContentWidth, ContentHeight);
                    reference.gui.baseZone = new DevApps.PythonExtends.Zone { Rect = new Rect(0, 0, ContentWidth, ContentHeight) };

                    var pyScope = Program.pyEngine.CreateScope();//lock Program.pyEngine !
                    pyScope.SetVariable("out", new DevApps.PythonExtends.Output(reference.buildStream));// mise en cache dans l'objet ?
                    pyScope.SetVariable("gui", reference.gui);
                    pyScope.SetVariable("name", this.Name);
                    pyScope.SetVariable("desc", reference.Description);
                    foreach (var pointer in reference.GetPointers())
                    {
                        Program.DevObject.References.TryGetValue(pointer.Value, out var pointerRef);
                        pyScope.SetVariable(pointer.Key, new DevApps.PythonExtends.Output(pointerRef != null ? pointerRef.buildStream : new MemoryStream()));// mise en cache dans l'objet ?
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
