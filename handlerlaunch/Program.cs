﻿using handlerlaunch;
using CoreLibLaunchSupport;
using Microsoft.Win32;
using SpinningWheelLib;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

using System.Runtime.InteropServices;

using SpinningWheelLib;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Media;
using System.Text;

namespace WMConsole
{
    internal class ConsoleOutputRedirector : TextWriter
    {
        private readonly Window1 _window;

        public ConsoleOutputRedirector(Window1 window)
        {
            _window = window;
        }

        public override void Write(string value)
        {
            _window.WriteToConsole(value, Colors.White);
        }

        public override void WriteLine(string value)
        {
            _window.WriteToConsole(value + Environment.NewLine, Colors.White);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }

    internal class MultiWriter : TextWriter
{
    private readonly TextWriter[] writers;
    
    public MultiWriter(TextWriter[] writers)
    {
        this.writers = writers;
    }

    public override void Write(string value)
    {
        foreach (var writer in writers)
            writer.Write(value);
    }

    public override void WriteLine(string value)
    {
        foreach (var writer in writers)
            writer.WriteLine(value);
    }

    public override Encoding Encoding => Encoding.UTF8;
}

    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
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
            // Hide console window immediately on startup
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            string tempLogFile = Path.Combine(Path.GetTempPath(), $"console_output_{Guid.NewGuid()}.txt");
            TextWriter originalConsole = Console.Out;
            StreamWriter fileWriter = null;

            try
            {
                fileWriter = new StreamWriter(new FileStream(tempLogFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
                var multiWriter = new MultiWriter(new TextWriter[] { originalConsole, fileWriter });
                Console.SetOut(multiWriter);

                LogDebug("Application started");
                ShowNotification("Application Started", "Initializing launcher...");

                CopyFiles();
                RegisterProtocolHandler();

                if (args?.Length > 0)
                {
                    LogDebug($"Received arguments: {string.Join(", ", args)}");

                    if (args[0].Contains("?ffxivhandle=yes"))
                    {
                        ShowNotification("FFXIV Launch", "Starting Final Fantasy XIV...");

                        if (!args[0].Contains("login=") || !args[0].Contains("pass="))
                        {
                            throw new ArgumentException("Missing required login credentials");
                        }

                        bool errorOccurred = false;
                        string previousOutput = "";

                        try
                        {
                            FFXIVhandler.HandleFFXivReq(args);
                            fileWriter.Flush();
                            previousOutput = File.ReadAllText(tempLogFile);
                        }
                        catch (Exception ex)
                        {
                            LogDebug($"Error handling FFXIV request: {ex.Message}");
                            errorOccurred = true;
                            ShowNotification("Error", $"Failed to launch FFXIV: {ex.Message}");
                        }

                        if (!errorOccurred)
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                ShowProgressWindow(previousOutput);
                            });
                        }
                    }
                    else if (args[0].Contains("?spellbornhandle=yes"))
                    {
                        ShowNotification("Spellborn Launch", "Starting Chronicles of Spellborn...");

                        if (!args[0].Contains("gamepath="))
                        {
                            throw new ArgumentException("Missing required game path");
                        }

                        bool errorOccurred = false;
                        string previousOutput = "";

                        try
                        {
                            var splbornobj = new SpellbornSupporter();
                            splbornobj.StartupRoutine(args);
                            fileWriter.Flush();
                            previousOutput = File.ReadAllText(tempLogFile);
                        }
                        catch (Exception ex)
                        {
                            LogDebug($"Error in Spellborn startup routine: {ex.Message}");
                            errorOccurred = true;
                            ShowNotification("Error", $"Failed to launch Spellborn: {ex.Message}");
                        }

                        if (!errorOccurred)
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                ShowProgressWindow(previousOutput);
                            });
                        }
                    }
                }
                else
                {
                    ShowNotification("Protocol Handler Registered",
                        "You may now proceed to https://pieckenst.github.io/WebLaunch-FFXIV/ to launch the game");
                }
            }
            catch (Exception ex)
            {
                ShowNotification("Error", ex.Message);
                LogDebug($"An error occurred: {ex.Message}");
            }
            finally
            {
                if (fileWriter != null)
                {
                    Console.SetOut(originalConsole);
                    fileWriter.Flush();
                    fileWriter.Close();
                    fileWriter.Dispose();
                }

                try
                {
                    if (File.Exists(tempLogFile))
                    {
                        File.Delete(tempLogFile);
                    }
                }
                catch { }
            }
        }









        private static void ShowProgressWindow(string previousOutput)
{
    LogDebug("Showing progress window");
    var app = new Application();
    var progressWindow = new Window1(30);
    
    // Display previous console output
    progressWindow.WriteToConsole(previousOutput, Colors.White);
    
    var checkProcessTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };
    
    checkProcessTimer.Tick += (s, e) =>
    {
        try
        {
            var processes = Process.GetProcessesByName("ffxiv_dx11");
            if (processes.Length > 0)
            {
                checkProcessTimer.Stop();
                progressWindow.Close();
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Error checking process: {ex.Message}");
            checkProcessTimer.Stop();
            progressWindow.Close();
        }
    };

    // Create a custom TextWriter that writes to both console and window
    var customWriter = new MultiWriter(new TextWriter[] 
    { 
        Console.Out,
        new ConsoleOutputRedirector(progressWindow)
    });
    Console.SetOut(customWriter);

    // Hide console window
    var handle = GetConsoleWindow();
    ShowWindow(handle, SW_HIDE);
    
    checkProcessTimer.Start();
    app.Run(progressWindow);
    
    // Restore original console output and show console window
    Console.SetOut(Console.Out);
    ShowWindow(handle, SW_SHOW);
    
    LogDebug("Progress window closed");
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