using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using static IronPython.Modules._ast;

namespace DevApps.MCP
{
    /// <summary>
    /// Serveur d'intéractions avec les modèles d'IA via le Model Context Protocol (MCP).
    /// </summary>
    internal static class MCPService
    {
        internal static Task? Current { get; private set; }
        internal static IHost? CurrentHost { get; private set; }
        internal static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        internal static string Summary = """
## Qu'est ce que c'est ?

DevApps est un outil de conception généraliste permettant de créer des présentations de processus dans un environnement simulé.

La phase de build permet le génération de code/données pour assister la réalisation de projets.

Il est adapté à plusieurs situations

* Décrire un mécanisme/système
* Générer et fusionner er du code source
* Réutiliser et partager des concepts/design pattern

DevApps est transparent et non intrusif pour vos projets. Vous pouvez décider de l'utiliser ponctuellement pour déployer un pan de votre application ou continuellement dans un cadre d'amélioration continue et de conception.

La force réside dans l'utilisation de facettes permettant de focaliser la création de code sur une partie unique de votre application. Les facettes peuvent également servir de documentation explicative/conceptuel.

DevApps est conçu pour ne pas imposer de concept interne inaliénable. Le logiciel ne fait que poser les bases d'objets interagissant avec des scripts. Le gros de la valeur ajouté se trouve dans les bibliothèques d'objets totalement personnalisables et échangeable. DevApps n'a pas pour objectif d'être auto suffisant mais d'accompagner le développement de projet.

## Objets

Dans DevApps les objets sont les principaux éléments manipulables pour décrire votre processus.

Un objet est abstrait par nature, il vous permet de saisir une donnée qu'elle soit textuelle ou binaire (à l'image d'un fichier vous pouvez donc stocker n'importe quoi dans le contenu d'un objet).

Mais un objet n'est pas qu'un contenu, il est également scriptable. Vous pouvez scripter:

* l'affichage
* la construction du contenu

Par défaut un objet est totalement vide car DevApps n'impose pas de modèle c'est à vous de le créer et de vous appuyer sur les objets partagés.

## Fichiers

DevApps permet interagir avec les fichiers finaux du projet, c'est à dire insérer ou modifier du code de votre projet pour assister le développement.

Pour ce faire de façon sécurisé, DevApps utilise un manageur dédié (hors objets) ayant pour but de lire/écrire/surveiller les modifications dans le projet final.

Une fois le lien ajouter au projet :

*  A chaque modification extérieur du fichier, le contenu est rechargé et le build exécuté si des modifications sont trouvée le fichier est réécrit pour être ajusté automatiquement.

## Objets : Instances, Modèles, Références et Fichiers

Il existe plusieurs types d'objets:

* Les instances sont les objets les plus courants
* Les références sont des objet héritant de certaines propriétés d'une instance
* Les fichiers sont des objets dont le contenu est mappé à un fichier
* Les modèles sont des instances considéré comme modèle pour d'autres instances

Une référence partage l'ensemble des paramètres d'une instance hormis les données.

Une instance peux être un modèle ou être lié à un modèle de base, dans ces cas il possède/référence un GUID.

Contrairement à une référence, l'instance avec modèle utilise le GUID pour mettre à jour son contenu. Un modèle n'est pas une référence car l'objet qui en hérite garde son indépendance vis à vis de ce dernier, mettre à jour une instance depuis un modèle est une action de l'utilisateur et donc son intégritée est garantie.

Un modèle peux faire partie du projet ou d'un projet partagé, le GUID permet de garantir l'unicité d'un modèle. N'importe quel objet peut devenir un modèle est étendre la bibliothèque. DevApps recherche dans les projets partagés pour retrouver l'objet de base.  

Les objets de fichiers maintiennent un flux sur le fichier cible, il est généralement utilisé pour les objets ayant un Timer avec lecture/écriture rapide vers un fichier de données. Pour interagir avec les fichiers de code du projet final il faut préférer l'utilisation des fichiers managées.

## Tags

chaque objet et pointeur d'objet est taggable, il donne une indication sur le contenu et les associations possibles entre les objets. Un Tag est simplement un mot-clé précédé d'un #.

Les Tags sont la base de la construction de l'arbre de la bibliothèque partagé. Un objet prend généralement 1 ou 2 tags.

Voici quelques tags fréquemment utilisés:

```
#cs indiqué du code csharp
#cpp indiqué du code c++
#c indiqué du code c
#script indique du code scriptable
#text indique un format texte 
#image un format image (png,bmp,jpg,...)
#codegen indique un générateur de code
#csv format comma separator
#raw données brutes 
#codemerge indique un fusionneur de code
#pdf fichier pdf
#yml format Yml
#json format Json 
#layout une structuration d'éléments visuels
#form un formulaire de saisie de données 
#canvas une zone graphique animable
#rust indique du code rust
#erd entity relational diagram
#dbml database markup langage 
#md markdown
```

En plus de catégoriser les objets, les tags sont utilisés sur les pointeurs pour déterminer qu'elle objet est compatible avec le lien attendue.


## Scripts

Tous les scripts de DevApps sont écrits en Python, c'est un langage de script puissant et facile à apprendre. Vous pouvez utiliser les scripts initialiser le contenu d'un objet, dessiner le visuel.
DevApps utilise Python dans un environnement sandboxé, c'est à dire que les scripts n'ont pas accès à l'ensemble des fonctionnalités de votre machine.
Par exemple, les scripts n'ont pas accès au système de fichiers ou au réseau, ils ne peuvent interagir qu'avec les objets et les outils de DevApps.
Python ne peut utiliser que les bibliothèques autorisées par DevApps, ce qui garantit la sécurité

Chaque script s'exécute dans un contexte définit

Objet.Draw peut utiliser les variables suivantes:
* dc : DrawingContext en .Net, il permet de dessiner des formes, du texte, des images, etc...
* out : le contenu de sortie, il est utilisé pour lire les données initialisé par Draw.Build
* [pointer] : pour chaque pointeur représente le contenu de l'objet pointé, il est utilisé pour lire les données initialisé par le Build de l'objet pointé

Objet.Build peut utiliser les variables suivantes:
* out : le contenu de sortie, il est utilisé pour lire les données initialisé par Draw.Build
* [pointer] : pour chaque pointeur représente le contenu de l'objet pointé, il est utilisé pour lire les données initialisé par le Build de l'objet pointé

## Géométries

Les géométries sont des objets graphiques dessinables dans les facettes, elles sont utilisées pour représenter visuellement des formes quelconques.
Elles utilisent le format SVG (comme M0,0 H480 V330 H0 Z) pour dessiner des formes.
Les géométries sont identifier par un GUID

## Textes

Les textes sont des objets graphiques dessinables dans les facettes, ils sont utilisées pour représenter visuellement un texte.
Ils utilisent le format UTF8.
Les textes sont identifier par un GUID

""";

