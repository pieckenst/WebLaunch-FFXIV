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
        private Assembly _imGuiAssembly;
        private Type _networkLogicType;
        private Type _imGuiType;
        private dynamic _imGui;
        private dynamic _vector4;

        public override string PluginId => "ffxiv-launcher";
        public override string Name => "FFXIV Game Launcher";
        public override string Description => "Launches Final Fantasy XIV with authentication";
        public override string TargetApplication => "ffxiv_dx11.exe";
        public override Version Version => new Version(1, 0, 0);
        public override bool SupportsImGui => true;

        public override IReadOnlyCollection<PluginDependency> Dependencies => new[]
{
    new PluginDependency("CoreLibLaunchSupport", new Version(1, 0, 0)),
    new PluginDependency("LibDalamud", new Version(1, 0, 0)),
    new PluginDependency("ImGui.NET", new Version(1, 89, 4)),
    new PluginDependency("Veldrid", new Version(4, 9, 0))
};

        public FFXIVGamePlugin(ILogger logger) : base(logger)
        {
            _config = new Dictionary<string, string>();
        }

        protected override async Task InitializeInternalAsync()
{
    Logger.Information("Initializing FFXIV plugin...");

    var loadContext = new PluginLoadContext(AppContext.BaseDirectory);
    _coreSupportAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("CoreLibLaunchSupport")) 
        ?? throw new InvalidOperationException("Could not load CoreLibLaunchSupport assembly");
    _imGuiAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("ImGui.NET"))
        ?? throw new InvalidOperationException("Could not load ImGui.NET assembly");
    var numericsAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("System.Numerics"))
        ?? throw new InvalidOperationException("Could not load System.Numerics assembly");

    _networkLogicType = _coreSupportAssembly.GetType("CoreLibLaunchSupport.networklogic") 
        ?? throw new InvalidOperationException("Could not load networklogic type");
    _imGuiType = _imGuiAssembly.GetType("ImGuiNET.ImGui")
        ?? throw new InvalidOperationException("Could not load ImGui type");
    _vector4 = numericsAssembly.GetType("System.Numerics.Vector4")
        ?? throw new InvalidOperationException("Could not load Vector4 type");

    _networkLogic = Activator.CreateInstance(_networkLogicType)
        ?? throw new InvalidOperationException("Could not create networklogic instance");

    var checkGateStatus = _networkLogicType.GetMethod("CheckGateStatus")
        ?? throw new InvalidOperationException("Could not find CheckGateStatus method");
    var checkLoginStatus = _networkLogicType.GetMethod("CheckLoginStatus")
        ?? throw new InvalidOperationException("Could not find CheckLoginStatus method");

    var gateStatus = await Task.Run(() => (bool)checkGateStatus.Invoke(_networkLogic, null));
    var loginStatus = await Task.Run(() => (bool)checkLoginStatus.Invoke(_networkLogic, null));

    Logger.Information($"Server Status - Gate: {gateStatus}, Login: {loginStatus}");

    if (!gateStatus)
    {
        Logger.Warning("Game servers unavailable");
    }

    if (!loginStatus)
    {
        Logger.Warning("Login servers unavailable");
    }

    if (gateStatus && loginStatus)
    {
        Logger.Information("All systems operational");
    }
}


        public override void RenderImGui()
{
    try
    {
        if (_imGuiType.GetMethod("Begin").Invoke(null, new object[] { "FFXIV Launcher Status" }) is true)
        {
            // Server Status Section
            _imGuiType.GetMethod("Text").Invoke(null, new[] { "Server Status:" });
            _imGuiType.GetMethod("SameLine").Invoke(null, null);

            var gateStatus = (bool)_networkLogicType.GetMethod("CheckGateStatus").Invoke(_networkLogic, null);
            var loginStatus = (bool)_networkLogicType.GetMethod("CheckLoginStatus").Invoke(_networkLogic, null);

            var statusColor = (gateStatus && loginStatus) 
                ? Activator.CreateInstance(_vector4, new object[] { 0f, 1f, 0f, 1f })
                : Activator.CreateInstance(_vector4, new object[] { 1f, 1f, 0f, 1f });

            _imGuiType.GetMethod("TextColored").Invoke(null, new[] { statusColor, gateStatus && loginStatus ? "Online" : "Partial Outage" });

            // Configuration Section
            _imGuiType.GetMethod("Separator").Invoke(null, null);
            _imGuiType.GetMethod("Text").Invoke(null, new[] { "Configuration:" });

            if (_config.TryGetValue("GamePath", out var gamePath))
            {
                _imGuiType.GetMethod("Text").Invoke(null, new[] { $"Game Path: {gamePath}" });
                if (!Directory.Exists(Path.Combine(gamePath, "game")))
                {
                    _imGuiType.GetMethod("TextColored").Invoke(null, new[]
                    {
                        Activator.CreateInstance(_vector4, new object[] { 1f, 0f, 0f, 1f }),
                        "Game directory not found!"
                    });
                }
            }

            // Notifications Section
            _imGuiType.GetMethod("Separator").Invoke(null, null);
            _imGuiType.GetMethod("Text").Invoke(null, new[] { "Recent Events:" });
            RenderDefaultNotifications();

            _imGuiType.GetMethod("End").Invoke(null, null);
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"ImGui rendering failed: {ex.Message}");
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
