
# Introduction

The current implementation of Myra does not allow for AOT, but most console manufacturers disallow for generating code at runtime. (Making AOT a must, given the .NET runtime isn't allowed). This is a major reason not to use Myra as a MonoGame dev; you can never release to console unless you do major refactoring.

**Meaning**: _Myra cannot be used for console titles!_ See also [https://docs.monogame.net/articles/getting_started/preparing_for_consoles.html](https://docs.monogame.net/articles/getting_started/preparing_for_consoles.html)

Currently MonoGame is in version _1.6.4_; this means the refactor would make it the _2.0_ version, and need to exist on a separate branch as a _2.0-alpha_ prerelease version until it is ready to be deployed.

[!NOTE]
If Maya's version was for example _2.5.3_, this document would be titled "Myra 3.0 plan" instead.

The following document outlines a plan of action:
1. Principal goals to be achieved
2. Major features to be added, reworked or removed
3. Timelines and software architectural diagrams how features should be implemented.

# Principal goals to be achieved

Let us take a step back: _why are we here?_ 

MonoGame is used because:
- It is open source, and therefore not bound to the "pray I don't alter it any further" licensing and terms agreements _some particular game engines_ are known for.
- It is small and lightweight but still quite performant; one can make games in modern .NET while not having to manage memory like C/C++ forces one to do.  (Unlike Godot)

Why does one need a User Interface framework on top of it?
- So one doesn't have to reinvent the wheel for user interfaces; layouts, dealing with different screen sizes, animations, UI element state, setting it up, etc.

### Why do people like Myra?

- It's easy to set it up in your project
- You can quickly make UI's using Myrapad in XML, using the build-in MML system
- You can easily bind variables to UI elements and handle events.

However:
- It is not AOT-compatible, which prevents you from deploying it to consoles and limiting its usage on desktop and mobile.
- It forces one to work with Myrapad and learn its "own system", rather than working with known UI-setups like XAML the industry is familiar with. (Using a MVVM pattern)

The latter reason is important; in the end, unless you're hobbying, you want to release a product; sticking to industry standards makes life easier. If you need to hire a UI engineer or artist, them knowing XAML makes it easy (and less costly) to integrate them in the project.

### Therefore, the principal goals are:

- Be AOT compatible:
	- So games can be released to console
	- For desktop to not require the player to have installed .NET on their device.
- Be highly performant, light on memory, low pressure on the garbage collector and the GPU
- Be more secure and less open to malicious intent. (foremostly through type safety)
- Keep the project on _.NET Standard 2.0_ so it can be used on any .NET Framework or modern .NET (or future) version.
- Make the developer's life easier: _"they should be busy with their game, not bugfixing the UI"_
	- Make it easy to set up `.xaml` and `.xaml.cs` code-behinds for entire user interfaces or individual widgets.
		- Ensure the IDE's text editor "colors the .xaml properly" and invoked red squiggly lines when issues occur
		- Make the behavior of XAML equivalent to WPF, Xamarin/MAUI, AvaloniaUI and so on - all "basic" XAML features must be supported.
	- Make it easy for the user to recognize when they make mistakes (or potential mistakes) that require fixing. At compile-time, but preferable before in the editor when possible.
		- Because it is now AOT-compatible, most effort is moved from runtime to compile-time, therefore errors/warnings can be thrown if something goes wrong; this way, the game developers save valuable time not having to "try something" if we already know it will fail.
	- Ensure compilation is performant; _we don't want to cause extreme long compilation times because of new features_
	- Use 'known features' when possible; rather than reinvent a wheel the game developers will need to get used to, use .NET provided tools instead. For example, use `System.ComponentModel.DataAnnotations` for view models.
	- Keep it `CLSCompliant` so one could use it in Visual Basic, F#, etc.


# Major features to be added, reworked or removed

## For removal:

- Myrapad (use IDE's instead)
- MML Folder in _/src/myra/_
- All `System.Reflection` based code (including XML-to-Widget/Desktop converters)

## To be reworked:

* Move supported Monogame version to 3.8.5 (or 3.8.* and up?)
* Have all events be based on .NET provided or custom `EventArgs`-derived classes.
* Widgets (base type)
* Desktop 
	* Perhaps renaming them to _UILayer_, given you can multiple of them at the same time?
* Styles. 
	* Rather than "apply the style to the properties of the widget", instead have the widget use the assigned style's properties instead during rendering.  
	* Allow styles to be applied based on the screen's width, similar how a webpage has different styles for mobile phones.

## To be added

* Source generators for _.xaml_, to convert it into C# code that generates the item.
	* Perhaps use the [XamlX Compiler](https://github.com/kekekeks/XamlX)?
* Source analyzer for the _.xaml_ code (find issues in the file the user is editing/compiling - AFAIK, _XamlX_ takes care of this for us)
* Source generator for DTO types usable with `PropertyGrid<T>` and `DataGrid<T>`, in a similar vein to source-generated `System.Text.Json` [Source generated JSON](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)
* An IDE plugin to allow for _.xaml_ "coloring" in the text editor and for _.xaml_ and _xaml.cs_ file creation templates.
	* Visual Studio, Visual Studio Code, Jetbrains, etc. 
* New widgets:
	* A `Form`, which is attached to a view model DTO class/record. (e.g. "new character creation screen"). The attributes on the properties define if the form submission is valid to be used. Inside the `Form`'s contents, the programmer can create the layout of the form and how the inputs, labels or necessary extra UI widgets  are placed.
	* (_to be decided?_)
* A content exporter for stylesheets, for the new content pipeline. 
	* (Or the game developer can export or load it in via JSON/XML instead - but that's fine, given a stylesheet should be a simple DTO object the modules use)

# XAML features

The following features for XAML should be supported:
- The accessibility modifier of the produced C# code of a XAML is the same as the one defined in the code-behind; if there no code-behind, the XAML will be `public`.


## Basic setup

A `.xaml` file has the following root element:

```xml 
<Widget
  xmlns="https://github.com/MyraUI/Myra"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:myAlias1="using:AppNameSpace.MyNamespace"
  xmlns:myAlias2="clr-namespace:OtherAssembly.MyNameSpace;assembly=OtherAssembly"
  x:Class="AppNameSpace.CurrentNamespace.FooBar">
</Widget>
```


- `xlmns` refers to the MyraUI's github repo. 
- `xlmns:x` refers to the rules set in WinFX; most XAML-based frameworks (e.g. Avalonia) use it.
- `xmlns:myAlias1` allows the programmer to invoke custom components.
- `xmlns:myAlias2` refers to another assembly's types (e.g. from a library)
- `x:Class` is optional (override for the code-behind's class name). If left empty, the compiler will presume the name of the _xaml_ file is the name of the class, and the folder structure in the _.csproj_ along with the project's root namespace defines the namespace of the class.

## Events

Attach event handlers in XAML by specifying the event as an attribute and the handler method name as its value:

```xml
<Button Click="ShowUpdates">Show updates</Button>
```

In this example, the _xaml.cs_ code-behind must have method `ShowUpdates` defined:
- As a static or non-static method
- With any accessibility modifier (none assigned, `private`, `protected`, `public` or `internal`)
- Either having:
	- No parameters
	- 1 parameter (`ClickEventArgs`)
	- 2 parameters (`object sender, ClickEventArgs e`)

## Markup extensions

Markup extensions let you set a property to something that can't be expressed as a plain string; like a reference to a shared resource or a data binding. They use curly brace syntax:

```xml
<Button Style="{StaticResource SearchButtonStyle}" />
```

| Extension                                                                                                                  | Purpose                                                                                        |
| -------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| [{x:Bind}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/x-bind-markup-extension)                   | Compile-time data binding (best performance)                                                   |
| [{Binding}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/binding-markup-extension)                 | Runtime data binding                                                                           |
| [{StaticResource}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/staticresource-markup-extension)   | Reference a `ResourceDictionary` entry by key                                                  |
| [{ThemeResource}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/themeresource-markup-extension)     | Only for `StyleSheet`'s: it's like `{StaticResource}`, but updates when the stylesheet changes |
| [{TemplateBinding}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/templatebinding-markup-extension) | Bind to a property of a control template's parent                                              |
| [{RelativeSource}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/relativesource-markup-extension)   | Bind relative to the templated parent                                                          |
| [{CustomResource}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/customresource-markup-extension)   | Advanced custom resource lookup                                                                |
| [{x:Null}](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/x-null-markup-extension)                   | Explicitly set a nullable value to `null`                                                      |

## Allow lookup in code-behind

Allow items to be referenced in the code-behind.

Assume the following:
```xml
<Widget
  xmlns="https://github.com/MyraUI/Myra"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <VerticalMenu Ref="MyMenu">
    <MenuItem>..</MenuItem>
    <MenuItem>..</MenuItem>
    ..etc
  </VerticalMenu>
</Widget>
```

This allows one to use `MyMenu` in the code-behind, or use `this` for the widget we're creating itself. The programmer can define `MyMenu` property itself, or let the source-generator handle it for them. 

This allows one to find items based on their position in the UI-tree:

```csharp
// Get the logical parent (or null if it is the root)
var parent = this.Parent;

// Get logical children
foreach (var child in MyMenu.Children)
{
    // Process child
}

// Find an ancestor of a specific type (or null)
var panel = this.FindLogicalAncestorOfType<Panel>();

// Get all logical descendants regardless of descendandy depth
var menuItems = MyMenu.GetLogicalDescendants().OfType<MenuItem>();
```


# Implementation steps:

The plan is executed in the following tasks:

## Step 1: Basic removal

Remove MML and Myrapad

## Step 2: Add XAML compiler

Implement a basic XAML compiler based on https://github.com/kekekeks/XamlX/tree/master 