using Barotrauma.Networking;
using System;
using System.IO;
using System.Linq;

namespace Barotrauma
{
    static partial class LuaCsInstaller
    {
        public static void Install()
        {
            ContentPackage luaPackage = LuaCsSetup.GetPackage("Lua For Barotrauma (with xpath patch)");

            if (luaPackage == null)
            {
                GameMain.Server.SendChatMessage("Couldn't find the Lua For Barotrauma package.", ChatMessageType.ServerMessageBox);
                return;
            }

            try
            {
                string path = Path.GetDirectoryName(luaPackage.Path);

                string[] filesToCopy = new string[]
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
                filesToCopy = filesToCopy.Concat(Directory.EnumerateFiles(path, "*.dll", SearchOption.AllDirectories)
                     .Concat(Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly))
                    /*.Where(s => s.Contains("mscordaccore_amd64_amd64_4."))*/.Select(s => Path.GetFileName(s))).ToArray();

                CreateMissingDirectory();

                File.Move("Barotrauma.dll", "Temp/Original/Barotrauma.dll", true);
                File.Move("Barotrauma.deps.json", "Temp/Original/Barotrauma.deps.json", true);

                File.Move("System.Reflection.Metadata.dll", "Temp/Original/System.Reflection.Metadata.dll", true);
                File.Move("System.Collections.Immutable.dll", "Temp/Original/System.Collections.Immutable.dll", true);
                File.Move("System.Runtime.CompilerServices.Unsafe.dll", "Temp/Original/System.Runtime.CompilerServices.Unsafe.dll", true);

                foreach (string file in filesToCopy)
                {
                    if (File.Exists(file))
                    {
                        File.Move(file, "Temp/ToDelete/" + file, true);
                    }
                    File.Copy(Path.Combine(path, "Binary", file), file, true);
                }

                File.WriteAllText(LuaCsSetup.VersionFile, luaPackage.ModVersion);
                File.WriteAllText("LuaDedicatedServer.bat", "\"%LocalAppData%/Daedalic Entertainment GmbH/Barotrauma/WorkshopMods/Installed/2856758496/Binary/DedicatedServer.exe\"");
            }
            catch (UnauthorizedAccessException e)
            {
                GameMain.LuaCs.PrintError("You seem to already have Client Side Lua installed, if you are trying to reinstall, make sure uninstall it first (mainmenu button located top left).", LuaCsMessageOrigin.LuaCs);

                return;
            }
            catch (Exception e)
            {
                GameMain.LuaCs.HandleException(e, LuaCsMessageOrigin.LuaCs);

                return;
            }

            GameMain.Server.SendChatMessage("Client-Side Lua installed, restart your game to apply changes.", ChatMessageType.ServerMessageBox);
        }
    }
}
