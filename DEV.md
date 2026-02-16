# DevApps

DevApps est un projet de logiciel Desktop, c'est à dire un logiciel prévu pour être exécuté est utilisé localement sur une machine dédié. Il prend la forme d'un moteur d'exécution .NET et d'une interface utilisateur WPF (Windows).

# Projet

## **Idée**

L'idée est de proposer aux développeur un outil multi-service pour être utilisé comme : une base de connaissance communautaire, un outil de conception de processus informatique et d'un outil de productivité (insertion-fusion code).

DevApps peut être un outil pour décrire un concept technique ou un projet simple. Il est à la frontière entre le processus scientifique et le développeur.

## **Concept**

Pour réaliser ce logiciel je me base sur plusieurs paradigmes:

* Pouvoir d'écrire n'importe quel donnée / transformation sans dépendre d'un cadre fixe (UML / Merise / Format de données / ...)
* Chaque projet détail un processus précis et doit nourrir un ensemble commun plus grand
* Minimiser au maximum les dépendances du versioning sur les projets (pour persister dans le temps) 
* L'Outil est non-intrusif car il ne cadre pas un projet il assiste pour sa réalisation

## **Réalisation**

Pour réaliser ce logiciel je définit plusieurs lignes directrices:

* Pour pouvoir d'écrire n'importe quel donnée l'utilisateur doit pouvoir représenter ses propres données (binaire ou texte) il doit également pouvoir représenter graphiquement ses données.
* Pour qu'un projet soit utilisable dans un autre il faut qu'il possède des objets/éléments que l'on puisse importer/utiliser dans son propre projet en maintenant un lien d'identification permettant aux 2 projet de s'utiliser mutuellement.
* Pour rendre le projet "indépendant" de la version du logiciel, ce dernier ne doit être qu'un outil permettant de faciliter la définition des objets du projet. Une méthode alternative ou manuelle doit être possible. Il n'y a pas de dépendance forte entre le projet et le logiciel (format de données ouvert, définition claire, la fonctionnalité est l'objet pas un composant du logiciel)
* Pour être non intrusif DevApps ne doit pas dénaturer les fichiers d'un projet en intégrant ses propres concepts (annotation, format de données, structure de l'arborescence, ...). Toutes les informations persistantes son regroupé dans un seul fichier sans dépendance pour le projet et ne faisant pas partie du livrable.
* Pouvoir visualiser les détails d'un projet sous un certains angle (différentes facettes). C'est à dire pouvoir se concentrer sur un conception (ex: la validation des champs de saisies peut importe les choix d'implémentation)

Pour ce faire: 

* Le fichier `.devapps` est un **fichier unique** comprenant toutes les informations d'un projet. Il est caché par défaut est généralement ignoré par le suivi de version
* Un **dossier dédié par projet**
* Un dossier servant de **bibliothèque partagé** dont l'arborescence classifie les projets par thème
* Un objet est par définition un élément structuré: il peut être **visuel**, exécuter une **action périodique**, construire ses propres **données**, **pointer** vers d'autres objets, exposé des **propriétés** (valeurs)
* Un objet peut être marqué comme "**modèle**", c'est à dire qu'il se définit comme réutilisable par d'autre projet. Le format **GUID** permet de lui attribuer un identifiant unique
* Un **pointeur est un lien symbolique** vers un autre objet pas une dépendance forte (un nom relatif permet de l'identifier)
* Un **moteur de script** unique permet à l'utilisateur de construire des données complexes et de dessiner la représentation visuel.

## Codage

Technologies:

* **JSON** pour le format de fichier `.devapps`
* **WPF** pour l'interface utilisateur
* **.NET** pour le moteur exécution
* **IronPython** pour le scripting

## Validation

**Comment la clause de non-intrusivité est elle respectée ?**

Oui, car toutes les actions visant à modifier le projet cible est déclenché par l'utilisateur. A aucun moment le projet ciblé ne possède de dépendances ou de lien avec des fichiers générés.

**Pourquoi ne pas utiliser un assisant IA pour réaliser le même objectif ?**

Un agent IA permet effectivement assister au développement en proposant des implémentations adaptées au contexte du projet ciblé. Cependant, le travail réalisé n'est pas prédictible est dénué de suivi. L'IA est un outil puissant mais possède aussi ses inconvénients, en se reposant sur une "intelligence" unique ont omet aussi tout le processus créatif et la méthodologie utilisée pour arriver au but final.  Je pense qu'en conservant le processus de conception DevApps contribue à concevoir une base de connaissance prévisible et critiquable. L'IA peut être un acteur du processus en facilitant/automatisant l'intégration du code conçu au préalable par un Humain mais sans l'intégrer dans le processus d'élaboration.

DevApps centralise donc les efforts de l'utilisateur sur la conception et non la réalisation.

**Oui mais je peux demander à une IA de me lister les différentes options techniques, choisir l'une d'entre elles puis lui demander de l'implémenter ?**

C'est vrai est l'IA est en pleine évolution. DevApps à cependant la qualité de maintenir une bibliothèque de connaissances indépendante et de pousser au processus créatif.

**Comment la clause de non-dépendance entre les projets est-elle assurée ?**

Chaque modèle d'objet possède un **identifiant unique (GUID)**, donc chaque objet basé sur un modèle maintient en mémoire le **GUID de l'objet sur lequel il est basé**. Cependant **chaque objet est unique** et chaque **modification du modèle n'est pas 'automatiquement' répercuté** sur ses héritants. En effet, cela pourrait être *tentant* de maintenir un lien d'héritage entre les 2 objets mais cela créerait immédiatement un arbre de dépendance fort qui empêcherait tout objet de changer de nature et imposerait au projet un contexte fort de dépendance de bibliothèques. Au lieu de cela, l'outil permet à l'utilisateur de rechercher l'objet modèle et de mettre à niveau l'objet héritant. Ainsi, l'utilisateur a le choix entre se basé sur la nouvelle implémentation du concept, conserver son implémentation originale ou encore devenir à son tour un nouveau modèle (concept dérivé). 

**Pourquoi ne pas intégrer différents moteurs de scripts rendant la programmation plus personnalisable et permettant l'utilisation de librairies externes tel que Pandas ?**

Car cela enfreint la clause de portabilité de la bibliothèque partagé et des projets en général. Cependant DevApps permet de coder des extensions utilitaires mais leurs utilisations sont restreintes à la génération de contenu en amont du processus de création. Il n'y a donc aucune dépendance avec le projet. Il n'y pas non plus de dépendance avec l'environnement de la machine hôte.

## Livraison

DevApps est disponible au téléchargement sous forme de **Release** au format **Windows Portable**

# Workflow

DevApps est un projet open-source sans financement. Il initialement basé sur un projet de **Fast-Coding**, on part d'un concept simple et on l'implémente le plus rapidement possible pour avoir un premier retour d'expérience positif/négatif et décider de l'avenir du projet.

Aujourd'hui la base de code commence à grandir est DevApps a besoin d'un Workflow pour améliorer la qualité et la direction du projet.

## Hébergement

DevApps est hébergé sur GitHub et cette plateforme propose un système d'intégration continue personnalisable : les GitHub Actions.

## GitHub Actions

```yaml
name: CI/CD DevApps

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build-lint-test:
    runs-on: windows-latest

    steps:
      # Checkout du code
      - uses: actions/checkout@v4

      # Setup .NET
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x

      # Restore des packages
      - name: Restore dependencies
        run: dotnet restore DevApps.sln

      # Installer outils de lint/format
      - name: Install Tools
        run: |
          dotnet tool install -g dotnet-format
          dotnet tool install -g csharpier

      # Vérifier format et lint
      - name: Format & Lint
        run: |
          dotnet format DevApps.sln --verify-no-changes
          csharpier . --check

      # Build de la solution
      - name: Build
        run: dotnet build DevApps.sln --configuration Release --no-restore

      # Tests (si projets de tests inclus)
      - name: Run Tests
        run: dotnet test DevApps.sln --no-build --verbosity normal

      # Publish de l’application (.exe)
      - name: Publish App
        run: dotnet publish DevApps/DevApps.csproj --configuration Release --output ./publish

      # Upload des artefacts
      - name: Upload Build Artifacts
        uses: actions/upload-artifact@v4
        with:
          name: DevApps-Publish
          path: ./publish

  codeql-analysis:
    needs: build-lint-test
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Initialize CodeQL
        uses: github/codeql-action/init@v3
        with:
          languages: csharp

      - name: Build for CodeQL
        run: dotnet build DevApps.sln --configuration Release

      - name: CodeQL Analyze
        uses: github/codeql-action/analyze@v3
```



# Notes de Développement

## Support de l'analyse syntaxique (ANTLR)

**Ajouter le support à un  nouveau langage**

* Installer le Java Runtime (dernière version)

https://learn.microsoft.com/en-us/java/openjdk/download

* Télécharger le ANTLR  (dernière version)

https://www.antlr.org/download.html

* Télécharger les grammaires

https://github.com/antlr/grammars-v4/tree/master

* Générer les classes pour le langage C# (par exemple ici pour interpréter le langage C)

```
java -jar antlr-4.13.2-complete.jar -Dlanguage=CSharp C.g4
```

**-Dlanguage** : indique le langage de destination de interpréteur

**C.g4** : A remplacer par le langage interprété en fonction des grammaires téléchargés

* Copier le contenu des fichiers `*.cs` générés dans le code **DevApps** sous `DevApps\ANTLR\[LANG]`



**NOTE: Si le dossier contient plusieurs fichiers '.g4' il faut les compiler individuellement:**

```
grammars-v4-master\dart2\Dart2Lexer.g4
grammars-v4-master\dart2\Dart2Parser.g4
```

**NOTE: Les commentaires de code sont parfois 'skip', il ne peuvent pas être détecté dans le Lexer.**

Pour les inclure il faut remplacer `skip` par `channel(HIDDEN)`

Remplacer:

```
SINGLE_LINE_COMMENT : '//' ~[\r\n]*                         -> skip;
MULTI_LINE_COMMENT  : '/*' ( MULTI_LINE_COMMENT | .)*? '*/' -> skip; 
```

Par

```
SINGLE_LINE_COMMENT : '//' ~[\r\n]*                         -> channel(HIDDEN);
MULTI_LINE_COMMENT  : '/*' ( MULTI_LINE_COMMENT | .)*? '*/' -> channel(HIDDEN); 
```
