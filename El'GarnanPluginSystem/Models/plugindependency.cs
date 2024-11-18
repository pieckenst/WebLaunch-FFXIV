namespace El_Garnan_Plugin_Loader.Models
{
    /// <summary>
    /// Represents a dependency for a plugin.
    /// </summary>
    public class PluginDependency
    {
        /// <summary>
        /// Gets the name of the dependency.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the minimum version of the dependency required.
        /// </summary>
        public Version MinVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDependency"/> class.
        /// </summary>
        /// <param name="name">The name of the dependency.</param>
        /// <param name="minVersion">The minimum version of the dependency required.</param>
        public PluginDependency(string name, Version minVersion)
        {
            Name = name;
            MinVersion = minVersion;
        }
    }
}
