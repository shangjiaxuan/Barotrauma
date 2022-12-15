using System;
using System.IO;
using Barotrauma.Networking;
using MoonSharp.Interpreter;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter.Interop;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading;
using LuaCsCompatPatchFunc = Barotrauma.LuaCsPatch;
using System.Diagnostics;

[assembly: InternalsVisibleTo(Barotrauma.CsScriptBase.CsScriptAssembly, AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(Barotrauma.CsScriptBase.CsOneTimeScriptAssembly, AllInternalsVisible = true)]
namespace Barotrauma
{
    class LuaCsSetupConfig
    {
        public bool FirstTimeCsWarning = true;

        public LuaCsSetupConfig() { }
    }

    partial class LuaCsSetup
    {
        public const string LuaSetupFile = "Lua/LuaSetup.lua";
        public const string VersionFile = "luacsversion.txt";
        public static ContentPackageId LuaForBarotraumaId = new SteamWorkshopId(2856758496);
        public static ContentPackageId CsForBarotraumaId = new SteamWorkshopId(2795927223);


        private const string configFileName = "LuaCsSetupConfig.xml";

#if SERVER
        public const bool IsServer = true;
        public const bool IsClient = false;
#else
        public const bool IsServer = false;
        public const bool IsClient = true;
#endif

        private static int executionNumber = 0;


        public Script Lua { get; private set; }
        public CsScriptRunner CsScript { get; private set; }
        public LuaScriptLoader LuaScriptLoader { get; private set; }

        public LuaGame Game { get; private set; }
        public LuaCsHook Hook { get; private set; }
        public LuaCsTimer Timer { get; private set; }
        public LuaCsNetworking Networking { get; private set; }
        public LuaCsSteam Steam { get; private set; }
        public LuaCsPerformanceCounter PerformanceCounter { get; private set; }

        public LuaCsModStore ModStore { get; private set; }
        private LuaRequire require { get; set; }

        public CsScriptLoader CsScriptLoader { get; private set; }
        public LuaCsSetupConfig Config { get; private set; }

        internal LuaCsExceptionHandler ExceptionHandler { get; set; }
        internal LuaCsMessageLogger MessageLogger { get; set; }

        public LuaCsSetup()
        {
            MessageLogger = DefaultMessageLogger;
            ExceptionHandler = DefaultExceptionHandler;

            Hook = new LuaCsHook(this);
            ModStore = new LuaCsModStore();

            Game = new LuaGame();
            Networking = new LuaCsNetworking();
        }

        public void UpdateConfig()
        {
            FileStream file;
            if (!File.Exists(configFileName)) file = File.Create(configFileName);
            else file = File.Open(configFileName, FileMode.Truncate, FileAccess.Write);
            LuaCsConfig.Save(file, Config);
            file.Close();
        }

        /// <summary>
        /// due to there's a race on the process and the unloaded AssemblyLoadContexts,
        /// should recreate runner after the script runs
        /// </summary>
        public void RecreateCsScript()
        {
            CsScript = new CsScriptRunner(CsScript.setup);
            Lua.Globals["CsScript"] = CsScript;
        }

        public static ContentPackage GetPackage(ContentPackageId id, bool fallbackToAll = true, bool useBackup = false)
        {
            foreach (ContentPackage package in ContentPackageManager.EnabledPackages.All)
            {
                if (package.UgcId.ValueEquals(id))
                {
                    return package;
                }
            }

            if (fallbackToAll)
            {
                foreach (ContentPackage package in ContentPackageManager.LocalPackages)
                {
                    if (package.UgcId.ValueEquals(id))
                    {
                        return package;
                    }
                }

                foreach (ContentPackage package in ContentPackageManager.AllPackages)
                {
                    if (package.UgcId.ValueEquals(id))
                    {
                        return package;
                    }
                }
            }

            if (useBackup && ContentPackageManager.EnabledPackages.BackupPackages.Regular != null)
            {
                foreach (ContentPackage package in ContentPackageManager.EnabledPackages.BackupPackages.Regular.Value)
                {
                    if (package.UgcId.ValueEquals(id))
                    {
                        return package;
                    }
                }
            }

            return null;
        }

        internal void HandleException(Exception ex, LuaCsMessageOrigin origin)
        {
            this.ExceptionHandler?.Invoke(ex, origin);
        }

        private DynValue DoFile(string file, Table globalContext = null, string codeStringFriendly = null)
        {
            if (!LuaCsFile.CanReadFromPath(file))
            {
                throw new ScriptRuntimeException($"dofile: File access to {file} not allowed.");
            }

            if (!LuaCsFile.Exists(file))
            {
                throw new ScriptRuntimeException($"dofile: File {file} not found.");
            }

            return Lua.DoFile(file, globalContext, codeStringFriendly);
        }

        private DynValue LoadFile(string file, Table globalContext = null, string codeStringFriendly = null)
        {
            if (!LuaCsFile.CanReadFromPath(file))
            {
                throw new ScriptRuntimeException($"loadfile: File access to {file} not allowed.");
            }

            if (!LuaCsFile.Exists(file))
            {
                throw new ScriptRuntimeException($"loadfile: File {file} not found.");
            }

            return Lua.LoadFile(file, globalContext, codeStringFriendly);
        }

        public DynValue CallLuaFunction(object function, params object[] args)
        {
            // XXX: `lua` might be null if `LuaCsSetup.Stop()` is called while
            // a patched function is still running.
            if (Lua == null) return null;

            lock (Lua)
            {
                try
                {
                    return Lua.Call(function, args);
                }
                catch (Exception e)
                {
                    HandleException(e, LuaCsMessageOrigin.LuaMod);
                }
                return null;
            }
        }

        private void SetModulePaths(string[] str)
        {
            LuaScriptLoader.ModulePaths = str;
        }

        public void Update()
        {
            Timer?.Update();
            Steam?.Update();

#if CLIENT
            Stopwatch luaSw = new Stopwatch();
            luaSw.Start();
#endif
            Hook?.Call("think");
#if CLIENT
            luaSw.Stop();
            GameMain.PerformanceCounter.AddElapsedTicks("Think Hook", luaSw.ElapsedTicks);
#endif
        }

        public void Stop()
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == CsScriptBase.CsScriptAssembly).SelectMany(assembly => assembly.GetTypes()))
            {
                UserData.UnregisterType(type, true);
            }

            foreach (var mod in ACsMod.LoadedMods.ToArray())
            {
                mod.Dispose();
            }
            
            ACsMod.LoadedMods.Clear();

            if (Thread.CurrentThread == GameMain.MainThread) 
            {
                Hook?.Call("stop");
            }

            Game?.Stop();

            Hook.Clear();
            ModStore.Clear();
            Game = new LuaGame();
            Networking = new LuaCsNetworking();
            Timer = new LuaCsTimer();
            Steam = new LuaCsSteam();
            PerformanceCounter = new LuaCsPerformanceCounter();
            LuaScriptLoader = null;
            Lua = null;
            CsScript = null;
            Config = null;

            if (CsScriptLoader != null)
            {
                CsScriptLoader.Clear();
                CsScriptLoader.Unload();
                CsScriptLoader = null;
            }
        }

        public void Initialize()
        {
            Stop();

            PrintMessage("Lua! Version " + AssemblyInfo.GitRevision);

            if (File.Exists(configFileName))
            {
                using (var file = File.Open(configFileName, FileMode.Open, FileAccess.Read))
                    Config = LuaCsConfig.Load<LuaCsSetupConfig>(file);
            }
            else
            {
                Config = new LuaCsSetupConfig();
            }

            bool csActive = GetPackage(CsForBarotraumaId, false, true) != null;

            LuaScriptLoader = new LuaScriptLoader();
            LuaScriptLoader.ModulePaths = new string[] { };

            RegisterLuaConverters();

            Lua = new Script(CoreModules.Preset_SoftSandbox | CoreModules.Debug);
            Lua.Options.DebugPrint = PrintMessage;
            Lua.Options.ScriptLoader = LuaScriptLoader;
            Lua.Options.CheckThreadAccess = false;
            Script.GlobalOptions.ShouldPCallCatchException = (Exception ex) => { return true; };
            CsScript = new CsScriptRunner(this);

            require = new LuaRequire(Lua);

            Game = new LuaGame();
            Networking = new LuaCsNetworking();
            Timer = new LuaCsTimer();
            Steam = new LuaCsSteam();
            PerformanceCounter = new LuaCsPerformanceCounter();
            Hook.Initialize();
            ModStore.Initialize();

            UserData.RegisterType<LuaCsConfig>();
            UserData.RegisterType<LuaCsAction>();
            UserData.RegisterType<LuaCsFile>();
            UserData.RegisterType<LuaCsCompatPatchFunc>();
            UserData.RegisterType<LuaCsPatchFunc>();
            UserData.RegisterType<LuaCsConfig>();
            UserData.RegisterType<CsScriptRunner>();
            UserData.RegisterType<LuaGame>();
            UserData.RegisterType<LuaCsTimer>();
            UserData.RegisterType<LuaCsFile>();
            UserData.RegisterType<LuaCsNetworking>();
            UserData.RegisterType<LuaCsSteam>();
            UserData.RegisterType<LuaUserData>();
            UserData.RegisterType<LuaCsPerformanceCounter>();
            UserData.RegisterType<IUserDataDescriptor>();

            Lua.Globals["printerror"] = (Action<object>)PrintLuaError;

            Lua.Globals["setmodulepaths"] = (Action<string[]>)SetModulePaths;

            Lua.Globals["dofile"] = (Func<string, Table, string, DynValue>)DoFile;
            Lua.Globals["loadfile"] = (Func<string, Table, string, DynValue>)LoadFile;
            Lua.Globals["require"] = (Func<string, Table, DynValue>)require.Require;

            Lua.Globals["dostring"] = (Func<string, Table, string, DynValue>)Lua.DoString;
            Lua.Globals["load"] = (Func<string, Table, string, DynValue>)Lua.LoadString;

            Lua.Globals["CsScript"] = CsScript;
            Lua.Globals["LuaUserData"] = UserData.CreateStatic<LuaUserData>();
            Lua.Globals["Game"] = Game;
            Lua.Globals["Hook"] = Hook;
            Lua.Globals["ModStore"] = ModStore;
            Lua.Globals["Timer"] = Timer;
            Lua.Globals["File"] = UserData.CreateStatic<LuaCsFile>();
            Lua.Globals["Networking"] = Networking;
            Lua.Globals["Steam"] = Steam;
            Lua.Globals["PerformanceCounter"] = PerformanceCounter;

            Lua.Globals["ExecutionNumber"] = executionNumber;
            Lua.Globals["CSActive"] = csActive;

            Lua.Globals["SERVER"] = IsServer;
            Lua.Globals["CLIENT"] = IsClient;

            if (csActive)
            {
                PrintMessage("Cs! Version " + AssemblyInfo.GitRevision);

                if (Config.FirstTimeCsWarning)
                {
                    Config.FirstTimeCsWarning = false;
                    UpdateConfig();

                    DebugConsole.AddWarning("Cs package active! Cs mods are NOT sandboxed, use it at your own risk!");
                }

                CsScriptLoader = new CsScriptLoader();
                CsScriptLoader.SearchFolders();
                if (CsScriptLoader.HasSources)
                {
                    try
                    {
                        Stopwatch compilationTime = new Stopwatch();
                        compilationTime.Start();
                        var modTypes = CsScriptLoader.Compile();
                        modTypes.ForEach(t =>
                        {
                            try
                            {
                                t.GetConstructor(new Type[] { })?.Invoke(null);
                            }
                            catch (Exception ex)
                            {
                                HandleException(ex, LuaCsMessageOrigin.CSharpMod);
                            }
                        });
                        compilationTime.Stop();
                        PrintCsMessage($"Took {compilationTime.ElapsedMilliseconds}ms to compile and run Cs Scripts.");
                    }
                    catch (Exception ex)
                    {
                        HandleException(ex, LuaCsMessageOrigin.CSharpMod);
                    }
                }

            }


            ContentPackage luaPackage = GetPackage(LuaForBarotraumaId);

            if (File.Exists(LuaSetupFile))
            {
                PrintMessage("Using LuaSetup.lua from the Barotrauma Lua/ folder.");

                try
                {
                    DynValue function = Lua.LoadFile(LuaSetupFile);
                    CallLuaFunction(function, Path.GetDirectoryName(Path.GetFullPath(LuaSetupFile)));
                }
                catch (Exception e)
                {
                    HandleException(e, LuaCsMessageOrigin.LuaMod);
                }
            }
            else if (luaPackage != null)
            {
                PrintMessage("Using LuaSetup.lua from the content package.");

                string path = Path.GetDirectoryName(luaPackage.Path);

                try
                {
                    string luaPath = Path.Combine(path, "Binary/Lua/LuaSetup.lua");
                    CallLuaFunction(Lua.LoadFile(luaPath), Path.GetDirectoryName(Path.GetFullPath(luaPath)));
                }
                catch (Exception e)
                {
                    HandleException(e, LuaCsMessageOrigin.LuaMod);
                }
            }
            else
            {
                PrintLuaError("LuaSetup.lua not found! Lua/LuaSetup.lua, no Lua scripts will be executed or work.");
            }

            executionNumber++;
        }
    }
}
