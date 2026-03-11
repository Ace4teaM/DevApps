# DevApps

## What is it?

DevApps is a **general-purpose design tool** that allows you to create **process presentations within a simulated environment**.

The **build phase** generates **code and data** to assist with project development.

It is suitable for several situations:

- Describing a mechanism or system
- Generating and merging source code
- Reusing and sharing concepts/design patterns

DevApps is **transparent and non-intrusive** for your projects. You can decide to use it **occasionally** to deploy a part of your application or **continuously** within a framework of continuous improvement and design.

Its strength lies in the use of **facets**, which allow code generation to focus on a **specific part of your application**. Facets can also serve as **explanatory or conceptual documentation**.

DevApps is designed **not to impose any immutable internal concepts**. The software only provides the foundations of objects interacting with scripts. Most of the added value lies in **object libraries that are fully customizable and shareable**. DevApps is not intended to be self-sufficient but rather to **support project development**.

------

# Objects

In DevApps, **objects** are the main manipulable elements used to describe your process.

An object is **abstract by nature**. It allows you to store data, whether **textual or binary**. Similar to a file, you can store **any type of content** inside an object.

But an object is not just content; it is also **scriptable**. You can script:

- the display
- the construction of the content

By default, an object is **completely empty**, since DevApps does not impose any model. It is up to you to create them and rely on shared objects.

------

# Files

DevApps allows interaction with the **final project files**, meaning it can **insert or modify code in your project** to assist development.

To do this securely, DevApps uses a **dedicated manager (separate from objects)** responsible for reading, writing, and monitoring modifications in the final project.

Once the link is added to the project:

- Each time an external modification occurs in the file, the content is reloaded and the **build process is executed**.
- If modifications are detected, the file is **rewritten to automatically adjust it**.
- The user can then choose whether or not to **apply the modifications to the final file**.

------

# Objects: Instances, Models, References and Files

There are several types of objects:

- **Instances** – the most common objects
- **References** – objects inheriting certain properties from an instance
- **Files** – objects whose content is mapped to a file
- **Models** – instances considered as templates for other instances

A **reference** shares all the parameters of an instance **except the data**.

An **instance** can be a model itself or be linked to a base model. In these cases, it contains or references a **GUID**.

Unlike a reference, an instance linked to a model uses the **GUID to update its content**. A model is not a reference because the object inheriting from it keeps its **independence**. Updating an instance from a model is a **user action**, ensuring its integrity.

A model may belong to the project or to a **shared project**. The GUID guarantees the **uniqueness of a model**. Any object can become a model and extend the library. DevApps searches in shared projects to find the base object.

**File objects** maintain a stream to the target file. They are generally used for objects with a **Timer** performing fast read/write operations on a data file.

To interact with the code files of the final project, it is preferable to use **managed files**.

------

# Tags

Each object and object pointer can be **tagged**. Tags provide an indication about the content and possible associations between objects.

A tag is simply a **keyword preceded by `#`**.

Tags form the basis of the **shared library tree structure**. An object usually has **one or two tags**.

Here are some commonly used tags:

```
#cs indicates C# code
#cpp indicates C++ code
#c indicates C code
#script indicates scriptable code
#text indicates text format
#image image format (png, bmp, jpg, ...)
#codegen code generator
#csv comma-separated format
#raw raw data
#codemerge code merger
#pdf PDF file
#yml YML format
#json JSON format
#layout visual element structure
#form data entry form
#canvas animated graphic area
#rust indicates Rust code
#erd entity relational diagram
#dbml database markup language
#md markdown
```

In addition to categorizing objects, tags are also used on **pointers** to determine which objects are compatible with the expected link.

------

# Scripts

All DevApps scripts are written in **Python**. It is a powerful scripting language that is easy to learn.

Scripts can be used to:

- initialize the content of an object
- draw the visual representation

DevApps runs Python in a **sandboxed environment**, meaning scripts do not have access to all the features of your machine.

For example, scripts **cannot access the file system or the network**. They can only interact with **DevApps objects and tools**.

Python can only use the **libraries authorized by DevApps**, which guarantees security.

Each script runs within a defined context.

### `Object.Draw` can use the following variables:

- **dc** – a .NET `DrawingContext` used to draw shapes, text, images, etc.
- **out** – the output content initialized by `Draw.Build`
- **[pointer]** – for each pointer, represents the content of the pointed object

### `Object.Build` can use the following variables:

- **out** – the output content
- **[pointer]** – the content of the pointed object

------

# Geometries

Geometries are **graphical objects that can be drawn in facets**. They are used to visually represent arbitrary shapes.

They use the **SVG format** (for example `M0,0 H480 V330 H0 Z`) to draw shapes.

Geometries are identified by a **GUID**.

------

# Texts

Texts are **graphical objects that can be drawn in facets**. They are used to visually represent text.

They use **UTF-8 format**.

Texts are identified by a **GUID**.

------

# Commands

Commands are another type of object used to group a series of executions as **pseudo-commands**.

Examples:

- Read/write files
- Manage versions
- Import/export data
- ...

For security reasons, **no system command is stored directly in the project**.

Commands are defined in the **Windows registry for each user**. A command definition contains a name, syntax, and arguments described in **JSON format**.

The user therefore defines which commands are available in their system, preventing the injection of suspicious commands.

The project itself contains **pseudo-command calls**, which may execute differently depending on the system. DevApps can therefore warn about **missing commands** and potentially install the required tools.

**BUILTIN commands** are predefined objects providing a set of standard commands. They generally allow interaction with the **final project files**.

Commands can be visualized as **visual objects showing their call order**.

Tools, like commands, are **external command-line executions**. The result is processed by the calling object.
