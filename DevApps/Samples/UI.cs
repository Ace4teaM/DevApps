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


            DevObject.Create("login", "login")
                .SetUserAction(@"out.write(gui.getline(out, r'^([A-z0-9]+)$'))")
                .SetOutput(@"UserName")
            ;

            DevObject.Create("password", "password")
                .SetUserAction(@"out.write(gui.getline(out, r'^([A-z0-9]+)$'))")
                .SetOutput(@"*****")
            ;

            DevObject.Create("Dialog", "Exemple de boite à outils")
                .AddPointer("login", "login")
                .AddPointer("password", "password")
                .SetDrawCode(@"
                    gui.fill()
                    gui.style('Gray', 2, True).background()
                    gui.style('White', 2, False).foreground()
                    gui.rectangle(6).inflate(-10)
                    gui.stack()
                    gui.text('LOGIN')
                    gui.separator()
                    gui.wrap().icon('user').edit(login).pop()
                    gui.wrap().icon('lock').edit(password).pop()
                    gui.separator()
                    # gui.wrap().button('LOGIN', log).button('CANCEL', cancel).pop()
                ");

            DevObject.Create("State", "Exemple de bouton à état")
                .SetOutput(@"0")
                .SetUserAction(@"out.write(gui.select({0:'0',1:'1'}, out))")
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
