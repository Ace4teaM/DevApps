using static Program;

namespace DevApps.Samples
{
    internal static class CodeTemplate
    {
        internal static void Create()
        {
            DevObject.Create("cstemplate", "Template")
                .SetInitMethod(@"
                class CsTemplate:
                    base = ""{namespace}\n\n{class def}\n\n{footer}""

                cstemplate = CsTemplate()
                ")
                .AddProperty(@"base", @"cstemplate.base")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");

            DevObject.Create("code", "Code")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");
            ;

            DevFacet.Create("Template", ["cstemplate", "code"]);

            DevFacet.Get("Template")?.Objects["cstemplate"].SetZone(new System.Windows.Rect(20, 20, 400, 400));
            DevFacet.Get("Template")?.Objects["code"].SetZone(new System.Windows.Rect(500, 20, 200, 400));
        }
    }
}
