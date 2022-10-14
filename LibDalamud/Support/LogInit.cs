using System.IO;
using System.Linq;
using CommandLine;
using Serilog;

namespace XIVLauncher.Common.Support;

public static class LogInit
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private class LogOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option("log-file-path", Required = false, HelpText = "Set path for log file.")]
        public string? LogPath { get; set; }
    }

    
}
