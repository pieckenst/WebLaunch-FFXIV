using handlerlaunch;
using CoreLibLaunchSupport;
using Microsoft.Win32;
using SpinningWheelLib;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

namespace WMConsole
{
    class Program
    {
        private const string DefaultInstallPath = @"D:\HandleGame\";
        private static string? _installPath;
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

        public static string GetExpansionFolder(byte expansionId) =>
            expansionId == 0 ? "ffxiv" : $"ex{expansionId}";

        public static string? TextFollowing(string? txt, string? value)
        {
            LogDebug("Entering TextFollowing method");
            if (string.IsNullOrEmpty(txt) || string.IsNullOrEmpty(value))
                return null;

            int index = txt.IndexOf(value);
            if (index == -1)
                return null;

            int start = index + value.Length;
            return start <= txt.Length ? txt[start..] : null;
        }

        public static string ReturnXpacNum(ushort expansionId) =>
            GetExpansionFolder((byte)expansionId);

        private static string GetInstallPath()
        {
            LogDebug("Entering GetInstallPath method");
            if (!string.IsNullOrEmpty(_installPath))
                return _installPath;

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            LogDebug($"Executable path: {exePath}");
            string currentDirectory = Path.GetDirectoryName(exePath) ?? throw new InvalidOperationException("Unable to determine current directory");
            LogDebug($"Current directory: {currentDirectory}");
            string? parentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            LogDebug($"Parent directory: {parentDirectory}");

            if (Directory.Exists(DefaultInstallPath))
            {
                _installPath = DefaultInstallPath;
                LogDebug($"Using default install path: {_installPath}");
            }
            else if (parentDirectory != null && Directory.Exists(parentDirectory))
            {
                _installPath = Path.Combine(parentDirectory, "HandleGame");
                LogDebug($"Using parent directory for install: {_installPath}");
            }
            else
            {
                _installPath = Path.Combine(currentDirectory, "HandleGame");
                LogDebug($"Using current directory for install: {_installPath}");
            }

            return _installPath;
        }

        private static void CopyFiles()
        {
            LogDebug("Entering CopyFiles method");
            string installPath = GetInstallPath();
            Directory.CreateDirectory(installPath);
            LogDebug($"Created install directory: {installPath}");

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string baseDir = Path.GetDirectoryName(exePath) ?? throw new InvalidOperationException("Unable to determine base directory");
            LogDebug($"Base directory: {baseDir}");

            var filesToCopy = new[]
            {
                exePath,
                Path.Combine(baseDir, "WMConsole.exe"),
                Path.Combine(baseDir, "WMConsole.runtimeconfig.json"),
                Path.Combine(baseDir, "CoreLibLaunchSupport.dll"),
                Path.Combine(baseDir, "SpinningWheelLib.dll"),
                Path.Combine(baseDir, "WpfAnimatedGif.dll"),
                Path.Combine(baseDir, "XamlAnimatedGif.dll"),
                Path.Combine(baseDir, "DotNetZip.dll"),
                Path.Combine(baseDir, "Newtonsoft.Json.dll"),
                Path.Combine(baseDir, "log4net.dll"),
                Path.Combine(baseDir, "Goblinfactory.Konsole.dll"),
                Path.Combine(baseDir, "CommandLine.dll"),
                Path.Combine(baseDir, "Downloader.dll"),
                Path.Combine(baseDir, "Facepunch.Steamworks.Win64.dll"),
                Path.Combine(baseDir, "LibDalamud.dll"),
                Path.Combine(baseDir, "Mono.Nat.dll"),
                Path.Combine(baseDir, "MonoTorrent.dll"),
                Path.Combine(baseDir, "ReusableTasks.dll"),
                Path.Combine(baseDir, "Serilog.dll"),
                Path.Combine(baseDir, "SharedMemory.dll"),
                Path.Combine(baseDir, "Microsoft.Toolkit.Uwp.Notifications.dll"),
                Path.Combine(baseDir, "Microsoft.Windows.SDK.NET.dll"),
                Path.Combine(baseDir, "WinRT.Runtime.dll")
            };

            foreach (string sourceFile in filesToCopy)
            {
                try
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string destFile = Path.Combine(installPath, fileName);
                    File.Copy(sourceFile, destFile, true);
                    LogDebug($"Copied file: {sourceFile} to {destFile}");
                }
                catch (Exception ex)
                {
                    LogDebug($"Error copying file {sourceFile}: {ex.Message}");
                }
            }
        }

