## DevApps - Notes de D�veloppement

### Support de l'analyse syntaxique (ANTLR)

**Ajouter le support � un  nouveau langage**

* Installer le Java Runtime (derni�re version)

https://learn.microsoft.com/en-us/java/openjdk/download

* T�l�charger le ANTLR  (derni�re version)

https://www.antlr.org/download.html

* T�l�charger les grammaires

https://github.com/antlr/grammars-v4/tree/master

* G�n�rer les classes pour le langage C# (par exemple ici pour interpr�ter le langage C)

```
java -jar antlr-4.13.2-complete.jar -Dlanguage=CSharp C.g4
```

* Copier le contenu des fichiers `*.cs` g�n�r�s dans le code **DevApps** sous `DevApps\ANTLR\[LANG]`
