using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Program;

namespace DevApps.Samples
{
    internal static class InputCheck
    {
        internal static void Create() {

            DevObject.Create("webapp", "Web Application").SetDrawCode(@"gui.full().foreground(0,0,0).circle().image(out).stack().text(name).text(desc)").LoadOutput("webapp");
            DevObject.Create("service", "Data Service").SetDrawCode(@"gui.text(name).text(desc)");
            DevObject.Create("req_getinventory", "GetInventory Request").SetDrawCode(@"gui.text(name).text(desc)");
            DevObject.Create("in_inventoryid", "Request Input").SetDrawCode(@"gui.text(name).text(desc)");
            DevObject.Create("in_inventoryitemid", "Request Input").SetDrawCode(@"gui.text(name).text(desc)");
            DevObject.Create("cmd_listing", "UDP command listing on port 4432").SetDrawCode(@"gui.text(name).text(desc)");

            /*
             * Par exemple, je souhaite intégrer un modèle de données relationnel.
             * Je vais d'abord créer des entités sous forme d'objets avec comme contenu une liste de membres.
             */

            DevObject.Create("Equipement", "Entité")
                .SetOutput(@"
* code : equip_code
* desc : string")
                .AddPointer("0.1", "Emplacement")
                .SetDrawCode(@"
                    gui.full().top().stack()
                    gui.text(name)
                    gui.text(desc)
                    gui.text('')
                    gui.full().bottom().grid(2,2)
                    gui.text(out.words(r'\s*[*]\s*(\w+)\s*[:]\s*(\w+)\s*'))
                ")
                .SetBuildMethod(@"
                    console.write('HELLo WORLD')
                ");

            DevObject.Create("Emplacement", "Entité")
                .SetOutput(@"
* code : empl_code
* desc : string
* latitude : int
* longitude : int")
                .SetDrawCode(@"
                    gui.text(name)
                    gui.text(desc)
                    gui.text('')
                    gui.grid(4,2).text(out.words(r'\s*[*]\s*(\w+)\s*[:]\s*(\w+)\s*'))
                ");

            /*
             * Maintenant je souhaite intégrer une logique de génération de code.
             * J'ajoute un objet template donnant la forme générale pour mes fichiers SQL et C#
            */

            DevObject.Create("SqlModelTemplate", "SQL Code Template")
                .SetOutput(@"
-- SQL Model
Create table {name}
(
{0} {1}
);
")
                .SetDrawCode(@"
                    gui.text(name)
                    gui.text(desc)
                    gui.text('')
                    gui.text(out.lines())
                ");

            DevObject.Create("CsModelTemplate", "C# Code Template")
                .SetOutput(@"
// c# Model
public class {name}
{
    public {1} {0} {get; set;}
}
")
                .SetDrawCode(@"gui.text(out.lines())");

            /*
             * Puis j'intègre le script de générations de code. (C'est le pointeur BUILD qui contient le code final)
             */

            //DevObject.Group("MODEL", "SqlModelTemplate", "CsModelTemplate");

            DevObject.Get("SqlModelTemplate")
                .AddPointer("build1", "MODEL");

            DevObject.Get("CsModelTemplate")
                .AddPointer("build2", "MODEL");

            DevObject.Select("SqlModelTemplate", "CsModelTemplate")
                .SetBuildMethod(@"
    
            print(equipement)

            # recherche les groupes de champs nom/type
            # result = re.findall(r'\s*[*]\s*(\w+)\s*[:]\s*(\w+)\s*', equipement)

            # edit le code existant
            #editor.inclass('InputsCheck').inproperty(result[0]).getset()

            # 
            #for r in result:
            #    tmp = 
            #    print('{} is {}'.format(r[0],r[1]))
            #    modified_out.insert(index, number)
    
            console.write('HELLO')


            #print(editor.string())
");

            /*
             * J'intègre les commandes de générations des fichiers
            */

            DevFacet.Create("Model", DevObject.SelectAll())
                /*.AddInstallCommand(@"
    py -m pip install openai
    ")*/
                .AddBuildCommand(@"
cat $build1 > .\bdd\sql\model.sql
cat $build2 > .\ui\cs\model\entities.cs
");

            /*
             * Fusion de code


            DevObject.Create("CodeMerge", "IA Code Template")
                .SetBuildMethod(@"
                    from openai import OpenAI

                    client = OpenAI(
                    api_key=""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""
                    )

                    completion = client.chat.completions.create(
                        model=""gpt-4o-mini"",
                        store=True,
                        messages=[
                            {""role"": ""user"", ""content"": ""write a haiku about ai""}
                        ]
                    )

                    print(completion.choices[0].message);
    ");*/

            DevObject.Create("CodeMerge", "IA Code Merge")
                .SetBuildMethod(@"
                url = 'https://api.openai.com/v1/chat/completions'
                key = ""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""
                headers = {""Authorization"": f""Bearer {key}""}
                message = """"""
                merge this code
                ```csharp
                public class HelloWorld
                {
                     public int Property{get;set;}
                }
                ```
                with this code
                ```csharp
                public class   HelloWorld : InherithedClass
                {
                    public void MyMethod()
                    {
                    }
                }
                ```
                display the result without comment and based only on the syntax of the language
                """"""
                data = {'model': 'gpt-4o-mini', 'messages':[{""role"": ""user"", ""content"": message}]}
                #print(requests.post(url, headers=headers, json=data))
")
                .SetDrawCode(@"
                    gui.text(name)
                    gui.text(desc)
                ");
            /*


            DevObject.Group("InputsRegex", "regex_bool", "regex_date", "regex_uri", "regex_string", "regex_pwd", "regex_numeric", "regex_name", "regex_text", "regex_ipv4", "regex_int", "regex_int", "regex_id", "regex_float", "regex_factor", "regex_datetime");

            DevObject.Create("regex_bool", "Boolean RegEx")
                .SetOutput(@"on|off|0|1|yes|no|true|false")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            var date_sep = @"[\-\/\\\s]";
            var time_sep = @"[\:\s]";

            DevObject.Create("regex_date", "date RegEx")
                .SetOutput(
                @"(?:([0-9]{1,2})" + date_sep + @"([0-9]{1,2})" + date_sep + @"([0-9]+))" + // DMY
                @"|" +
                @"(?:([0-9]+)" + date_sep + @"([0-9]{1,2})" + date_sep + @"([0-9]{1,2}))" // YMD
                )
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            var scheme = @"[A-Za-z]{1}[A-Za-z0-9+\.\-]*";
            var port = @"[0-9]+";
            var domain = @"[A-Za-z]{1}[A-Za-z0-9_\.:\-]*"; //Registry-based
            var path = @"[A-Za-z0-9_\.+%\-]*";
            var query = @"[A-Za-z0-9_\.&=+;%\-\(\)\:\/]*";
            var fragment = @"[A-Za-z0-9_+%\-]*";

            DevObject.Create("regex_uri", "uri RegEx")
                .SetOutput(
                @"((" + scheme + @"):/+)?" +
                @"(" + domain + @")?" +
                @"^([/" + path + @"]*)([?" + query + @"]?)([#" + fragment + @"]?)"
                )
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_text", "multiline text RegEx")
                .SetOutput(@".*")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_string", "string RegEx")
                .SetOutput(@"[^""\n\r]*")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_pwd", "Password RegEx")
                .SetOutput(@"[a-zA-Z0-9_\-\@\#\&\+\~]+")

                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_numeric", "Numeric RegEx")
                .SetOutput(@"(\-?(?:0|[1-9]{1}[0-9]*))|(\-?[0-9]+(?:[\.\,][0-9]*)?)")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_name", "Name RegEx")
                .SetOutput(@"[a-zA-Z_]{1}[a-zA-Z0-9_\-\.]*")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_text", "Mail RegEx")
                .SetOutput(@"XXXXXXXXXXXXXXXXXXXXXXX")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_ipv4", "IPv4 RegEx")
                .SetOutput(@"(?:0|1[0-9]{0,2}|2[0-4][0-9]|25[0-5])\."+
                           @"(?:0|1[0-9]{0,2}|2[0-4][0-9]|25[0-5])\."+
                           @"(?:0|1[0-9]{0,2}|2[0-4][0-9]|25[0-5])\."+
                           @"(?:0|1[0-9]{0,2}|2[0-4][0-9]|25[0-5])")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_int", "Integer RegEx")
                .SetOutput(@"\-?(?:0|[1-9]{1}[0-9]*)")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_id", "Identifier RegEx")
                .SetOutput(@"[a-zA-Z_]{1}[a-zA-Z0-9_]*")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_float", "Float RegEx")
                .SetOutput(@"\-?[0-9]+(?:[\.\,][0-9]*)?")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_factor", "Factor RegEx")
                .SetOutput(@"(?:0|1)(?:\\.[0-9]+)?")
                .AddFunction(@"check", @"re.match(out, in0)")
                ;

            DevObject.Create("regex_datetime", "DateTime RegEx")
                .SetOutput(
                    @"(?:([0-9]{1,2})" + date_sep + @"([0-9]{1,2})" + date_sep + @"([0-9]+)\s*([0-2]{1}[0-9]{1})" + time_sep + @"([0-6]{1}[0-9]{1})" + time_sep + @"([0-6]{1}[0-9]{1}))" + //DMY-HMS
                    @"|"+
                    @"(?:([0-9]+)" + date_sep + @"([0-9]{1,2})" + date_sep + @"([0-9]{1,2})\s*([0-6]{1}[0-9]{1})" + time_sep + @"([0-6]{1}[0-9]{1})" + time_sep + @"([0-2]{1}[0-9]{1}))")//YMD-SMH)
                .AddFunction(@"check", @"re.match(out, in0)")
                ;*/

        }
    }
}
