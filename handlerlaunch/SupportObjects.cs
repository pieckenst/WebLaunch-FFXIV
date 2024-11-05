using CoreLibLaunchSupport;
using log4net;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using SpinningWheelLib;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


using System.ComponentModel;
using System.Diagnostics;
using Ionic.Zip;
using Konsole;
using WMConsole;

namespace handlerlaunch
{
    
    public class Update
    {
        [JsonProperty("applies_to")]
        public string appliesTo { get; set; }

        [JsonProperty("version")]
        public string version { get; set; }

        [JsonProperty("file")]
        public string file { get; set; }

        [JsonProperty("patchnotes")]
        public string patchnotes { get; set; }

        [JsonProperty("checksum")]
        public string checksum { get; set; }

        [JsonProperty("server")]
        public string files { get; set; }

        [JsonProperty("enabled")]
        public string enabled { get; set; }
    }
    public class UpdateJson
    {
        [JsonProperty("update")]
        public Update Update { get; set; }
    }
    internal static class updateHandler
    {
        internal static dynamic getJsonItem(string file)
        {
            return JsonConvert.DeserializeObject<object>(new StreamReader(new WebClient().OpenRead("https://files.spellborn.org/" + file)).ReadToEnd());
        }

        internal static bool checkIfChecksumMatches(string file, string checksum)
        {
            using MD5 mD = MD5.Create();
            using FileStream inputStream = File.OpenRead(file);
            if (BitConverter.ToString(mD.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant() == checksum)
            {
                return true;
            }
            return false;
        }

        internal static dynamic fetchUpdates()
        {
            return JsonConvert.DeserializeObject<List<UpdateJson>>(new StreamReader(new WebClient().OpenRead("https://files.spellborn.org/updates.json")).ReadToEnd());
        }
    }
    internal class registryManipulation
    {
        private static object keyValue;
        public static void createkeys(string passover) {
            var KeyTests = Registry.CurrentUser.OpenSubKey("Software", true);
            KeyTests.CreateSubKey("The Chronicles of Spellborn");
            var keyval = KeyTests.GetValue("installPath").ToString();
            if (keyval == null)
            {
                KeyTests.SetValue("installPath", passover, RegistryValueKind.String);
            }
        }
        public static string getKeyValue(string keyName)
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\The Chronicles of Spellborn");
                if (registryKey != null)
                {
                    try
                    {
                        if (registryKey.GetValue(keyName) != null)
                        {
                            return registryKey.GetValue(keyName).ToString();
                        }
                    }
                    catch
                    {
                        return "false";
                    }
                    return "false";
                }
                return "false";
            }
            catch (Exception)
            {
                return "false";
            }
        }

