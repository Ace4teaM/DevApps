using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace DevApps.Samples
{
    internal static class UI
    {
        internal static void Create() {

            // définir une taille de base aux objets
            // puis ajuster la taille en fonction du dessin
            // le contenu ne doit jamais être directement tronqué

            DevObject.Create("List", "Exemple de liste")
                .SetOutput(@"
                    Item 1
                    Item 2
                    Item 3
                    Item 4
                    Item 5
                    Item 6
                    ", true)
                .SetDrawCode(@"
                    gui.list(out)
                ");


            DevObject.Create("Text", "Exemple de saisie")
                .SetOutput(@"Search")
                .SetDrawCode(@"
                    gui.wrap().icon('search').edit(out).pop()
                ");

            DevObject.Create("Dialog", "Exemple de boite à outils")
                .AddPointer("login", String.Empty)
                .AddPointer("password", String.Empty)
                .AddUserAction("log", "out.write('ok')")
                .AddUserAction("cancel", "out.write('nok')")
                .SetDrawCode(@"
                    gui.fill().gray().rectangle(6).inflate(-10)
                    gui.stack().white()
                    gui.text('LOGIN')
                    gui.separator()
                    gui.wrap().icon('user').edit(login).pop()
                    gui.wrap().icon('lock').edit(password).pop()
                    gui.separator()
                    # gui.wrap().button('LOGIN', log).button('CANCEL', cancel).pop()
                ");

            DevObject.Create("State", "Exemple de bouton à état")
                .SetOutput(@"0")
                .SetDrawCode(@"
                    gui.state(out, 'ON', '1', 'OFF', '0')
                ");

            DevObject.Create("Level", "Exemple de sélection numérique")
                .SetOutput(@"50")
                .SetDrawCode(@"
                    gui.level(out, '%', 0, 100, 1)
                ");
        }
    }
}
