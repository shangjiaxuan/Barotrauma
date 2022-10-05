using System;
using System.IO;
using System.Linq;

namespace Barotrauma
{
    static partial class LuaCsInstaller
    {
        public static void Uninstall()
        {
            if (!File.Exists("Temp/Original/Barotrauma.dll"))
            {
                new GUIMessageBox("Error", "Error: Temp/Original/Barotrauma.dll not found, Github version? Use Steam validate files instead.");

                return;
            }

            var msg = new GUIMessageBox("Confirm", "Are you sure you want to remove Client-Side LuaCs?", new LocalizedString[2] { TextManager.Get("Yes"), TextManager.Get("Cancel") });

            msg.Buttons[0].OnClicked = (GUIButton button, object obj) =>
            {
                msg.Close();

                string[] filesToRemove = new string[]
                {
                    "Barotrauma.dll", "Barotrauma.deps.json",
                    "System.Reflection.Metadata.dll", "System.Collections.Immutable.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll"
                };
                try
                {
                    CreateMissingDirectory();

                    foreach (string file in filesToRemove)
                    {
                        File.Move(file, "Temp/ToDelete/" + file, true);
                        File.Move("Temp/Original/" + file, file, true);
                    }
                }
                catch (Exception e)
                {
                    new GUIMessageBox("Error", $"{e} {e.InnerException} \nTry verifying files instead.");
                    return false;
                }

                new GUIMessageBox("Restart", "Restart your game to apply the changes. If the mod continues to stay active after the restart, try verifying games instead.");

                return true;
            };

            msg.Buttons[1].OnClicked = (GUIButton button, object obj) =>
            {
                msg.Close();
                return true;
            };
        }

        public static void CheckUpdate()
        {
            if (!File.Exists(LuaCsSetup.VersionFile)) { return; }

            ContentPackage luaPackage = LuaCsSetup.GetPackage("Lua For Barotrauma (with xpath patch)");
            string luaCsPath = Path.GetDirectoryName(luaPackage.Path);

            if (luaPackage == null) { return; }

            string clientVersion = File.ReadAllText(LuaCsSetup.VersionFile);
            string workshopVersion = luaPackage.ModVersion;

            if (clientVersion == workshopVersion) { return; }

            string additional = LuaCsSetup.GetPackage("CsForBarotrauma", false, true) == null ? "" : "Cs";

            var msg = new GUIMessageBox($"Lua{additional} Update", $"Your Lua{additional} client version is different from the version found in the Lua{additional}ForBarotrauma workshop files. Do you want to update?\n\n Client Version: {clientVersion}\n Workshop Version: {workshopVersion}", 
                new LocalizedString[2] { TextManager.Get("Yes"), TextManager.Get("Cancel") });

            msg.Buttons[0].OnClicked = (GUIButton button, object obj) =>
            {
                string[] filesToUpdate = new string[]
                {
                    "Barotrauma.dll", "Barotrauma.deps.json",
                    "0harmony.dll", "Mono.Cecil.dll",
                    "Sigil.dll",
                    "Mono.Cecil.Mdb.dll", "Mono.Cecil.Pdb.dll",
                    "Mono.Cecil.Rocks.dll", "MonoMod.Common.dll",
                    "MoonSharp.Interpreter.dll",

                    "Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp.dll",
                    "Microsoft.CodeAnalysis.CSharp.Scripting.dll", "Microsoft.CodeAnalysis.Scripting.dll",

                    "System.Reflection.Metadata.dll", "System.Collections.Immutable.dll",
                    "System.Runtime.CompilerServices.Unsafe.dll"
                };

                filesToUpdate = filesToUpdate.Concat(Directory.EnumerateFiles(luaCsPath, "*.dll", SearchOption.AllDirectories)
                        .Concat(Directory.EnumerateFiles(luaCsPath, "*.json", SearchOption.TopDirectoryOnly))
                        /*.Where(s => s.Contains("mscordaccore_amd64_amd64_4."))*/.Select(s => Path.GetFileName(s))).ToArray();

                try
                {
                    CreateMissingDirectory();

                    foreach (string file in filesToUpdate)
                    {
                        try
                        {
                            File.Move(file, "Temp/Old/" + file, true);
                            File.Copy(Path.Combine(luaCsPath, "Binary", file), file, true);
                        }
                        catch (Exception e)
                        {
                            DebugConsole.ThrowError($"Failed to update file {e}");
                        }

                    }

                    File.WriteAllText(LuaCsSetup.VersionFile, workshopVersion);
                }
                catch (Exception e)
                {
                    new GUIMessageBox("Failed", $"Failed to update, error: {e}");

                    msg.Close();
                    return true;
                }

                new GUIMessageBox("Restart", $"Lua{additional} updated! Restart your game to apply the changes.");

                msg.Close();
                return true;
            };

            msg.Buttons[1].OnClicked = (GUIButton button, object obj) =>
            {
                msg.Close();
                return true;
            };
            
        }
    }
}
