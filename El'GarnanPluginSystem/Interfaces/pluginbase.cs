using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using El_Garnan_Plugin_Loader.Interfaces;
using El_Garnan_Plugin_Loader.Models;

namespace El_Garnan_Plugin_Loader.Base
{
    /// <summary>
    /// Base class for game plugins, providing common functionality and structure.
    /// </summary>
    public abstract class GamePluginBase : IGamePlugin
    {
        /// <summary>
        /// Logger instance for logging plugin activities and errors.
        /// </summary>
        protected readonly ILogger Logger;

        /// <summary>
        /// Semaphore to ensure that only one launch operation can occur at a time.
        /// </summary>
        protected readonly SemaphoreSlim LaunchLock = new(1, 1);

        private bool _isInitialized;
        private string _lastError;
        private PluginStatus _status = PluginStatus.Uninitialized;

        /// <summary>
        /// Gets the unique identifier for the plugin.
        /// </summary>
        public abstract string PluginId { get; }

        /// <summary>
        /// Gets the display name of the plugin.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description of the plugin.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the target application or game for the plugin.
        /// </summary>
        public abstract string TargetApplication { get; }

        /// <summary>
        /// Gets the version of the plugin.
        /// </summary>
        public abstract Version Version { get; }

        /// <summary>
        /// Gets the collection of plugin dependencies.
        /// </summary>
        public abstract IReadOnlyCollection<PluginDependency> Dependencies { get; }

        /// <summary>
        /// Indicates whether the plugin supports hot reloading.
        /// </summary>
        public virtual bool SupportsHotReload => false;

        /// <summary>
        /// Gets the current status of the plugin.
        /// </summary>
        public PluginStatus Status => _status;

        /// <summary>
        /// Gets the last error message encountered by the plugin.
        /// </summary>
        public string LastError => _lastError;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamePluginBase"/> class.
        /// </summary>
        /// <param name="logger">The logger to be used by the plugin.</param>
        protected GamePluginBase(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Asynchronously initializes the plugin.
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            try
            {
                _status = PluginStatus.Initializing;
                
                if (!_isInitialized)
                {
                    await InitializeInternalAsync();
                    _isInitialized = true;
                }
                
                _status = PluginStatus.Ready;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _status = PluginStatus.Failed;
                Logger.Error($"Failed to initialize plugin {Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously launches the game with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters for launching the game.</param>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating success.</returns>
        public async Task<bool> LaunchGameAsync(GameLaunchParameters parameters)
        {
            if (_status != PluginStatus.Ready)
            {
                _lastError = "Plugin not ready";
                return false;
            }

            try
            {
                await LaunchLock.WaitAsync();
                return await LaunchGameInternalAsync(parameters);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Logger.Error($"Failed to launch game via plugin {Name}", ex);
                return false;
            }
            finally
            {
                LaunchLock.Release();
            }
        }

        /// <summary>
        /// Asynchronously reloads the plugin if hot reloading is supported.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating success.</returns>
        public virtual async Task<bool> ReloadAsync()
        {
            if (!SupportsHotReload)
            {
                _lastError = "Hot reload not supported";
                return false;
            }

            try
            {
                _status = PluginStatus.Reloading;
                await ShutdownAsync();
                await InitializeAsync();
                return true;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _status = PluginStatus.Failed;
                Logger.Error($"Failed to reload plugin {Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Asynchronously validates the plugin configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating success.</returns>
        public virtual async Task<bool> ValidateConfigurationAsync()
        {
            try
            {
                return await ValidateConfigurationInternalAsync();
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Logger.Error($"Configuration validation failed for plugin {Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Asynchronously shuts down the plugin and releases resources.
        /// </summary>
        public virtual async Task ShutdownAsync()
        {
            try
            {
                await ShutdownInternalAsync();
                _isInitialized = false;
                _status = PluginStatus.Uninitialized;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _status = PluginStatus.Failed;
                Logger.Error($"Failed to shutdown plugin {Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Asynchronously initializes the plugin's internal components.
        /// </summary>
        protected abstract Task InitializeInternalAsync();

        /// <summary>
        /// Asynchronously launches the game with the specified parameters internally.
        /// </summary>
        /// <param name="parameters">The parameters for launching the game.</param>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating success.</returns>
        protected abstract Task<bool> LaunchGameInternalAsync(GameLaunchParameters parameters);

        /// <summary>
        /// Asynchronously shuts down the plugin's internal components.
        /// </summary>
        protected abstract Task ShutdownInternalAsync();

        /// <summary>
        /// Asynchronously validates the plugin's internal configuration.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with a boolean result indicating success.</returns>
        protected virtual Task<bool> ValidateConfigurationInternalAsync() => Task.FromResult(true);
    }
}
