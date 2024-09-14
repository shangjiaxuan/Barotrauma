# Introduction

**[LuaCsForBarotrauma](https://github.com/evilfactory/LuaCsForBarotrauma)** also provides a way to write custom CSharp mods for %Barotrauma. Since this type of mod is not sandboxed, it's disabled by default and must be enabled in the LuaCs Settings menu.

If you are wondering how to get started with CSharp modding, this guide is for you. It will walk you through the process of setting up a new C# mod project, and provide you with a simple example to get you started.

## Enabling

To enable CSharp mod loading, you will have to go to your mainmenu, click top left LuaCs Settings, and enable the `Enable CSharp` option. If you do not have CSharp enabled and you join a server that has CSharp mods, you will receive a popup asking if you want to enable it for that session.

You can also enable by setting the `EnableCSharp` option in the `LuaCsConfig.xml` file found in the game root folder to `true`.
```xml
<?xml version="1.0" encoding="utf-8"?>
<LuaCsSetupConfig EnableCsScripting="true" TreatForcedModsAsNormal="false" PreferToUseWorkshopLuaSetup="false" DisableErrorGUIOverlay="false" HideUserNames="true" />
```

## Development

- [In-memory CSharp Mod](md_manual_inmemorymod.html)
- [Assembly CSharp Mod](md_manual_assemblymod.html)
- [Using Harmony](md_manual_harmony.html)

