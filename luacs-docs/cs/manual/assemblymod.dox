# Assembly CSharp Mod

One type of CSharp mod that can be created is an assembly mod. This type of mod is compiled into a .dll file and requires the distribution of dll files.

Assembly mods can be debugged in your favourite IDE and take full advantage of the entire .NET ecosystem. They take an initial effort to set up, but can be more powerful and flexible.

## Getting Started

---

### 1. IDE Setup

You will need an IDE that supports `.NET 6` as a platform/SDK target. Visual Studio 2022 is recommended. You can download it [here](https://visualstudio.microsoft.com/vs/). Other IDEs like JetBrains Rider also work, but there is no guarantee that the project will function as expected, although there should be little issue.

### 2. Project skeleton

Download or clone the following repository https://github.com/MapleWheels/VSProjectSkeleton, this contains the skeleton project that we will be using as the basis for this tutorial. Most things that need to be setup have already been done.
 - If you do not have Git Tools installed, it is recommended that you get familiar with them as it makes life easier. [Download and install it](https://git-scm.com/downloads).

Make sure to extract the contents of the repository to a folder that you can easily access if you decided to manually download the zip file from github. You may also rename the directory to the name of your mod if you want.

### 3. Reference dlls

You also need to download the publicized reference dlls from https://github.com/evilfactory/LuaCsForBarotrauma/releases/download/latest/luacsforbarotrauma_refs.zip. This contains the publicized reference dlls that are required to build the project.

After downloading the zip file, extract the contents to the `Refs` folder in project skeleton directory. The directory structure should look like this:

```asciidoc
./Refs
----/Linux/Barotrauma.dll
----/Linux/DedicatedServer.dll
----/Linux/BarotraumaCore.dll
----/Windows/Barotrauma.dll
----/Windows/DedicatedServer.dll
----/Windows/BarotraumaCore.dll
...other files
```

### 4. Setup the project

First rename the `MyModName.sln` file to the name of your mod. IE: `ExampleMod.sln`.

Then open up the Solution file (`.sln`) from the Skeleton Project in Visual Studio. Just verify that there are not errors related to "Unable to find Reference", this means that all assemblies in `/Refs` have be found successfully.

After that, in your IDE, you will need to do the following for all `.csproj` files:
```asciidoc
- LinuxClient.csproj
- LinuxServer.csproj
- OSXClient.csproj
- OSXServer.csproj
- WindowsClient.csproj
- WindowsServer.csproj
```
 - 1. Open the Project Configuration for the `.csproj` file by right-clicking and selecting `Properties`.
 - 2. Change the `AssemblyName` to the name of your mod WITHOUT SPACES OR SPECIAL CHARACTERS. Example: `SampleMod`
 - 3. Change the `Root Namespace` to either your mod's Assembly Name or mod's short version without spaces in plain English. Example: `SampleMod`.

Note: If you do not want either the `Client` plugin or the `Server` plugin (IE. Client-side or Server-side ONLY mod), then you must delete all projects that end with `Server`. Example: `LinuxServer`, `WindowsServer`. Warning, this is generally irreversible and will require you to setup new Cs Projects if you want a client in the future.

### 5. Setting up MSBuild to Copy Files into Local Mods.

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

3. Change the `ModDeployDir` value from `..\LUATRAMA_DEBUG_LOCALMODS_MYMODDIR\` to the path of your Barotrauma installation's `LocalMods` directory. IE: `C:\Program Files (x86)\Steam\steamapps\common\Barotrauma\LocalMods\MyModName\`.
- IMPORTANT: The value must end with `/` or `\ ` . IE: `...\LocalMods\ModdingToolkit\`.

4. Replace `MyModName` with a valid assembly name, this should be similar to your mod name but does not need to match. This name should:
- Not include spaces.
- Not include special characters except periods ( . ), which are allowed.
- Use english characters.
- Recommended: Follow the convention of `ModName` or `PackageName.LibraryName` for libraries, IE: `ModdingToolkit.GUI`.

### 6. Wrapping up

After setting up the project, you can start coding your mod. The project is already set up to build the mod into the `LocalMods` directory of your Barotrauma installation. You can now build the project and the mod dll files will be copied to the `LocalMods` directory.

## Extras

---

### Setting up a debug build of LuaCsForBarotrauma

This will give us a debug build of LuaCsForBarotrauma. A debug build gives us access to many tools as well as the `DEBUG` symbol for writing test/print code that will not be run in the release version.

1. Clone the [LuaCsForBarotrauma](https://github.com/evilfactory/LuaCsForBarotrauma) repository to your local drive. If you do not have Git Tools installed, please [download and install it](https://git-scm.com/downloads).
- Important: When cloning, use the command `git clone --recurse-submodules --remote-submodules https://github.com/evilfactory/LuaCsForBarotrauma.git`. This will download the submodules automatically.

2. Open up the LuaCs Solution in your IDE based on your Operating System, one of:
```asciidoc
- WindowsSolution.sln
- MacSolution.sln
- LinuxSolution.sln
 ```
-- NOTE: This assumes that you are using the `WindowsSolution.sln`. For other platforms, the naming of files may be slightly different (MacXXX, LinuxXXX, where 'XXX' is either "Client" or "Server").

3. In the Project Settings for `WindowsClient` and `WindowsServer`, you want to change the `Platform Target` from `Any CPU` to `x64`. This is necessary for OpenAL code to build successfully.

4. Build the whole Solution (`Build -> Build Solution`). This will create the necessary dependencies from libraries and make sure that there are no errors at this point.\

5. For both the `WindowsClient` and `WindowsServer` projects, set their output/build type to `DEBUG` (should be in a drop-down menu at the top next to a green play button).\

6. Select `Build Solution`. This will generate Debug builds for use. This debug build will now exist in `./Barotrauma/bin/DebugXXX/net6.0/` where `XXX` is based on your Solution choice. IE. `./Barotrauma/bin/DebugWindows/net6.0`.

Now instead of using your Barotrauma steam installation, you can use the debug build of LuaCsForBarotrauma to test your mods. Simply put your mod in the `LocalMods` directory of the debug build and set the ModDeployDir in the `Build.props` file.