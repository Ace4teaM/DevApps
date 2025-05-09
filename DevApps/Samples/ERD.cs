﻿using static Program;

namespace DevApps.Samples
{
    internal static class ERD
    {
        internal static void Create()
        {
            // Entity Relational Model


            DevObject.Create("visual", "ERD Visualisation (kroki.io)", ["#svg", "#script"])
                .AddPointer("data", "datamodel", ["#erd"])
                .SetDrawCode(@"gui.svg(out)")
                .SetBuildMethod(@"
import sys
import base64
import zlib
from urllib.request import urlopen

# prepare les données à être envoyé dans l'url
cmp_data = zlib.compress(data.bytes(), 9)

b64_data = base64.urlsafe_b64encode(cmp_data)

# lie le contenu depuis kroki.io
link = 'https://kroki.io/erd/svg/' + b64_data.decode('utf-8')
f = urlopen(link).read()


out.write_bytes(f)

# supprime l'en-tete HTML
#idx = f.index(b'<svg ')
#str = f[idx:]

#str = '<?xml version=""1.0"" standalone=""no""?>\n' + str.decode('utf-8')

#out.write(str)

"
);
            ;

            DevObject.Create("datamodel", "ERD Data Model", ["#erd"])
                .SetUserAction("gui.edit('code', out)")
                .SetOutput(@"
[Commande]
*numero
*date

[Client]
*numero
*nom
*prenom
*adresse
*ville
*code_postal
*telephone
*email

[Produit]
*numero
*nom
*description
*prix
*quantite

[DetailCommande]
*numero
*quantite
*prix
*total
*commande
*produit

Commande 1--1 DetailCommande
Commande *--1 Client
Commande *--* Produit
")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");
            ;

            var oCode = DevObject.Create("codegen", "ERD Code generator", ["#codegen"])
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())")
                .SetUserAction(@"out.write(gui.select({'entities_to_cs_classes':'Entities UML > C# classes','entities_to_sql_tables':'Entities UML > SQL Tables','entities_to_cs_sql_model':'Entities UML > C# Database Model'},out))")
                .SetOutput(@"UserName")
                .AddPointer("input_data", "datamodel", ["#erd"])
                .SetBuildMethod(@"
def generate_csharp_code(erd_text):
    # Dictionnaire pour stocker les entités et leurs attributs
    entities = {}
    relationships = []

    # Diviser le texte en lignes pour le traitement
    lines = erd_text.strip().split('\n')

    current_entity = None

    for line in lines:
        line = line.strip()
        if line.startswith('[') and line.endswith(']'):
            # Nouvelle entité
            current_entity = line[1:-1]
            entities[current_entity] = {'primary_key': None, 'attributes': [], 'foreign_keys': []}
        elif line.startswith('*'):
            # Clé primaire
            entities[current_entity]['primary_key'] = line[1:]
        elif line.startswith('+'):
            # Clé étrangère
            entities[current_entity]['foreign_keys'].append(line[1:])
        elif line:
            # Attribut standard
            entities[current_entity]['attributes'].append(line)
        elif '--' in line:
            # Relation
            relationships.append(line)

    # Générer le code C#
    csharp_code = """"""
using System;
using System.Collections.Generic;

namespace ERDiagram
{
""""""
    # Ajouter les classes pour chaque entité
    for entity, details in entities.items():
        csharp_code += f""    public class {entity}\n    {{\n""

        # Ajouter les attributs
        for attr in details['attributes']:
            csharp_code += f""        public string {attr} {{ get; set; }}\n""

        # Ajouter la clé primaire
        if details['primary_key']:
            csharp_code += f""        public string {details['primary_key']} {{ get; set; }}\n""

        # Ajouter les clés étrangères
        for fk in details['foreign_keys']:
            csharp_code += f""        public string {fk} {{ get; set; }}\n""

        csharp_code += ""    }\n""

    # Ajouter les relations (simplifié pour l'exemple)
    for relation in relationships:
        csharp_code += f""    // Relation: {relation}\n""

    csharp_code += ""}""
    return csharp_code

# Exemple d'utilisation
erd_content = input_data.text()

# Générer le code C# à partir du contenu ERD
csharp_code = generate_csharp_code(erd_content)
out.write(csharp_code)
                ")
                .SetDrawCode(@"gui.style('Black', 2, False).foreground().stack().text(out.lines())");

            DevFacet.Create("ERD", ["codegen", "datamodel", "visual"]);

            DevFacet.Get("ERD")?.Objects["codegen"].SetZone(new System.Windows.Rect(20, 20, 400, 400));
            DevFacet.Get("ERD")?.Objects["datamodel"].SetZone(new System.Windows.Rect(500, 20, 200, 400));
            DevFacet.Get("ERD")?.Objects["visual"].SetZone(new System.Windows.Rect(800, 20, 250, 400));
        }
    }
}
