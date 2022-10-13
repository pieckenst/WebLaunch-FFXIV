using LibLaunchSupport;
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
using WMConsole;

using System.ComponentModel;
using System.Diagnostics;
using Ionic.Zip;
using Konsole;

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
    public class spellbornsupporter
    {
        private string installedVersion;

        private dynamic jsonLatest;

        private MessageBoxResult dialogResult;

        private string installPath = registryManipulation.getKeyValue("installPath");

        private bool downloadFinished;

        private int totalFiles;
        const char _block = '■';

        private int filesExtracted;

        private string currentVersion;

        private dynamic updateJson;
        private string stringupdfile;
        public string passoverfromweb;
        private bool enableLaunch;
        
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string dogamepathreturn (string[] args) {
            string gamepathns = Program.TextFollowing(args[0], ":?gamepath=");
            string gamepathcst = "";
            string secsanitationstep = "";
            string thirdsanitationstep = "";
            if (gamepathns.Contains(":?"))
                gamepathcst = gamepathns.Split(":?")[0];
            if (gamepathcst.Contains("%22")) { secsanitationstep = gamepathcst.Replace("%22", ""); } else { secsanitationstep = gamepathcst; }
            if (gamepathcst.Contains("%5C")) { thirdsanitationstep = secsanitationstep.Replace("%5C", "/"); }
            return thirdsanitationstep;
        }
        public void startupRoutine(string[] args)
        {
            passoverfromweb = dogamepathreturn(args);
            bool folderExists = Directory.Exists(passoverfromweb);
            try {
                if (!folderExists)
                {
                    Directory.CreateDirectory(passoverfromweb);
                }
                if (!registryManipulation.detectInstallation())
                {
                    _log.Info("Clean install detected/no registry installpath key found");

                    installPath = passoverfromweb;

                }
                installedVersion = registryManipulation.getKeyValue("installedVersion");
#if DEBUG
                Console.WriteLine("Latest installed version is " + installedVersion.ToString());
#else
                    Console.Write("");
#endif
                _log.Info("Fetching latest version from the file server");
                jsonLatest = updateHandler.getJsonItem("latest.json");
                _log.Info("Managed to get the latest.json file");
                if (installedVersion != jsonLatest.version.ToString())
                {
                    _log.Info("Currently installed version does not match with latest.json");
                    if (installedVersion == "false")
                    {
                        _log.Info("Found nothing in the registry, this is most likely a clean install. Go forwards with full install.");
                        _log.Info("Requested browser to load the launcher welcome/install page ");
                        _log.Info("Calling download function to download newest versions. Information passed through is: filename: " + jsonLatest.file + " checksum: " + jsonLatest.checksum + " version: " + jsonLatest.version);
                        Console.WriteLine("Calling download function to download newest versions. Information passed through is: filename: " + jsonLatest.file + " checksum: " + jsonLatest.checksum + " version: " + jsonLatest.version);
                        downloadFile(jsonLatest.file, jsonLatest.checksum, jsonLatest.version);
                    }
                    
                }
#if DEBUG
                Console.WriteLine("Version doesn't match, but registry tells that we are installed. Proceeding with fetching updates.");
#else
                Console.Write("");
#endif
                checkUpdates();
                if (enableLaunch)
                {
                    Process.Start(installPath + "\\bin\\client\\Sb_client.exe");

                }
            }
            catch(Exception e) 
            {
                Console.WriteLine("An error was encountered during execution- the debug info below is listed");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.InnerException);
                Console.WriteLine(e.StackTrace);
                _log.Error(e.Message);
                Console.ReadLine();
            }
        }
        public void unzipFile(string file, string version)
        {
            Console.WriteLine("Starting with unzipping ZIP file. Filename: " + file + " version: " + version);
            using (Ionic.Zip.ZipFile zipFile = Ionic.Zip.ZipFile.Read(file))
            {
                totalFiles = zipFile.Count;
                filesExtracted = 0;
                
                zipFile.ExtractAll(installPath, ExtractExistingFileAction.OverwriteSilently);
            }
            Console.WriteLine("Unzipping completed, storing new version " + version + " in the registry");
            registryManipulation.updateKeyValue("installedVersion", version);
            Console.WriteLine("Stored new version in registry, new value is " + version);

            
            Console.WriteLine("Checking for updates again.");
            checkUpdates();
        }

        private bool checkUpdates()
        {
            currentVersion = registryManipulation.getKeyValue("installedVersion");
            Console.WriteLine("Getting installed version from registry. It is " + installedVersion);
            Console.WriteLine("Fetch the updates.json file from file server");
            dynamic updates = updateHandler.fetchUpdates();
            int i;
            for (i = 0; i < updates.Count; i++)
            {
                if (updates[i].Update.appliesTo == currentVersion)
                {
                    Console.WriteLine("We found an update applicable to us");
                    if (updates[i].Update.enabled == "false")
                    {
                        #if DEBUG
                        Console.WriteLine("Update is applicable to us, but not yet enabled. Take no action and enable play button");
                        #else 
                        Console.Write("");
                        #endif
                        enablePlayButton();
                        return false;
                    }
#if DEBUG
                    Console.WriteLine("Update found that is applicable to us and it is enabled. ");
#else
                    Console.Write("");
#endif



                    Console.WriteLine("Calling download function to download update. Information passed through is: filename: " + updates[i].Update.file + " checksum: " + updates[i].Update.checksum + " version: " + updates[i].Update.version);
                    downloadFile(updates[i].Update.file, updates[i].Update.checksum, updates[i].Update.version);
                    return true;
                }
            }
            _log.Info("All seems OK, enable the play button.");
            enablePlayButton();

            return false;
        }
        private void ZipExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_BeforeExtractEntry)
            {
                filesExtracted++;
                
            }
        }

        private void downloadFile(dynamic file, dynamic checksum, dynamic version)
        {
            try
            {
                _log.Info("Test if we have write access - if this fails, remove registry key and ask for different install location. Remove test file afterwards.");
                File.WriteAllText(Path.Combine(installPath, "testfile.txt"), "This is a testfile");
                File.Delete(installPath + "\\testfile.txt");
            }
            catch
            {
                _log.Error("That did not go according to plan, writing seems to have failed.");
                
                    MessageBox.Show("You have tried to install The Chronicles of Spellborn in a location that requires administrator access. Since Spellborn is so old, this is not recommended. We will open up a file browser so you can reset your installation location. To keep your current install location, please restart this launcher as administrator.", "There is an issue with the install location", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                
                _log.Error("Removed installpath in registry to allow picking new path.");
                //registryManipulation.deleteKeyValue("installpath");
                installPath = passoverfromweb;
                _log.Info("Closing this window, installpath has been removed - restarting application");
            }
            string uriString = "https://files.spellborn.org/" + file;
            stringupdfile= file;
            _log.Info("Check if the file already exists");
            if (File.Exists(installPath + "/" + file))
            {
                _log.Info("File with same name already found (" + file + "). Checking if checksum matches.");
                
                _log.Info("Calculating MD5 checksum of the downloaded file " + file);
                if (CalculateMD5(installPath + "/" + file) != checksum.ToString())
                {
                    _log.Info("Checksum calculated and compared - file does not match, download again.");
                    
                    _log.Info("Restarting download");
                    using WebClient webClient = new WebClient();
                    webClient.DownloadProgressChanged += wc_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += wc_DownloadFileCompleted;
                    webClient.QueryString.Add("file", file.ToString());
                    webClient.QueryString.Add("checksum", checksum.ToString());
                    webClient.QueryString.Add("version", version.ToString());
                    webClient.DownloadFileAsync(new Uri(uriString), installPath + "/" + file);
                    while (webClient.IsBusy)
                        System.Threading.Thread.Sleep(1000);
                    Console.Write("  COMPLETED!");
                    Console.WriteLine();
                }
                else
                {
                    _log.Info("File matches checksum, starting extraction");
                    
                    unzipFile((installPath + "\\" + file).ToString(), version.ToString());
                }
            }
            else
            {
                Console.WriteLine("File not found, downloading");
                using WebClient webClient2 = new WebClient();
                webClient2.DownloadProgressChanged += wc_DownloadProgressChanged;
                webClient2.DownloadFileCompleted += wc_DownloadFileCompleted;
                webClient2.QueryString.Add("file", file.ToString());
                webClient2.QueryString.Add("checksum", checksum.ToString());
                webClient2.QueryString.Add("version", version.ToString());
                webClient2.DownloadFileAsync(new Uri(uriString), installPath + "/" + file);
                while (webClient2.IsBusy)
                    System.Threading.Thread.Sleep(1000);
                Console.Write("  COMPLETED!");
                Console.WriteLine();
            }
        }
        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var p = e.BytesReceived / 1000;
            Console.Write("\r [ Downloading " + stringupdfile + ": " + string.Format("{0:n0}", e.BytesReceived / 1000) + " kb" + "]");
            Console.WriteLine() ;
            Console.Write("\n [");
            for (int i =0; i < 100; i++)
            {
                if (i >= p)
                    Console.Write(' ');
                else
                    Console.Write(_block);
            }
            Console.Write("] \n");




        }
        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            
            if (e.Error != null)
            {
                MessageBox.Show("An error ocurred while trying to download file:" + e.Error.Message);
                _log.Error("Something went wrong during the download: " + e.Error.Message);
                return;
            }
            _log.Info("Download completed");
            
            string text = ((WebClient)sender).QueryString["file"];
            string checksum = ((WebClient)sender).QueryString["checksum"];
            string version = ((WebClient)sender).QueryString["version"];
            _log.Info("Verifying checksum of downloaded file.");
            if (updateHandler.checkIfChecksumMatches(installPath + "/" + text, checksum))
            {
                _log.Info("File checksum matches");
                
                unzipFile((installPath + "\\" + text).ToString(), version);
            }
            else
            {
                _log.Error("File checksum does not match, download invalid. Restart launcher to try again.");
                
                MessageBox.Show("The download looks to be invalid, please relaunch the launcher to try again.");
            }
        }

        private static string CalculateMD5(string filename)
        {
            using MD5 mD = MD5.Create();
            using FileStream inputStream = File.OpenRead(filename);
            return BitConverter.ToString(mD.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant();
        }
        private void enablePlayButton()
        {
            
            _log.Info("Play button enabled");
            enableLaunch = true;
        }
    } 
    public class FFXIVhandler
    {
        public static void HandleFFXivReq(string[] args)
        {
            string username = "";
            string password = "";
            string otp = "";
            string gamepath = "";
            string issteams = "";
            int expansionLevel = 0;
            int region = 3;
            int langs = 1;
            bool dx11 = true;
            bool isSteam = false;
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
                    password = passunsanitized.Split(':')[0];
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
                if (gamepathcst.Contains("%22")) { secsanitationstep = gamepathcst.Replace("%22", ""); } else { secsanitationstep = gamepathcst; }
                if (gamepathcst.Contains("%5C")) { thirdsanitationstep = secsanitationstep.Replace("%5C", "/"); }
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
                Console.WriteLine(issteams);
                if (issteams == "yes")
                {
                    isSteam = true;
                }
                else
                {
                    isSteam = false;
                }
            }
            var xpacPath = "";
            if (Program.ReturnXpacNum(1) == "ex1") { xpacPath = "ex1"; }
            if (Program.ReturnXpacNum(2) == "ex2") { xpacPath = "ex2"; }
            if (Program.ReturnXpacNum(3) == "ex3") { xpacPath = "ex3"; }
            if (Program.ReturnXpacNum(4) == "ex4") { xpacPath = "ex4"; }
            var sqpack = $@"{gamepath}\sqpack\{xpacPath}";

            if (xpacPath == "ex1")
            {
                expansionLevel = 1;
                Console.WriteLine(expansionLevel);
            }
            if (xpacPath == "ex2")
            {
                expansionLevel = 2;
                Console.WriteLine(expansionLevel);
            }
            if (xpacPath == "ex3")
            {
                expansionLevel = 3;
                Console.WriteLine(expansionLevel);
            }
            if (xpacPath == "ex4")
            {
                expansionLevel = 4;
                Console.WriteLine(expansionLevel);
            }






           

            try
            {
                var sid = networklogic.GetRealSid(gamepath, username, password, otp, isSteam);
                if (sid.Equals("BAD"))
                    return;

                var ffxivGame = networklogic.LaunchGame(gamepath, sid, langs, dx11, expansionLevel, isSteam, region);



            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            
        }
    }
}
