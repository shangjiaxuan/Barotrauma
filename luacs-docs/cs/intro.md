# Barotrauma C# modding guide

## Introduction

C# modding is part of the  **[Lua For Barotrauma](https://github.com/evilfactory/LuaCsForBarotrauma)** mod and requires the package `Cs For Barotrauma` to be turned on ([steam workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2795927223)), in some cases the package is not needed and a prompt in game will be shown to enable it automatically.

This modding requires strict source structure, but comes with the benefits of being handled natively by game engine, witch removes many hurdles with type casting or similar issues.


## Setting Up Assembly Mod

---
#### Part 1: General Project Setup

1. Download [Visual Studio](https://visualstudio.microsoft.com/vs/). You may use another version of VS, or another IDE like JetBrains Rider. However, it must support `.NET 6` as a platform/SDK target and there is no guarantee that the project will function as expected, although there should be little issue.
- Important: You must have `Visual Studio Build Tools` 2019 or later installed with the Individual Component under `SDKs, libraries, and frameworks` named `Visual Studio SDK Build Tools Core` installed.
2. Git Clone or Download the following:
 - [Refs.zip](https://github.com/MapleWheels/LuaCsForBarotrauma/releases): This contains all references used in the project. Choose the one from the latest release on the Github repo.
 - [VSProjectSkeleton](https://github.com/MapleWheels/VSProjectSkeleton): This contains the skeleton project that we will be using as the basis for this tutorial. Most things that need to be setup have already been done.
 - If you do not have Git Tools installed, it is recommended that you get familiar with them as it makes life easier and is required for Part 2 (optional). [Download and install it](https://git-scm.com/downloads).
3. Extract the VS Project Skeleton to a folder that you are working out of.\
 -- IMPORTANT: This folder should NOT be in your Barotrauma/Luatruama local mods folder!
4. Copy the contents of the Refs.zip into the `/Refs/` folder in the VS Skeleton Project. Your project should then have the below structure.
```asciidoc
./MyModName.sln
./.gitignore
./README.txt
./Build.props

=== Assets Here ===
./Assets
----/Content/ <<=== Your Non-CSharp Content Goes Here (including Lua, XML)!
Ex:----/Lua/...
Ex:----/Items/...
----/filelist.xml
----/RunConfig.xml

=== Client CSharp Code Here ===
./ClientProject
----/ClientSource/...
----/LinuxClient.csproj
----/OSXClient.csproj
----/WindowsClient.csproj

=== Server CSharp Code Here ===
./ServerProject
----/ServerSource/...
----/LinuxServer.csproj
----/OSXServer.csproj
----/WindowsServer.csproj

=== Shared CSharp Code Here ===
./SharedProject
----/SharedSource/...

./Refs
----/Linux/Barotrauma.dll
----/Linux/DedicatedServer.dll
----/Windows/Barotrauma.dll
----/Windows/DedicatedServer.dll
----/OSX/Barotrauma.dll
----/OSX/DedicatedServer.dll
----/0Harmony.dll
----/Farseer.NetStandard.dll
----/Lidgren.NetStandard.dll
----/Mono.Cecil.dll
----/MonoGame.Framework.Linux.NetStandard.dll
----/MonoGame.Framework.MacOS.NetStandard.dll
----/MonoGame.Framework.Windows.NetStandard.dll
----/MonoMod.Common.dll
----/MoonSharp.Interpreter.dll
----/XNATypes.dll
```

5. Rename the Skeleton Project folder to the name of your mod.
6. Open up the Solution file (`.sln`) from the Skeleton Project in Visual Studio. Just verify that there are not errors related to "Unable to find Reference", this means that all assemblies in `/Refs` have be found successfully.
7. Rename the `MyModName.sln` file to the name of your mod. IE: `ModdingToolkit.sln`.
8. Open the `.sln` Solution file. In your IDE, you will need to do the following for all `.csproj` files:
```asciidoc
- Assets.csproj
- LinuxClient.csproj
- LinuxServer.csproj
- OSXClient.csproj
- OSXServer.csproj
- WindowsClient.csproj
- WindowsServer.csproj
```
 - A. Open the Project Configuration for the `.csproj` file by right-clicking and selecting `Properties`.
 - B. Change the `AssemblyName` to the name of your mod WITHOUT SPACES OR SPECIAL CHARACTERS. Example: `ModdingToolkit`
 - C. Change the `Root Namespace` to either your mod's Assembly Name or mod's short version without spaces in plain English. Example: `ModdingToolkit`.

---
#### Part 2 (Optional, Recommended): Setting up a debug build of LuaCsForBarotrauma

This will give us a debug build of LuaCsForBarotrauma. A debug build gives us access to many tools as well as the `DEBUG` symbol for writing test/print code that will not be run in the release version.

1. Clone the [LuaCsForBarotrauma](https://github.com/evilfactory/LuaCsForBarotrauma) repository to your local drive. If you do not have Git Tools installed, please [download and install it](https://git-scm.com/downloads).
- Important: When cloning, use the command `git clone --recurse-submodules --remote-submodules https://github.com/evilfactory/LuaCsForBarotrauma.git`. This will download the submodules automatically.

2. Open up the LuaCs Solution in your IDE based on your Operating System, one of:
```asciidoc
- WindowsSolution.sln
- MacSolution.sln
- LinuxSolution.sln
 ```
-- NOTE: This tutorial assumes that you are using the `WindowsSolution.sln`. For other platforms, the naming of files may be slightly different (MacXXX, LinuxXXX, where 'XXX' is either "Client" or "Server").\
\
3. In the Project Settings for `WindowsClient` and `WindowsServer`, you want to change the `Platform Target` from `Any CPU` to `x64`. This is necessary for OpenAL code to build successfully.\
\
4. Build the whole Solution (`Build -> Build Solution`). This will create the necessary dependencies from libraries and make sure that there are no errors at this point.\
\
5. For both the `WindowsClient` and `WindowsServer` projects, set their output/build type to `DEBUG` (should be in a drop-down menu at the top next to a green play button).\
\
6. Select `Build Solution`. This will generate Debug builds for use. This debug build will now exist in `./Barotrauma/bin/DebugXXX/net6.0/` where `XXX` is based on your Solution choice. IE. `./Barotrauma/bin/DebugWindows/net6.0`.\
\
7. Make the local folder in Barotrauma/LocalMods/ for your mod: 
- A. Navigate into the Client Debug Build folder (or game folder if you are coming from Part 3, Step 3) and then into `/LocalMods`.
- B. CREATE a folder with the name of your mod. Ideally, it should match the name of your `.sln` file with the extension.
- C. COPY the path to this directory (with your mod) and save it in a sticky note or .txt file. We will be using it shortly.\
\
---
#### Part 3: Setting up MSBuild to Copy Files into Local Mods.

You will have to do the following for the `Build.props` file found in the main project directory:

1. Open the file in your Text Editor or IDE of choice.

2. Locate the line `ModDeployDir`, IE:
```xml
<PropertyGroup>
    <!-- IMPORTANT: Should point to <Barotrauma_Install>\LocalMods\<MyModName>\ -->
    <!-- IMPORTANT: ModDeplyDir Path must end with '\' -->
    <ModDeployDir>..\LUATRAMA_DEBUG_LOCALMODS_MYMODDIR\</ModDeployDir>
    <!-- IMPORTANT: Avoid the use of special (IE: / : ; , \ < > ?) (periods "." are good) and non-english characters in the AssemblyName.-->
    <AssemblyName>MyModName</AssemblyName>
</PropertyGroup>
```

3. Change the `ModDeployDir` value from `..\LUATRAMA_DEBUG_LOCALMODS_MYMODDIR\` to the path to your mod's folder in the `/LocalMods` folder for LuaCsForBarotrauma from `Part 2, Step 7`. 
- IMPORTANT: The value must end with `/` or `\ ` . IE: `\LocalMods\ModdingToolkit\`.
- Note: If you did not complete Part 2, then complete only Part 2, Step 7 but use your vanilla Barotrauma installation location instead.
- Note: If you do not want either the `Client` plugin or the `Server` plugin (IE. Client-side or Server-side ONLY mod), then you must delete all projects that end with `Server`. Example: `LinuxServer`, `WindowsServer`. Warning, this is generally irreversible and will require you to setup new Cs Projects if you want a client in the future.

4. Replace `MyModName` with a valid assembly name, this should be similar to your mod name but does not need to match. This name should:
- Not include spaces.
- Not include special characters except periods ( . ), which are allowed.
- Use english characters.
- Recommended: Follow the convention of `ModName` or `PackageName.LibraryName` for libraries, IE: `ModdingToolkit.GUI`.

---
#### Part 4: Adding Launch Configurations for Debug-Mode Barotrauma Client and Server to the IDE.

This part makes life very easy and gives you access to Edit and Continue. It'll let you Debug your build as you go and launch Barotrauma from the IDE. Note that this is IDE-specific so please follow the links for your IDE while doing the below:\
-- Make two launch options;
- A. One should target `Barotrauma.exe` or `Barotrauma.dll`, name it `Client`. 
- B. The other should target `DedicatedServer.exe` or `DedicatedServer.dll` and name it `Server`.

Follow the link;

- For [Jetbrains Rider IDE](https://www.jetbrains.com/help/rider/Run_Debug_Configuration.html#envvars-progargs) (what I use).
- For [Visual Studio 2022](https://learn.microsoft.com/en-us/visualstudio/debugger/project-settings-for-csharp-debug-configurations-dotnetcore?view=vs-2022#launch-profile-net-core-net-5) IDE.

---
#### (Optional) Part 5: Adding Script Dependencies

This part is optional and only to be used if your mod relies on the assemblies/code from another mod to run/compile. This will allow you to add Script/Code ContentPackages as dependencies to make sure they load first.

1. Open `<ProjectDirectory>/Assets/RunConfig.cfg` it should look like this:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunConfig>
    <!--Options: "None" / "Forced" (always) / "Standard" (when enabled)-->
    <Server>Standard</Server>
    <Client>Standard</Client>
</RunConfig>
```

2. Add a `Dependencies` section/node/element:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunConfig>
    <!--Options: "None" / "Forced" (always) / "Standard" (when enabled)-->
    <Server>Standard</Server>
    <Client>Standard</Client>
    <Dependencies>
     
    </Dependencies>
</RunConfig>
```

3. Add the STEAMID **or** Name of your dependencies surrounded by `<Dependency></Dependency>` elements as follows:
- `<PackageName>Name</PackageName>` for name matching
- `<SteamWorkshopId>000000</SteamWorkshopId>` and replace zeroes with the dependency's workshop steam id.
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunConfig>
    <!--Options: "None" / "Forced" (always) / "Standard" (when enabled)-->
    <Server>Standard</Server>
    <Client>Standard</Client>
    <Dependencies>
        <Dependency>
            <!-- You do not need both of these, the SteamWorkshopId is usually enough -->
            <SteamWorkshopId><!--ID goes here--></SteamWorkshopId>
            <PackageName><!--Name goes here--></PackageName>
        </Dependency>
        <Dependency> <!-- Dependency 2 Example -->
            <PackageName>modding toolkit</PackageName>
        </Dependency>
    </Dependencies>
</RunConfig>
```

And you're good to go!


## Setting Up Simple Mod

The main star of the show is Barotrauma.ACsMod class. It is what most of your mods will use to hook game methods, and execute custom code. If you are planning on creating new components it's possible to just inherit from ItemComponent.

### Important features

- Mods have full access to Harmony to allow you to perform any patches you wish.
- All utility classes can be accessed either by their type (i.e that have name that starts with `LuaCs...`) or through `GameMain.LuaCs` property (refer to [class documentation](#ltcd)).
- All C# code files must be located in `<mod_root>/CSharp/*` otherwise they won't be compiled
- To configure **server / client** execution behaviour create `RunConfig.xml` in `CSharp` directory, like is shown below (run types are `Standard`, `Forced` and `None`)
- Additionally you can specify what code runs where by either ***filepath*** or ***pre-processor statements***
- In case of filepath, your files must be located in either `CSharp/Shared/*`, `CSharp/Server/*` or `CSharp/Client/*`, for *shared* code, *server-side* code or *client-side* code respectively (in any other case, the code is assumed to be shared)
- I case of pre-processor, you can use `SERVER` or `CLIENT` definitions to separate code into *server-side* code and *client-side* code respectively

### Code example

A generic C# mod boilerplate:

File-tree:

```
 <mod_root>/
 ├─ CSharp/
 │  ├─ RunConfig.xml
 │  ├─ Shared/ExampleMod.cs
 │  ├─ Server/ExampleMod.cs
 │  └─ Client/ExampleMod.cs
 └─ dummyitem.xml
```

____
`<mod_root>/CSharp/RunConfig.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunConfig>
  <Server>Standard</Server>
  <Client>Standard</Client>
</RunConfig>
```

____
`<mod_root>/CSharp/Shared/ExampleMod.cs`

```csharp
using System;
using Barotrauma;

namespace ExampleNamespace {
    partial class ExampleMod: ACsMod {
        public ExampleMod() {
            // shared code
            ...

            #if SERVER
                InitServer();
            #elif CLIENT
                InitClient();
            #endif
        }

        public override void Stop() {
            // stopping code, e.g. save custom data
             #if SERVER
                // server-side code
                ...
            #elif CLIENT
                // client-side code
                ...
            #endif
        }
    }
}
```

____
`<mod_root>/CSharp/Server/ExampleMod.cs`

```csharp
using System;
using Barotrauma;

namespace ExampleNamespace {
    partial class ExampleMod {
        public void InitServer() {
            // server-side initialization
            ...
        }
    }
}
```

____
`<mod_root>/CSharp/Client/ExampleMod.cs`

```csharp
using System;
using Barotrauma;

namespace ExampleNamespace {
    partial class ExampleMod {
        public void InitClient() {
            // client-side initialization
            ...
        }
    }
}
```

____
`<mod_root>/dummyitem.xml`

```xml
<Items></Items> 
```

## Links to class documentation {#ltcd}

- [Server documentation](../baro-server/html/index.html)
- [Client documentation](../baro-client/html/index.html)
