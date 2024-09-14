# In-memory CSharp Mod

One type of CSharp mod that can be created is an in-memory mod. This type of mod is compiled in memory and does not require the distribution of dll files. This type of mod is relatively easy to create and is useful for small mods that do not require a lot of complexity.

The disadvantage of in-memory mods is that they can take up a chunk of initialization time, as the mod must be compiled every time the mod is loaded. This type of mod also can't be easily debugged in your IDE or take full advantage of the entire .NET ecosystem.

## Getting Started

---

First you need to create a new Barotrauma mod as you are already familiar with. Then you need to create a folder named `CSharp` in the root of your mod folder. This is where you will put all your C# code files. All .cs source files in this folder will be compiled into the mod.

Source files inside the CSharp/Server and CSharp/Client folders will only be compiled for the server and client respectively. Files in the CSharp/Shared folder will be compiled for both the server and client.

### Example mod

____
`<mod_root>/CSharp/Shared/ExampleMod.cs`

```cs
using System;
using Barotrauma;


// This is required so that the .NET runtime doesn't complain about you trying to access internal Types and Members
[assembly: IgnoreAccessChecksTo("Barotrauma")]
[assembly: IgnoreAccessChecksTo("BarotraumaCore")]
[assembly: IgnoreAccessChecksTo("DedicatedServer")]
namespace ExampleNamespace {
    partial class ExampleMod : IAssemblyPlugin {
        public void Initialize()
        {
            // When your plugin is loading, use this instead of the constructor
            // Put any code here that does not rely on other plugins.
            LuaCsLogger.Log("ExampleMod loaded!");
        }

        public void OnLoadCompleted()
        {
            // After all plugins have loaded
            // Put code that interacts with other plugins here.
        }

        public void PreInitPatching()
        {
            // Not yet supported: Called during the Barotrauma startup phase before vanilla content is loaded.
        }

        public void Dispose()
        {
            // Cleanup your plugin!
            LuaCsLogger.Log("ExampleMod disposed!");
        }
    }
}
```