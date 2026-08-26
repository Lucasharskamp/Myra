## Overview
[![Nuget](https://img.shields.io/nuget/dt/Myra)](https://www.nuget.org/packages/Myra/)
[![Build & Publish Beta](https://github.com/rds1983/Myra/actions/workflows/build-and-publish-beta.yml/badge.svg)](https://github.com/rds1983/Myra/actions/workflows/build-and-publish-beta.yml)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

_Myra.Xaml_ is a fork of [Myra](https://github.com/MyraUI/Myra), a UI library for [MonoGame](http://www.monogame.net/), [FNA](https://github.com/FNA-XNA/FNA), and [Stride](https://github.com/stride3d/stride).  

It sets out to solve two problems:
1. Myra is regarded as the best UI library for MonoGame, but suffers from the fact it is heavy on reflection, preventing one from [deploying to consoles](https://docs.monogame.net/articles/getting_started/preparing_for_consoles.html).
2. Myra's own MML lacks proper IDE tooling and requires engineers to learn new things; if one already knows XAML (from Windows.Forms, WPF, Xamarin/MAUI, AvaloniaUI, etc.) it makes the transition a lot easier, which benefits small game studios. It also provides the default XAML features which MML cannot provide.

Other than not using MML and runtime reflection, _Myra.Xaml_ contains the same features as _Myra_, but rather than using MML it uses XAML.

## How does it work?

- The XAML is turned into _Common Intermediate Language_ (CIL) code and written onto the project's DLL at compile-time, using [XamlX](https://github.com/kekekeks/XamlX) and [Mono.Cecil](https://github.com/jbevain/cecil) 
	- _XamlX_ is an open-source library, built as the Xaml parser behind AvaloniaUI but also intended to be used elsewhere.
	- _Mono.Cecil_ is a popular open-source library used to inspect, modify, and generate .NET assemblies and programs in CIL without loading them into standard runtime reflection. It is used by the Unity Engine and many debuggers, obfuscators and profilers, amongst others.
## Myra's Documentation
[https://myraui.github.io/Myra/](https://myraui.github.io/Myra/)

## Support
Use the following resources if you need help with Myra or have any questions:
* [Myra Discord](https://discord.gg/ZeHxhCY)

## Gallery
All Widgets Sample
![](/images/AllWidgetsSample.png)

Commodore 64 Skin
![](/images/CustomStylesheetSample.png)

## Credits

* [Myra](https://github.com/MyraUI/Myra)
* [XamlX](https://github.com/kekekeks/XamlX)
* [Mono.Cecil](https://github.com/jbevain/cecil) 
* [MonoGame](http://www.monogame.net/)
* [FNA](https://github.com/FNA-XNA/FNA)
* [Stride](https://github.com/stride3d/stride)
* [MonoGame.Extended](https://github.com/craftworkgames/MonoGame.Extended)
* [VisUI](https://github.com/kotcrab/vis-editor/wiki/VisUI)
* [LibGDX](http://libgdx.badlogicgames.com/)
* [Cyotek.Drawing.BitmapFont](https://github.com/cyotek/Cyotek.Drawing.BitmapFont)
* [stb](https://github.com/nothings/stb)
* [TextCopy](https://github.com/SimonCropp/TextCopy)