        public static void updateKeyValue(string keyName, string keyValue)
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\The Chronicles of Spellborn", writable: true);
                if (registryKey != null)
                {
                    registryKey.SetValue(keyName, keyValue);
                    return;
                }
                using RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("Software\\The Chronicles of Spellborn", writable: true);
                registryKey2.SetValue(keyName, keyValue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static void deleteKeyValue(string keyName)
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\The Chronicles of Spellborn", writable: true);
                registryKey.DeleteValue(keyName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static bool detectInstallation()
        {
            try
            {
                using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\The Chronicles of Spellborn");
                if (registryKey != null)
                {
                    try
                    {
                        if (registryKey.GetValue("installPath") != null)
                        {
                            return true;
                        }
                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
    public class SpellbornSupporter
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        private string installedVersion;
        private dynamic jsonLatest;
        private MessageBoxResult dialogResult;
        private string installPath;
        private bool downloadFinished;
        private int totalFiles;
        private const char _block = '■';
        private int filesExtracted;
        private string currentVersion;
        private dynamic updateJson;
        private string stringUpdateFile;
        public string passOverFromWeb = "";
        private bool enableLaunch;
        private static void LogDebug(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
        }
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SpellbornSupporter()
        {
            installPath = registryManipulation.getKeyValue("installPath");
        }

        public string GetGamePathFromArgs(string[] args)
        {
            try 
            {
                LogDebug($"Processing args: {string.Join(", ", args)}");
                string gamePath = args[0].Contains("gamepath=") ? Program.TextFollowing(args[0], "gamepath=") : null;
                if (string.IsNullOrEmpty(gamePath))
                {
                    _log.Warn("Game path not found in args, using default path");
                    return "D:\\Games\\Spellborn";
                }

                gamePath = gamePath.Contains(":?") ? gamePath.Split(":?")[0] : gamePath;
                gamePath = gamePath.Replace("%22", "").Replace("%5C", "/");

                LogDebug($"Extracted game path: {gamePath}");
                return gamePath;
            }
            catch (Exception e)
            {
                _log.Error("Error in GetGamePathFromArgs", e);
                return "D:\\Games\\Spellborn";
            }
        }

        public void StartupRoutine(string[] args)
        {
            try
            {
                LogDebug("Starting Spellborn startup routine");
                string gamePath;
                try
                {
                    gamePath = GetGamePathFromArgs(args);
                    passOverFromWeb = gamePath;
                    LogDebug($"Game path set: {gamePath}");
                }
                catch (Exception e)
                {
                    LogDebug($"Error in GetGamePathFromArgs: {e.Message}");
                    _log.Error("Error in GetGamePathFromArgs", e);
                    throw new Exception("Failed to get game path from args", e);
                }
                
                bool installDirectoryEnsured;
                try
                {
                    installDirectoryEnsured = EnsureInstallDirectory();
                    LogDebug($"Install directory ensured: {installDirectoryEnsured}");
                }
                catch (Exception e)
                {
                    LogDebug($"Error in EnsureInstallDirectory: {e.Message}");
                    _log.Error("Error in EnsureInstallDirectory", e);
                    throw new Exception("Failed to ensure install directory", e);
                }

                bool versionUpdated;
                try
                {
                    CheckAndUpdateVersion();
                    LogDebug($"Version checked and updated");
                }
                catch (Exception e)
                {
                    LogDebug($"Error in CheckAndUpdateVersion: {e.Message}");
                    _log.Error("Error in CheckAndUpdateVersion", e);
                    throw new Exception("Failed to check and update version", e);
                }
                
                if (enableLaunch)
                {
                    bool gameLaunched;
                    try
                    {
                        LaunchGame();
                        LogDebug($"Game launched");
                    }
                    catch (Exception e)
                    {
                        LogDebug($"Error in LaunchGame: {e.Message}");
                        _log.Error("Error in LaunchGame", e);
                        throw new Exception("Failed to launch game", e);
                    }
                }
            }
            catch(Exception e) 
            {
                LogDebug($"Error in StartupRoutine: {e.Message}");
                _log.Error("Error in StartupRoutine", e);
                Console.WriteLine($"An error occurred: {e.Message}");
                Console.ReadLine();
                throw; // Re-throw the exception to pass it to the calling function
            }
        }

        private bool EnsureInstallDirectory()
        {
            try
            {
                if (!Directory.Exists(passOverFromWeb))
                {
                    LogDebug($"Creating directory: {passOverFromWeb}");
                    Directory.CreateDirectory(passOverFromWeb);
                }

                if (!registryManipulation.detectInstallation())
                {
                    LogDebug("Clean install detected/no registry installpath key found");
                    installPath = passOverFromWeb;
                }

                return true;
            }
            catch (Exception e)
            {
                LogDebug("Error in EnsureInstallDirectory {e}");
                return false;
            }
        }

        private void CheckAndUpdateVersion()
        {
            installedVersion = registryManipulation.getKeyValue("installedVersion");
            LogDebug($"Installed version: {installedVersion}");

            jsonLatest = updateHandler.getJsonItem("latest.json");
            LogDebug("Fetched latest.json file");

            if (installedVersion != jsonLatest.version.ToString())
            {
                LogDebug("Version mismatch detected");
                if (installedVersion == "false")
                {
                    LogDebug("Clean install detected, proceeding with full install");
                    if (!DownloadAndInstallLatestVersion())
                    {
                        throw new Exception("Failed to download and install latest version");
                    }
                }
                else
                {
                    LogDebug("Updating existing installation");
                    CheckUpdates();
                }
            }
            else
            {
                LogDebug("Version is up to date");
                enableLaunch = true;
            }
        }

        private bool DownloadAndInstallLatestVersion()
        {
            try
            {
                LogDebug($"Downloading latest version: {jsonLatest.version}");
                bool downloadSuccess = DownloadFile(jsonLatest.file, jsonLatest.checksum, jsonLatest.version);
                if (!downloadSuccess)
                {
                    LogDebug("Download failed");
                    return false;
                }
                LogDebug("Download completed successfully");
                return true;
            }
            catch (Exception e)
            {
                LogDebug($"Error in DownloadAndInstallLatestVersion: {e.Message}");
                return false;
            }
        }

        public void UnzipFile(string file, string version)
        {
            try
            {
                LogDebug($"Unzipping file: {file}, version: {version}");
                using (Ionic.Zip.ZipFile zipFile = Ionic.Zip.ZipFile.Read(file))
                {
                    totalFiles = zipFile.Count;
                    filesExtracted = 0;
                    zipFile.ExtractProgress += ZipExtractProgress;
                    zipFile.ExtractAll(installPath, ExtractExistingFileAction.OverwriteSilently);
                }

                LogDebug($"Updating registry with new version: {version}");
                registryManipulation.updateKeyValue("installedVersion", version);

                CheckUpdates();
            }
            catch (Exception e)
            {
                _log.Error("Error in UnzipFile", e);
            }
        }

        private bool CheckUpdates()
        {
            try
            {
                currentVersion = registryManipulation.getKeyValue("installedVersion");
                LogDebug($"Checking updates for version: {currentVersion}");

                dynamic updates = updateHandler.fetchUpdates();
                foreach (var update in updates)
                {
                    if (update.Update.appliesTo == currentVersion)
                    {
                        LogDebug("Found applicable update");
                        if (update.Update.enabled == "false")
                        {
                            LogDebug("Update is not yet enabled");
                            EnablePlayButton();
                            return false;
                        }

                        LogDebug("Downloading update");
                        DownloadFile(update.Update.file, update.Update.checksum, update.Update.version);
                        return true;
                    }
                }

                LogDebug("No updates found, enabling play button");
                EnablePlayButton();
                return false;
            }
            catch (Exception e)
            {
                _log.Error("Error in CheckUpdates", e);
                return false;
            }
        }

        private void ZipExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                filesExtracted++;
                _log.Debug($"Extracting file {filesExtracted} of {totalFiles}");
            }
        }

        private bool DownloadFile(dynamic file, dynamic checksum, dynamic version)
        {
            try
            {
                EnsureWriteAccess();

                string uriString = $"https://files.spellborn.org/{file}";
                stringUpdateFile = file.ToString();

                if (File.Exists(Path.Combine(installPath, file.ToString())))
                {
                    return HandleExistingFile(file, checksum, version);
                }
                else
                {
                    return DownloadNewFile(uriString, file, checksum, version);
                }
            }
            catch (Exception e)
            {
                LogDebug($"Error in DownloadFile: {e.Message}");
                return false;
            }
        }

        private void EnsureWriteAccess()
        {
            try
            {
                string testFile = Path.Combine(installPath, "testfile.txt");
                File.WriteAllText(testFile, "This is a testfile");
                File.Delete(testFile);
            }
            catch (Exception e)
            {
                _log.Error("Write access test failed", e);
                MessageBox.Show("Installation location requires administrator access. Please choose a different location or run as administrator.", "Installation Location Issue", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                installPath = passOverFromWeb;
            }
        }

        private bool HandleExistingFile(dynamic file, dynamic checksum, dynamic version)
        {
            LogDebug($"File {file} already exists, checking checksum");
            if (CalculateMD5(Path.Combine(installPath, file.ToString())) != checksum.ToString())
            {
                LogDebug("Checksum mismatch, re-downloading file");
                return DownloadNewFile($"https://files.spellborn.org/{file}", file, checksum, version);
            }
            else
            {
                LogDebug("File checksum matches, starting extraction");
                UnzipFile(Path.Combine(installPath, file.ToString()), version.ToString());
                return true;
            }
        }

        private bool DownloadNewFile(string uriString, dynamic file, dynamic checksum, dynamic version)
        {
            try
            {
                LogDebug($"Preparing to download file: {file}");
                LogDebug($"Download URI: {uriString}");
                LogDebug($"Checksum: {checksum}");
                LogDebug($"Version: {version}");

                using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += Wc_DownloadFileCompleted;
                    webClient.QueryString.Add("file", file.ToString());
                    webClient.QueryString.Add("checksum", checksum.ToString());
                    webClient.QueryString.Add("version", version.ToString());

                    LogDebug("Starting file download...");
                    webClient.DownloadFileAsync(new Uri(uriString), Path.Combine(installPath, file.ToString()));
                    
                    while (webClient.IsBusy)
                        Thread.Sleep(100);

                    LogDebug("File download completed.");
                }
                return true;
            }
            catch (UriFormatException ex)
            {
                LogDebug($"Invalid URI format: {ex.Message}");
                throw;
            }
            catch (WebException ex)
            {
                LogDebug($"Web error occurred during download: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogDebug($"An unexpected error occurred during download: {ex.Message}");
                throw;
            }
        }
       private static readonly object ConsoleWriterLock = new object();
private int _lastProgress = -1;
private int _lastConsoleLine = -1;

private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
{
    var progress = (int)((double)e.BytesReceived / e.TotalBytesToReceive * 100);
    if (progress != _lastProgress)
    {
        _log.Debug($"Downloading {stringUpdateFile}: {e.BytesReceived / 1024:n0} kb / {e.TotalBytesToReceive / 1024:n0} kb");
        
        int consoleWidth = Console.WindowWidth;
        string progressText = $" [ Downloading {stringUpdateFile}: {e.BytesReceived / 1024:n0} kb / {e.TotalBytesToReceive / 1024:n0} kb ]";
        int progressBarWidth = Math.Max(consoleWidth - progressText.Length - 7, 10); // Ensure minimum width of 10
        int filledWidth = (int)((double)progress / 100 * progressBarWidth);
        string progressBar = $"[{new string('■', filledWidth)}{new string(' ', progressBarWidth - filledWidth)}] {progress}%";
        
        string fullLine = (progressText + " " + progressBar).PadRight(consoleWidth);

        lock (ConsoleWriterLock)
        {
            try
            {
                int currentLine = Console.CursorTop;
                if (_lastConsoleLine == -1)
                {
                    _lastConsoleLine = currentLine;
                }
                else
                {
                    currentLine = _lastConsoleLine;
                }

                if (currentLine >= Console.BufferHeight - 1)
                {
                    Console.SetCursorPosition(0, Console.BufferHeight - 2);
                    Console.WriteLine();
                    currentLine = Console.CursorTop;
                    _lastConsoleLine = currentLine;
                }

                Console.SetCursorPosition(0, currentLine);
                Console.Write(fullLine);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If we encounter an out of range error, reset the console cursor to a safe position
                Console.SetCursorPosition(0, 0);
                _lastConsoleLine = 0;
            }
        }
        
        _lastProgress = progress;
    }
}
        private void Wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _log.Error("Download error", e.Error);
                MessageBox.Show($"An error occurred while downloading: {e.Error.Message}");
                return;
            }

            LogDebug("Download completed");
            WebClient webClient = (WebClient)sender;
            string file = webClient.QueryString["file"];
            string checksum = webClient.QueryString["checksum"];
            string version = webClient.QueryString["version"];

            if (updateHandler.checkIfChecksumMatches(Path.Combine(installPath, file), checksum))
            {
                LogDebug("File checksum matches");
                UnzipFile(Path.Combine(installPath, file), version);
            }
            else
            {
                _log.Error("File checksum mismatch");
                MessageBox.Show("The download appears to be invalid. Please restart the launcher to try again.");
            }
        }
        private static string CalculateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            using (FileStream stream = File.OpenRead(filename))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private void EnablePlayButton()
        {
            LogDebug("Play button enabled");
            enableLaunch = true;
        }

        private void LaunchGame()
        {
            try
            {
                string clientPath = Path.Combine(installPath, "bin", "client", "Sb_client.exe");
                LogDebug($"Launching game from: {clientPath}");
                Process.Start(clientPath);
            }
            catch (Exception e)
            {
                _log.Error("Error launching game", e);
                MessageBox.Show($"Failed to launch the game: {e.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    public class FFXIVhandler
    {
        private static string DecryptPassword(string encryptedPassword, string hash)
        {
            try
            {
                Console.WriteLine("Starting password decryption");
                Console.WriteLine($"Hash received: {hash}");

                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                Console.WriteLine($"Encrypted bytes length: {encryptedBytes.Length}");

                byte[] keyBytes = Encoding.UTF8.GetBytes(hash.Substring(0, 16));
                Console.WriteLine($"Key bytes length: {keyBytes.Length}");

                byte[] decryptedBytes = new byte[encryptedBytes.Length];
                for (int i = 0; i < encryptedBytes.Length; i++)
                {
                    decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
                }

                string decryptedPassword = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine("Decryption completed successfully");
#if DEBUG
                Console.WriteLine($"Decrypted password: {decryptedPassword}");
#endif
                return decryptedPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decrypting password: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }





        public static void HandleFFXivReq(string[] args)
        {
            string username = "";
            string password = "";
            string hash = "";
            string otp = "";
            string gamepath = "";
            string issteams = "";
            int expansionLevel = 0;
            int region = 3;
            int langs = 1;
            bool dx11 = true;
            bool isSteam = false;

            var gateTask = Task.Run(() => networklogic.CheckGateStatus());
    var loginTask = Task.Run(() => networklogic.CheckLoginStatus());
    
    Task.WaitAll(new[] { gateTask, loginTask }, 3000); // 3 second timeout for both checks
    
    if (!gateTask.Result || !loginTask.Result)
    {
        return;
    }

#if DEBUG
            Console.WriteLine(args[0]);
#else
        Console.WriteLine("");
#endif

            if (args[0].Contains("?login="))
            {
                string usernameunsanitized = Program.TextFollowing(args[0], "login=");
                if (usernameunsanitized.Contains(':'))
                    username = usernameunsanitized.Split(':')[0];
#if DEBUG
                Console.WriteLine(username);
#else
            Console.Write("");
#endif
            }

            if (args[0].Contains(":?pass="))
            {
                string passunsanitized = Program.TextFollowing(args[0], ":?pass=");
                if (passunsanitized.Contains(':'))
                {
                    string encryptedPassword = passunsanitized.Split(':')[0];
                    hash = Program.TextFollowing(args[0], ":?hash=").Split(':')[0];
                    password = DecryptPassword(encryptedPassword, hash);
                }
#if DEBUG
                Console.WriteLine(password);
#else
            Console.Write("");
#endif
            }

            if (args[0].Contains(":?otp="))
            {
                string otpns = Program.TextFollowing(args[0], ":?otp=");
                if (otpns.Contains(':'))
                    otp = otpns.Split(':')[0];
#if DEBUG
                Console.WriteLine(otp);
#else
            Console.Write("");
#endif
            }

            if (args[0].Contains(":?gamepath="))
            {
                string gamepathns = Program.TextFollowing(args[0], ":?gamepath=");
                string gamepathcst = "";
                string secsanitationstep = "";
                string thirdsanitationstep = "";
                if (gamepathns.Contains(":?"))
                    gamepathcst = gamepathns.Split(":?")[0];
                if (gamepathcst.Contains("%22"))
                    secsanitationstep = gamepathcst.Replace("%22", "");
                else
                    secsanitationstep = gamepathcst;
                if (gamepathcst.Contains("%5C"))
                    thirdsanitationstep = secsanitationstep.Replace("%5C", "/");
                gamepath = thirdsanitationstep;
#if DEBUG
                Console.WriteLine(gamepath);
#else
            Console.Write("");
#endif
            }

            if (args[0].Contains(":?issteam="))
            {
                string issteamsns = Program.TextFollowing(args[0], ":?issteam=");
                if (issteamsns.Contains(':'))
                    issteams = issteamsns.Split(':')[0];
#if DEBUG
                Console.WriteLine(issteams);
#else
            Console.Write("");
#endif
                isSteam = issteams == "yes";
            }

            var xpacPath = "";
            if (Program.ReturnXpacNum(1) == "ex1") xpacPath = "ex1";
            if (Program.ReturnXpacNum(2) == "ex2") xpacPath = "ex2";
            if (Program.ReturnXpacNum(3) == "ex3") xpacPath = "ex3";
            if (Program.ReturnXpacNum(4) == "ex4") xpacPath = "ex4";

            var sqpack = $@"{gamepath}\sqpack\{xpacPath}";
            expansionLevel = xpacPath switch
            {
                "ex1" => 1,
                "ex2" => 2,
                "ex3" => 3,
                "ex4" => 4,
                _ => 0
            };

#if DEBUG
            Console.WriteLine(expansionLevel);
            Console.Write("\n");
#endif

            try
            {
                Console.WriteLine($"Attempting to get SID for user {username} at path {gamepath}");
    var sid = networklogic.GetRealSid(gamepath, username, password, otp, isSteam);
    
    if (sid.Equals("BAD"))
    {
        Console.WriteLine("Failed to obtain valid SID");
#if DEBUG
            Console.WriteLine(sid);
#else
            Console.Write("");
#endif
        return;
    }
    
    Console.WriteLine("SID obtained successfully, launching game...");
    var ffxivGame = networklogic.LaunchGameAsync(gamepath, sid, langs, dx11, expansionLevel, isSteam, region);
    
    Console.WriteLine($"Game launch initiated with expansion level {expansionLevel}");
    if (ffxivGame != null)
    {
        Console.WriteLine("Game process started successfully");
    }
            }
            catch (Exception exc)
            {
                Console.WriteLine($"FFXIV Launch Error: {exc.Message}");
    Console.WriteLine($"Stack trace: {exc.StackTrace}");
    throw;
            }
        }
    }

}
