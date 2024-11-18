using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using El_Garnan_Plugin_Loader.Base;
using El_Garnan_Plugin_Loader.Interfaces;
using El_Garnan_Plugin_Loader.Models;
using El_Garnan_Plugin_Loader;

namespace GamePlugins.FFXIV
{
    public class FFXIVGamePlugin : GamePluginBase
    {
        private dynamic _networkLogic;
        private readonly Dictionary<string, string> _config;
        private Assembly _coreSupportAssembly;
        private Type _networkLogicType;

        public override string PluginId => "ffxiv-launcher";
        public override string Name => "FFXIV Game Launcher";
        public override string Description => "Launches Final Fantasy XIV with authentication";
        public override string TargetApplication => "ffxiv_dx11.exe";
        public override Version Version => new Version(1, 0, 0);
        public override IReadOnlyCollection<PluginDependency> Dependencies => new[]
        {
            new PluginDependency("CoreLibLaunchSupport", new Version(1, 0, 0))
        };
        public override bool SupportsHotReload => true;

        public FFXIVGamePlugin(ILogger logger) : base(logger)
        {
            _config = new Dictionary<string, string>();
        }

        protected override async Task InitializeInternalAsync()
        {
            Logger.Information("Initializing FFXIV plugin...");

            var loadContext = new PluginLoadContext(AppContext.BaseDirectory);
            _coreSupportAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("CoreLibLaunchSupport"));
            _networkLogicType = _coreSupportAssembly.GetType("CoreLibLaunchSupport.networklogic");
            _networkLogic = Activator.CreateInstance(_networkLogicType);

            try 
            {
                var checkGateStatus = _networkLogicType.GetMethod("CheckGateStatus");
                var checkLoginStatus = _networkLogicType.GetMethod("CheckLoginStatus");

                var gateStatus = await Task.Run(() => (bool)checkGateStatus.Invoke(_networkLogic, null));
                if (!gateStatus)
                {
                    Logger.Warning("FFXIV game servers appear to be unavailable, but continuing anyway");
                }

                var loginStatus = await Task.Run(() => (bool)checkLoginStatus.Invoke(_networkLogic, null));
                if (!loginStatus)
                {
                    Logger.Warning("FFXIV login servers appear to be unavailable, but continuing anyway");
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Server status check failed: {ex.Message}. Continuing anyway.");
            }
        }

        protected override async Task<bool> LaunchGameInternalAsync(GameLaunchParameters parameters)
        {
            try
            {
                Logger.Debug($"Launch parameters received: GamePath={parameters.GamePath}");
                Logger.Debug($"Environment variables count: {parameters.EnvironmentVariables.Count}");
                foreach (var kvp in parameters.EnvironmentVariables)
                {
                    Logger.Debug($"Environment variable: {kvp.Key}={kvp.Value}");
                }

                if (parameters.EnvironmentVariables.TryGetValue("FFXIV_USERNAME", out var username) &&
                    parameters.EnvironmentVariables.TryGetValue("FFXIV_PASSWORD", out var password) &&
                    parameters.EnvironmentVariables.TryGetValue("FFXIV_OTP", out var otp))
                {
                    Logger.Debug("All required credentials found");
                    var getRealSid = _networkLogicType.GetMethod("GetRealSid");
                    var launchGameAsync = _networkLogicType.GetMethod("LaunchGameAsync");

                    Logger.Debug("Attempting to obtain session ID");
                    var sid = await Task.Run(() => (string)getRealSid.Invoke(_networkLogic, new object[]
                    {
                parameters.GamePath,
                username,
                password,
                otp,
                parameters.IsSteam
                    }));

                    if (sid == "BAD")
                    {
                        throw new Exception("Failed to obtain valid session ID");
                    }

                    Logger.Debug("Successfully obtained session ID");
                    var process = await Task.Run(() => launchGameAsync.Invoke(_networkLogic, new object[]
                    {
                parameters.GamePath,
                sid,
                parameters.Language,
                parameters.DirectX11,
                parameters.ExpansionLevel,
                parameters.IsSteam,
                parameters.Region
                    }));

                    return process != null;
                }
                else
                {
                    Logger.Error("Missing credentials:");
                    Logger.Error($"Username present: {parameters.EnvironmentVariables.ContainsKey("FFXIV_USERNAME")}");
                    Logger.Error($"Password present: {parameters.EnvironmentVariables.ContainsKey("FFXIV_PASSWORD")}");
                    Logger.Error($"OTP present: {parameters.EnvironmentVariables.ContainsKey("FFXIV_OTP")}");
                    throw new ArgumentException("Missing required credentials in environment variables");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to launch FFXIV", ex);
                return false;
            }
        }


        protected override Task ShutdownInternalAsync()
        {
            Logger.Information("Shutting down FFXIV plugin...");
            return Task.CompletedTask;
        }

        protected override Task<bool> ValidateConfigurationInternalAsync()
        {
            try
            {
                // Initialize config if empty
                if (!_config.ContainsKey("GamePath"))
                {
                    _config["GamePath"] = Path.Combine(AppContext.BaseDirectory, "game");
                }

                if (!Directory.Exists(Path.Combine(_config["GamePath"], "game")))
                {
                    Logger.Warning("FFXIV game directory not found, will be validated at launch time");
                }
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Error("Configuration validation failed", ex);
                return Task.FromResult(false);
            }
        }

    }
}
