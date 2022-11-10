using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Barotrauma.Networking;
using MoonSharp.Interpreter;
using Microsoft.Xna.Framework;
using MoonSharp.Interpreter.Interop;
using System.IO.Compression;
using HarmonyLib;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;
using System.Threading;
using LuaCsCompatPatchFunc = Barotrauma.LuaCsPatch;

[assembly: InternalsVisibleTo(Barotrauma.CsScriptBase.CsScriptAssembly, AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(Barotrauma.CsScriptBase.CsOneTimeScriptAssembly, AllInternalsVisible = true)]
namespace Barotrauma
{
	class LuaCsSetupConfig
    {
		public bool FirstTimeCsWarning = true;

		public LuaCsSetupConfig() { }
	}

    internal delegate void LuaCsMessageLogger(string prefix, object o);

    internal delegate void LuaCsExceptionHandler(Exception ex, LuaCsMessageOrigin origin);

    internal enum LuaCsMessageOrigin
    {
        LuaCs,
        Unknown,
        LuaMod,
        CSharpMod,
    }

	partial class LuaCsSetup
	{
		public const string LuaSetupFile = "Lua/LuaSetup.lua";
		public const string VersionFile = "luacsversion.txt";

		private const string configFileName = "LuaCsSetupConfig.xml";

#if SERVER
		public const bool IsServer = true;
		public const bool IsClient = false;
#else
		public const bool IsServer = false;
		public const bool IsClient = true;
#endif

		private static int executionNumber = 0;

		private Script lua;
		public Script Lua
        {
            get { return lua; }
        }

		public CsScriptRunner CsScript { get; private set; }

		/// <summary>
		/// due to there's a race on the process and the unloaded AssemblyLoadContexts,
		/// should recreate runner after the script runs
		/// </summary>
		public void RecreateCsScript()
        {
			CsScript = new CsScriptRunner(CsScript.setup);
			lua.Globals["CsScript"] = CsScript;
		}

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


		public static ContentPackage GetPackage(Identifier name, bool fallbackToAll = true, bool useBackup = false)
		{
			foreach (ContentPackage package in ContentPackageManager.EnabledPackages.All)
			{
				if (package.NameMatches(name))
				{
					return package;
				}
			}

			if (fallbackToAll)
			{
				foreach (ContentPackage package in ContentPackageManager.LocalPackages)
				{
					if (package.NameMatches(name))
					{
						return package;
					}
				}

				foreach (ContentPackage package in ContentPackageManager.AllPackages)
				{
					if (package.NameMatches(name))
					{
						return package;
					}
				}
			}

			if (useBackup && ContentPackageManager.EnabledPackages.BackupPackages.Regular != null)
            {
				foreach (ContentPackage package in ContentPackageManager.EnabledPackages.BackupPackages.Regular.Value)
				{
					if (package.NameMatches(name))
					{
						return package;
					}
				}
			}

			return null;
		}

        private void DefaultExceptionHandler(Exception ex, LuaCsMessageOrigin origin)
        {
            switch (ex)
            {
                case NetRuntimeException netRuntimeException:
                    if (netRuntimeException.DecoratedMessage == null)
                    {
                        PrintError(netRuntimeException, origin);
                    }
                    else
                    {
                        // FIXME: netRuntimeException.ToString() doesn't print the InnerException's stack trace...
                        PrintError($"{netRuntimeException.DecoratedMessage}: {netRuntimeException}", origin);
                    }
                    break;
                case InterpreterException interpreterException:
                    if (interpreterException.DecoratedMessage == null)
                    {
                        PrintError(interpreterException, origin);
                    }
                    else
                    {
                        PrintError(interpreterException.DecoratedMessage, origin);
                    }
                    break;
                default:
                    var msg = ex.StackTrace != null
                        ? ex.ToString()
                        : $"{ex}\n{Environment.StackTrace}";
                    PrintError(msg, origin);
                    break;
            }
        }

        internal LuaCsExceptionHandler ExceptionHandler { get; set; }


        internal void HandleException(Exception ex, LuaCsMessageOrigin origin)
        {
            this.ExceptionHandler?.Invoke(ex, origin);
        }

        private static void PrintErrorBase(string prefix, object message, string empty)
        {
            message ??= empty;
            var str = message.ToString();

            for (int i = 0; i < str.Length; i += 1024)
            {
                var subStr = str.Substring(i, Math.Min(1024, str.Length - i));

                var errorMsg = subStr;
                if (i == 0) errorMsg = prefix + errorMsg;

                DebugConsole.ThrowError(errorMsg);

#if SERVER
                if (GameMain.Server != null)
                {
                    foreach (var c in GameMain.Server.ConnectedClients)
                    {
                        GameMain.Server.SendDirectChatMessage(ChatMessage.Create("", errorMsg, ChatMessageType.Console, null, textColor: Color.Red), c);
                    }

                    GameServer.Log(errorMsg, ServerLog.MessageType.Error);
                }
#endif
            }
        }

#if SERVER
        private const string LOG_PREFIX = "SV";
#else
        private const string LOG_PREFIX = "CL";
#endif

        // TODO: deprecate this (in an effort to get rid of as much global state as possible)
        public void PrintError(object o, LuaCsMessageOrigin origin)
        {
            switch (origin)
            {
                case LuaCsMessageOrigin.LuaCs:
                    PrintGenericError(o);
                    break;
                case LuaCsMessageOrigin.LuaMod:
                    PrintLuaError(o);
                    break;
                case LuaCsMessageOrigin.CSharpMod:
                    PrintCsError(o);
                    break;
            }
        }

        private static void PrintLuaError(object o) => PrintErrorBase($"[{LOG_PREFIX} LUA ERROR] ", o, "nil");

        // TODO: deprecate this
        // XXX: this is only public so that we don't break backward compat with C# mods
        public static void PrintCsError(object o) => PrintErrorBase($"[{LOG_PREFIX} CS ERROR] ", o, "Null");

        private static void PrintGenericError(object o) => PrintErrorBase($"[{LOG_PREFIX} ERROR] ", o, "Null");

        internal LuaCsMessageLogger MessageLogger { get; set; }

        private static void DefaultMessageLogger(string prefix, object o)
        {
            var message = o.ToString();
            for (int i = 0; i < message.Length; i += 1024)
            {
                var subStr = message.Substring(i, Math.Min(1024, message.Length - i));

#if SERVER
                if (GameMain.Server != null)
                {
                    foreach (var c in GameMain.Server.ConnectedClients)
                    {
                        GameMain.Server.SendDirectChatMessage(ChatMessage.Create("", subStr, ChatMessageType.Console, null, textColor: Color.MediumPurple), c);
                    }

                    GameServer.Log(prefix + subStr, ServerLog.MessageType.ServerMessage);
                }
#endif
            }

#if SERVER
            DebugConsole.NewMessage(message.ToString(), Color.MediumPurple);
#else
            DebugConsole.NewMessage(message.ToString(), Color.Purple);
#endif
        }

        private void PrintMessageBase(string prefix, object message, string empty) => MessageLogger?.Invoke(prefix, message ?? empty);
        internal void PrintMessage(object message) => PrintMessageBase("[LuaCs] ", message, "nil");

        // TODO: deprecate this (in an effort to get rid of as much global state as possible)
        public static void PrintCsMessage(object message) => GameMain.LuaCs.PrintMessage(message);

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

			return lua.DoFile(file, globalContext, codeStringFriendly);
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

			return lua.LoadFile(file, globalContext, codeStringFriendly);
		}

		public DynValue CallLuaFunction(object function, params object[] args)
		{
            // XXX: `lua` might be null if `LuaCsSetup.Stop()` is called while
            // a patched function is still running.
            if (lua == null) return null;

			lock (lua)
			{
				try
				{
					return lua.Call(function, args);
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
			Hook?.Update();
			Timer?.Update();
			Steam?.Update();
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
			lua = null;
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

			bool csActive = GetPackage("CsForBarotrauma", false, true) != null;

			LuaScriptLoader = new LuaScriptLoader();
			LuaScriptLoader.ModulePaths = new string[] { };

            RegisterLuaConverters();

			lua = new Script(CoreModules.Preset_SoftSandbox | CoreModules.Debug);
			lua.Options.DebugPrint = PrintMessage;
			lua.Options.ScriptLoader = LuaScriptLoader;
			lua.Options.CheckThreadAccess = false;
			CsScript = new CsScriptRunner(this);

			require = new LuaRequire(lua);

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

			lua.Globals["printerror"] = (Action<object>)PrintLuaError;

			lua.Globals["setmodulepaths"] = (Action<string[]>)SetModulePaths;

			lua.Globals["dofile"] = (Func<string, Table, string, DynValue>)DoFile;
			lua.Globals["loadfile"] = (Func<string, Table, string, DynValue>)LoadFile;
			lua.Globals["require"] = (Func<string, Table, DynValue>)require.Require;

			lua.Globals["dostring"] = (Func<string, Table, string, DynValue>)lua.DoString;
			lua.Globals["load"] = (Func<string, Table, string, DynValue>)lua.LoadString;

			lua.Globals["CsScript"] = CsScript;
			lua.Globals["LuaUserData"] = UserData.CreateStatic<LuaUserData>();
			lua.Globals["Game"] = Game;
			lua.Globals["Hook"] = Hook;
			lua.Globals["ModStore"] = ModStore;
			lua.Globals["Timer"] = Timer;
			lua.Globals["File"] = UserData.CreateStatic<LuaCsFile>();
			lua.Globals["Networking"] = Networking;
			lua.Globals["Steam"] = Steam;
			lua.Globals["PerformanceCounter"] = PerformanceCounter;

			lua.Globals["ExecutionNumber"] = executionNumber;
			lua.Globals["CSActive"] = csActive;

			lua.Globals["SERVER"] = IsServer;
			lua.Globals["CLIENT"] = IsClient;

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
					}
					catch (Exception ex)
					{
						HandleException(ex, LuaCsMessageOrigin.CSharpMod);
					}
				}

			}


			ContentPackage luaPackage = GetPackage("Lua For Barotrauma (with xpath patch)");

			if (File.Exists(LuaSetupFile))
			{
				PrintMessage("Using LuaSetup.lua from the Barotrauma Lua/ folder.");

				try
				{
					DynValue function = lua.LoadFile(LuaSetupFile);
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
					CallLuaFunction(lua.LoadFile(luaPath), Path.GetDirectoryName(Path.GetFullPath(luaPath)));
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
