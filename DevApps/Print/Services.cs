using Serializer;
using System.Diagnostics;
using System.IO;

namespace DevApps.Print
{
    internal static class Services
    {
        public static void PrintAll() 
        {
            try
            {
                var pdf = ToPDF.Make();
                var tmpFile = Path.GetTempFileName() + ".pdf";
                using var file = File.OpenWrite(tmpFile);
                pdf.CopyTo(file);

                Process.Start(new ProcessStartInfo(tmpFile) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }
        public static void Print(Program.DevFacet facet)
        {
            try
            {
                /*var (key,value) = facet.Objects.First();
                ToPDF.DessinerVisualDansPDF(ToPDF.CreateDrawingVisual(key, Program.DevObject.Get(key)!, value.GetZone()));
                */
                var pdf = ToPDF.Make(facet);
                var tmpFile = Path.GetTempFileName() + ".pdf";
                using var file = File.OpenWrite(tmpFile);
                pdf.CopyTo(file);

                Process.Start(new ProcessStartInfo(tmpFile) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }
        }
    }
}
