using LibLaunchSupport;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;

namespace WMConsole
{
    class Program
    {
        static string TextFollowing(string txt, string value)
        {
            if (!String.IsNullOrEmpty(txt) && !String.IsNullOrEmpty(value))
            {
                int index = txt.IndexOf(value);
                if (-1 < index)
                {
                    int start = index + value.Length;
                    if (start <= txt.Length)
                    {
                        return txt.Substring(start);
                    }
                }
            }
            return null;
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Listening.."); //Gets the current location where the file is downloaded   
            var loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string locruntconf = loc.Replace(".dll", ".runtimeconfig.json");
            string locedit = loc.Replace(".dll", ".exe");
            string reqasemb = loc.Replace("WMconsole.dll", "LibLaunchSupport.dll");
            if (!Directory.Exists(@"D:\HandleGame\"))
            {
                System.IO.Directory.CreateDirectory(@"D:\HandleGame\");
            } //Creates the Downloaded file in the specified folder  
            if (!File.Exists(@"D:\HandleGame\" + locedit.Split('\\').Last()))
            {
                File.Copy(loc, @"D:\HandleGame\" + loc.Split('\\').Last());
                File.Copy(locedit, @"D:\HandleGame\" + locedit.Split('\\').Last());
                File.Copy(locruntconf, @"D:\HandleGame\" + locruntconf.Split('\\').Last());
                File.Copy(reqasemb, @"D:\HandleGame\" + reqasemb.Split('\\').Last());
            }
            var KeyTest = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("Classes", true);
            RegistryKey key = KeyTest.CreateSubKey("HandleWebRequest");
            key.SetValue("URL Protocol", "HandleReqLaunchFFXIV"); ;
            key.CreateSubKey(@"shell\open\command").SetValue("", @"D:\HandleGame\WMConsole.exe %1");
            if (args != null && args.Length > 0)
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
                Console.WriteLine(args[0]);
                if (args[0].Contains("?login="))
                {
                    string usernameunsanitized = TextFollowing(args[0],"login=");
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
                    string passunsanitized = TextFollowing(args[0], ":?pass=");
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
                    string otpns = TextFollowing(args[0], ":?otp=");
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
                    string gamepathns = TextFollowing(args[0], ":?gamepath=" );
                    string gamepathcst = "";
                    string secsanitationstep = "";
                    string thirdsanitationstep = "";
                    if (gamepathns.Contains(":?"))
                        gamepathcst = gamepathns.Split(":?")[0];
                    if (gamepathcst.Contains("%22")) { secsanitationstep = gamepathcst.Replace("%22", "");} else { secsanitationstep= gamepathcst;}
                    if (gamepathcst.Contains("%5C")) { thirdsanitationstep = secsanitationstep.Replace("%5C", "/");}
                    gamepath= thirdsanitationstep;
#if DEBUG
                    Console.WriteLine(gamepath);
#else
                    Console.Write("");
#endif
                }
                if (args[0].Contains(":?issteam="))
                {
                    string issteamsns = TextFollowing(args[0], ":?issteam=");
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
                if(Directory.Exists(gamepath + "game/sqpack/ex1"))
                {
                    expansionLevel= 1;
                }
                if (Directory.Exists(gamepath + "game/sqpack/ex2"))
                {
                    expansionLevel = 2;
                }
                if (Directory.Exists(gamepath + "game/sqpack/ex3"))
                {
                    expansionLevel = 3;
                }
                if (Directory.Exists(gamepath + "game/sqpack/ex4"))
                {
                    expansionLevel = 4;
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
                Console.ReadLine();
            }
            if (args == null || args.Length == 0)
            {
                {
                    Console.WriteLine("Protocol handler has been registered- you may now proceed to https://pieckenst.github.io/WebLaunch-FFXIV/ to launch the game");
                    




                }

            }

        }
    }
}