## DevApps - Notes de Développement

### Support de l'analyse syntaxique (ANTLR)

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



Si le dossier contient plusieurs fichiers '.g4' il faut les compiler individuellement:

```
grammars-v4-master\dart2\Dart2Lexer.g4
grammars-v4-master\dart2\Dart2Parser.g4
```



* Copier le contenu des fichiers `*.cs` générés dans le code **DevApps** sous `DevApps\ANTLR\[LANG]`
