using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using El_Garnan_Plugin_Loader.Models;

namespace El_Garnan_Plugin_Loader.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Shows a notification with the specified title, message and type
        /// </summary>
        void ShowNotification(string title, string message, NotificationType type = NotificationType.Info);
    }
    /// <summary>
    /// Core interface for game launcher plugins
    /// </summary>
    public interface IGamePlugin
    {
        /// <summary>
        /// Unique identifier for the plugin
        /// </summary>
        string PluginId { get; }

        /// <summary>
        /// Display name of the plugin
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Plugin description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Target game/application
        /// </summary>
        string TargetApplication { get; }

        /// <summary>
        /// Plugin version
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Plugin dependencies
        /// </summary>
        IReadOnlyCollection<PluginDependency> Dependencies { get; }

        /// <summary>
        /// Indicates if plugin supports hot reloading
        /// </summary>
        bool SupportsHotReload { get; }

        /// <summary>
        /// Plugin status
        /// </summary>
        PluginStatus Status { get; }

        /// <summary>
        /// Last error message if any
        /// </summary>
        string LastError { get; }

        /// <summary>
        /// Initialize plugin resources
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Launch the game with given parameters
        /// </summary>
        Task<bool> LaunchGameAsync(GameLaunchParameters parameters);

        /// <summary>
        /// Clean up plugin resources
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Reload plugin (if supported)
        /// </summary>
        Task<bool> ReloadAsync();

        /// <summary>
        /// Validate plugin configuration
        /// </summary>
        Task<bool> ValidateConfigurationAsync();

        /// <summary>
        /// Renders default notifications using ImGui
        /// </summary>
        protected static void RenderDefaultNotifications()
        {
            // Default empty implementation
        }

        /// <summary>
        /// Renders the plugin's ImGui interface if supported
        /// </summary>
        void RenderImGui() => RenderDefaultNotifications();

        /// <summary>
        /// Indicates if the plugin supports ImGui rendering
        /// </summary>
        bool SupportsImGui => false;

        /// <summary>
        /// Gets the plugin's notification queue
        /// </summary>
        Queue<PluginNotification> NotificationQueue { get; }
    }

    /// <summary>
    /// Represents the status of the plugin.
    /// </summary>
    public enum PluginStatus
    {
        /// <summary>
        /// Plugin is not initialized.
        /// </summary>
        Uninitialized,
        
        /// <summary>
        /// Plugin is in the process of initializing.
        /// </summary>
        Initializing,
        
        /// <summary>
        /// Plugin is ready to be used.
        /// </summary>
        Ready,
        
        /// <summary>
        /// Plugin has encountered an error and failed.
        /// </summary>
        Failed,
        
        /// <summary>
        /// Plugin is in the process of reloading.
        /// </summary>
        Reloading,
        
        /// <summary>
        /// Plugin is disabled.
        /// </summary>
        Disabled
    }
}
