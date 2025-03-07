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
                    gui.wrap().icon('search').text(out.text()).pop()
                ");


            DevObject.Create("login", "login")
                .SetUserAction(@"out.write(gui.getline(out, r'^([A-z0-9]+)$'))")
                .SetOutput(@"UserName")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.text())");
            ;

            DevObject.Create("password", "password")
                .SetUserAction(@"out.write(gui.getline(out, r'^([A-z0-9]+)$'))")
                .SetOutput(@"*****")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.text())");
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
                    gui.wrap().icon('user').text(login.text()).pop()
                    gui.wrap().icon('lock').text(password.text()).pop()
                    gui.separator()
                    # gui.div(2).rectangle('LOGIN').rectangle('CANCEL').pop()
                ");

            DevObject.Create("State", "Exemple de bouton à état")
                .SetOutput(@"0")
                .SetUserAction(@"out.write('1' if out.text() == '0' else '0')")
                .SetDrawCode(@"
                    gui.state(out, 'ON', '1', 'OFF', '0')
                ");

            DevObject.Create("Level", "Exemple de sélection numérique")
                .SetOutput(@"33")
                .SetDrawCode(@"
                    gui.level(out, '%', 0, 100, 1)
                ");
        }
    }
}