        private static void RegisterProtocolHandler()
        {
            LogDebug("Entering RegisterProtocolHandler method");
            try
            {
                using var keyTest = Registry.CurrentUser.OpenSubKey("Software", true)?.OpenSubKey("Classes", true);
                if (keyTest == null)
                    throw new Exception("Unable to open registry key");

                using var key = keyTest.CreateSubKey("HandleWebRequest");
                key.SetValue("URL Protocol", "HandleReqLaunch");
                string commandPath = $"{Path.Combine(GetInstallPath(), "WMConsole.exe")} %1";
                key.CreateSubKey(@"shell\open\command")?.SetValue("", commandPath);
                LogDebug($"Registered protocol handler with command: {commandPath}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error registering protocol handler: {ex.Message}");
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            LogDebug("Application started");
            Console.Title = "Protocol Handler Process";
            Console.WriteLine("Initializing...");

            try
            {
                CopyFiles();
                RegisterProtocolHandler();

                if (args != null && args.Length > 0)
                {
                    try
                    {
                        LogDebug($"Received arguments: {string.Join(", ", args)}");
                        if (args[0].Contains("?ffxivhandle=yes"))
                        {
                            LogDebug("Handling FFXIV request");
                            bool errorOccurred = false;
                            try
                            {
                                FFXIVhandler.HandleFFXivReq(args);
                            }
                            catch (Exception ex)
                            {
                                LogDebug($"Error handling FFXIV request: {ex.Message}");
                                errorOccurred = true;
                            }
                            if (!errorOccurred)
                            {
                                try
                                {
                                    ShowProgressWindow();
                                }
                                catch (Exception ex)
                                {
                                    LogDebug($"Error showing progress window: {ex.Message}");
                                }
                            }
                            try
                            {
                                ShowNotification("Game process has been launched", "You may now play the game");
                            }
                            catch (Exception ex)
                            {
                                LogDebug($"Error showing notification: {ex.Message}");
                            }
                        }
                        else if (args[0].Contains("?spellbornhandle=yes"))
                        {
                            LogDebug("Handling Spellborn request");
                            var splbornobj = new SpellbornSupporter();
                            bool errorOccurred = false;
                            try
                            {
                                splbornobj.StartupRoutine(args);
                            }
                            catch (Exception ex)
                            {
                                LogDebug($"Error in Spellborn startup routine: {ex.Message}");
                                errorOccurred = true;
                            }
                            if (!errorOccurred)
                            {
                                try
                                {
                                    ShowProgressWindow();
                                }
                                catch (Exception ex)
                                {
                                    LogDebug($"Error showing progress window: {ex.Message}");
                                }
                            }
                            try
                            {
                                ShowNotification("Game process has been launched", "You may now play the game");
                            }
                            catch (Exception ex)
                            {
                                LogDebug($"Error showing notification: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error processing arguments: {ex.Message}");
                    }
                }
                else
                {
                    LogDebug("No arguments received, showing registration notification");
                    try
                    {
                        ShowNotification("Protocol handler registered", 
                            "Protocol handler has been registered - you may now proceed to https://pieckenst.github.io/WebLaunch-FFXIV/ to launch the game");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error showing registration notification: {ex.Message}");
                    }
                    Console.WriteLine("Listening...");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"An error occurred: {ex.Message}");
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.ReadLine();
            }
        }

        private static void ShowProgressWindow()
        {
            LogDebug("Showing progress window");
            Window1 windower = new Window1();
            windower.Show();
            System.Threading.Thread.Sleep(10000);
            windower.Close();
            LogDebug("Closed progress window");
        }

        private static void ShowNotification(string title, string content)
        {
            LogDebug($"Showing notification: {title} - {content}");
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddArgument("conversationId", 9813)
                .AddText(title)
                .AddText(content)
                .Show();
        }

        private static void LogDebug(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
    }
}