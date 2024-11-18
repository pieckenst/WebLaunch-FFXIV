using System.Collections.Generic;

namespace El_Garnan_Plugin_Loader.Models
{
    /// <summary>
    /// Parameters for launching games through plugins
    /// </summary>
    public class GameLaunchParameters
    {
        /// <summary>
        /// Path to game installation
        /// </summary>
        public string GamePath { get; set; }

        /// <summary>
        /// Game session ID if applicable
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Language setting (0-3)
        /// </summary>
        public int Language { get; set; }

        /// <summary>
        /// Use DirectX 11 mode
        /// </summary>
        public bool DirectX11 { get; set; }

        /// <summary>
        /// Maximum expansion level
        /// </summary>
        public int ExpansionLevel { get; set; }

        /// <summary>
        /// Is Steam version
        /// </summary>
        public bool IsSteam { get; set; }

        /// <summary>
        /// Game region
        /// </summary>
        public int Region { get; set; }

        /// <summary>
        /// Raw launch arguments
        /// </summary>
        public string[] LaunchArgs { get; set; }

        /// <summary>
        /// Additional environment variables
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

        /// <summary>
        /// Credentials for game authentication
        /// </summary>
        public GameCredentials Credentials { get; set; }
    }
}