        internal static void Start()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Services.AddMcpServer()
                .WithResources(new Dictionary<string, object>
                {
                    ["ServerInfo"] = new
                    {
                        Name = assembly.GetCustomAttributes<AssemblyTitleAttribute>().FirstOrDefault()?.Title ?? "N/A",
                        Version = assembly.GetName().Version?.ToString() ?? "N/A",
                        Description = assembly.GetCustomAttributes<AssemblyDescriptionAttribute>().FirstOrDefault()?.Description ?? "N/A",
                        Company = assembly.GetCustomAttributes<AssemblyCompanyAttribute>().FirstOrDefault()?.Company ?? "N/A",
                        Summary = Summary
                    }/* ,
                    ["Config"] = new
                    {
                        MaxConnections = 10,
                        EnableLogging = true
                    }*/
                })
                .WithStdioServerTransport() // utilise les flux d'entrée/sortie standard pour la communication
                //.WithStreamServerTransport() // utilise un flux personnalisé (ex: TCP) pour la communication
                .WithToolsFromAssembly(); // scan l'assembly pour trouver les outils à exposer [McpServerTool]

            CurrentHost = builder.Build();

            Current = CurrentHost.RunAsync(Cancel.Token);
        }

        internal static void Stop()
        {
            Cancel.Cancel();

            // attendre la fin du service
            CurrentHost?.StopAsync().Wait();
        }
    }
}
