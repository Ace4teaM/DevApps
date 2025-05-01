using static Program;

namespace DevApps.Samples
{
    internal static class CodeTemplate
    {
        internal static void Create()
        {
            DevObject.Create("template", "Template", [ "#cs", "#template"])
                .SetOutput(@"
namespace ${namespace}
{
    ${class_def}
}
${footer}
")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");

            DevObject.Create("data", "Data", ["#json"])
                .SetOutput(@"
{\n""namespace"":""Program"",\n""class_def"":""class HelloWorld { }"",\n""footer"":""// end of code""\n}
")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");

            DevObject.Create("code", "Code", ["#cs", "#script"])
                .AddPointer("template", "template", ["#template"])
                .AddPointer("data", "data", ["#json"])
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())")
            .SetBuildMethod(@"
import string
import json

# Définir un template avec des variables
template_str = template.text()

# Créer un dictionnaire de données à insérer dans le template
donnees = json.loads(data.text())

# Créer un objet Template à partir du template
template = string.Template(template_str)

# Remplacer les variables par les valeurs correspondantes
resultat = template.safe_substitute(donnees)

# Afficher le résultat
out.write(resultat)
                ");

            DevFacet.Create("Template", ["template", "code", "data"]);

            DevFacet.Get("Template")?.Objects["template"].SetZone(new System.Windows.Rect(20, 20, 200, 400));
            DevFacet.Get("Template")?.Objects["code"].SetZone(new System.Windows.Rect(300, 20, 200, 400));
            DevFacet.Get("Template")?.Objects["data"].SetZone(new System.Windows.Rect(600, 20, 200, 400));
        }
    }
}
