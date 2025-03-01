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
