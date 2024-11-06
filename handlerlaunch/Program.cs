using handlerlaunch;
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
    
    if (!Directory.Exists(installPath))
    {
        Directory.CreateDirectory(installPath);
    }
    
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
            if (!File.Exists(sourceFile))
            {
                LogDebug($"Source file not found: {sourceFile}");
                continue;
            }

            string fileName = Path.GetFileName(sourceFile);
            string destFile = Path.Combine(installPath, fileName);

            if (File.Exists(destFile))
            {
                File.SetAttributes(destFile, FileAttributes.Normal);
            }

            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                sourceStream.CopyTo(destStream);
            }
            
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
                   if (args[0].Contains("?debugtest=yes"))
{
    bool errorOccurred = false;
    string previousOutput = "";
    try
    {
        if (fileWriter != null)
        {
            fileWriter.Flush();
            using (var reader = new StreamReader(new FileStream(tempLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                previousOutput = reader.ReadToEnd();
            }
        }

        if (args[0].Contains("-type=progress"))
        {
            ShowProgressWindow(previousOutput);
        }
        else if (args[0].Contains("-type=custom"))
        {
            ShowProgressWindow(previousOutput, true, "Custom Progress", 400, 250, "Debug Test", "!", 
                new List<string> { "Step 1", "Step 2", "Step 3" }, "Processing...", null,
                (s, e) => { LogDebug("OK clicked"); },
                (s, e) => { LogDebug("Cancel clicked"); });
        }
        else if (args[0].Contains("-type=autoclose"))
        {
            ShowProgressWindow(previousOutput, true, "Auto-closing Message", 400, 250, "Debug Test", "!", 
                null, "This window will close automatically", null, null, null, true, 5);
        }
        else
        {
            ShowProgressWindow(previousOutput, true, "Game Launch Status", 1000, 1500, "Launch Progress", "X", 
                new List<string> { "Checking game files...", "Verifying credentials...", "Initializing game launcher..." }, 
                "Please wait while the game launches...");
        }
    }
    catch (Exception ex)
    {
        LogDebug($"Error in debug test: {ex.Message}");
        errorOccurred = true;
        ShowNotification("Error", $"Failed to launch debug test: {ex.Message}");
    }
}

                    else if (args[0].Contains("?ffxivhandle=yes"))                    {
                        ShowNotification("FFXIV Launch", "Starting Final Fantasy XIV...");

                        if (!args[0].Contains("login=") || !args[0].Contains("pass=") || !args[0].Contains("hash="))
                        {
                            throw new ArgumentException("Missing required login credentials");
                        }

                        bool errorOccurred = false;
                        string previousOutput = "";
try 
{
    FFXIVhandler.HandleFFXivReq(args);
    if (fileWriter != null)
    {
        fileWriter.Flush();
        using (var reader = new StreamReader(new FileStream(tempLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        {
            previousOutput = reader.ReadToEnd();
        }
    }
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
    var outputLock = new object();
    var completionSource = new TaskCompletionSource<bool>();

    try
    {
        var splbornobj = new SpellbornSupporter();
        var consoleOutput = new StringBuilder();
        
        var task = Task.Run(() =>
        {
            try
            {
                Directory.SetCurrentDirectory(splbornobj.GetGamePathFromArgs(args));
                splbornobj.StartupRoutine(args);
                return true;
            }
            catch (Exception ex)
            {
                lock (outputLock)
                {
                    consoleOutput.AppendLine($"Error: {ex.Message}");
                }
                return false;
            }
            finally
            {
                completionSource.SetResult(true);
            }
        });

        ShowProgressWindow(previousOutput, true, "Spellborn Launch", 400, 300,
            "Launch Progress", "!", 
            new List<string> { "Checking game files...", "Verifying installation...", "Starting game..." },
            "Please wait while the game launches...", null, null, null, true, 30);

        task.Wait();
        errorOccurred = !task.Result;
    }
    catch (Exception ex)
    {
        LogDebug($"Error in Spellborn startup routine: {ex.Message}");
        errorOccurred = true;
        ShowNotification("Error", $"Failed to launch Spellborn: {ex.Message}");
    }

    completionSource.Task.Wait();
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
                try
                {
                    if (fileWriter != null)
                    {
                        Console.SetOut(originalConsole);
                        fileWriter.Flush();
                        fileWriter.Close();
                        fileWriter.Dispose();
                    }

                    if (File.Exists(tempLogFile))
                    {
                        try
                        {
                            File.Delete(tempLogFile);
                        }
                        catch (IOException)
                        {
                            // Ignore file deletion errors
                            LogDebug("Could not delete temporary log file - will be cleaned up later");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Error during cleanup: {ex.Message}");
                }
            }
                      Console.WriteLine("Press any key to exit or wait 60 seconds...");
                      Task.WhenAny(
                          Task.Run(() => Console.ReadLine()),
                          Task.Delay(TimeSpan.FromSeconds(60))
                      ).Wait();
                  }











      private static void ShowProgressWindow(string previousOutput, bool isMessageBox = false,
    string customLabel = "Loading...", double? customWidth = null, double? customHeight = null,
    string messageTitle = "", string messageIcon = "!", List<string> listItems = null,
    string footerText = "", SpinningWheelLib.Window1.ListItemsProvider listItemsProvider = null,
    RoutedEventHandler okHandler = null, RoutedEventHandler cancelHandler = null,
    bool hideButtons = false, int? autoCloseSeconds = null)
{
    LogDebug($"Showing {(isMessageBox ? "message box" : "progress window")}");
    
    var thread = new Thread(() =>
    {
        try
        {
            LogDebug("Creating window with specified parameters");
            Window1 window = null;
            
            SynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
            
            if (Application.Current == null)
            {
                new Application();
            }

            window = new Window1(30, isMessageBox, customLabel, customWidth, customHeight,
                messageTitle, messageIcon, listItems, footerText, listItemsProvider,
                okHandler, cancelHandler, hideButtons, autoCloseSeconds);
            
            if (!string.IsNullOrEmpty(previousOutput))
            {
                LogDebug("Writing previous output to console");
                window.WriteToConsole(previousOutput, Colors.White);
            }
            
            var checkProcessTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            
            checkProcessTimer.Tick += (s, e) =>
            {
                try
                {
                    if (!isMessageBox)
                    {
                        var processes = Process.GetProcessesByName("ffxiv_dx11");
                        if (processes.Length > 0)
                        {
                            LogDebug("Target process found, closing window");
                            checkProcessTimer.Stop();
                            window.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"Error checking process: {ex.Message}");
                    checkProcessTimer.Stop();
                    window.Close();
                }
            };

            window.Closed += (s, e) =>
            {
                LogDebug("Window closed event triggered");
                checkProcessTimer.Stop();
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            };

            LogDebug("Hiding console window");
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            
            if (!isMessageBox)
            {
                LogDebug("Starting process check timer");
                checkProcessTimer.Start();
            }
            
            LogDebug("Showing window");
            window.Show();
            Dispatcher.Run();
            
            LogDebug("Showing console window");
            ShowWindow(handle, SW_SHOW);
        }
        catch (Exception ex)
        {
            LogDebug($"Critical error in window thread: {ex.Message}");
            LogDebug($"Stack trace: {ex.StackTrace}");
        }
    });
    
    thread.SetApartmentState(ApartmentState.STA);
    LogDebug("Starting window thread");
    thread.Start();
    
    LogDebug($"{(isMessageBox ? "Message box" : "Progress window")} initialized");
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