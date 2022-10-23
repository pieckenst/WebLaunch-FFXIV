using handlerlaunch;
using CoreLibLaunchSupport;
using Microsoft.Win32;
using SpinningWheelLib;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace WMConsole
{
    class Program
    {
        public static string GetExpansionFolder(byte expansionId) =>
            expansionId == 0 ? "ffxiv" : $"ex{expansionId}";
        public static string TextFollowing(string txt, string value)
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
        public static string ReturnXpacNum(ushort expansionId)
        {
            var processxpac= GetExpansionFolder((byte)expansionId);
            return processxpac;
        }
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Protocol Handler Process";
            Console.WriteLine("Listening.."); //Gets the current location where the file is downloaded   
            var loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string locruntconf = loc.Replace(".dll", ".runtimeconfig.json");
            string locedit = loc.Replace(".dll", ".exe");
            string reqasemb = loc.Replace("WMconsole.dll", "CoreLibLaunchSupport.dll");
            string lib1 = loc.Replace("WMconsole.dll", "SpinningWheelLib.dll");
            string lib2 = loc.Replace("WMconsole.dll", "WpfAnimatedGif.dll");
            string lib3 = loc.Replace("WMconsole.dll", "XamlAnimatedGif.dll");
            string lib4 = loc.Replace("WMconsole.dll", "DotNetZip.dll");
            string lib5 = loc.Replace("WMconsole.dll", "Newtonsoft.Json.dll");
            string lib6 = loc.Replace("WMconsole.dll", "log4net.dll");
            string lib7 = loc.Replace("WMconsole.dll", "Goblinfactory.Konsole.dll");
            string lib8 = loc.Replace("WMconsole.dll", "CommandLine.dll");
            string lib9 = loc.Replace("WMconsole.dll", "Downloader.dll");
            string lib10 = loc.Replace("WMconsole.dll", "Facepunch.Steamworks.Win64.dll");
            string lib11 = loc.Replace("WMconsole.dll", "LibDalamud.dll");
            string lib12 = loc.Replace("WMconsole.dll", "Mono.Nat.dll");
            string lib13 = loc.Replace("WMconsole.dll", "MonoTorrent.dll");
            string lib14 = loc.Replace("WMconsole.dll", "ReusableTasks.dll");
            string lib15 = loc.Replace("WMconsole.dll", "Serilog.dll");
            string lib16 = loc.Replace("WMconsole.dll", "SharedMemory.dll");
            string lib17 = loc.Replace("WMconsole.dll", "Microsoft.Toolkit.Uwp.Notifications.dll");
            string lib18 = loc.Replace("WMconsole.dll", "Microsoft.Windows.SDK.NET.dll");
            string lib19 = loc.Replace("WMconsole.dll", "WinRT.Runtime.dll");
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
                File.Copy(lib1, @"D:\HandleGame\" + lib1.Split('\\').Last());
                File.Copy(lib2, @"D:\HandleGame\" + lib2.Split('\\').Last());
                File.Copy(lib3, @"D:\HandleGame\" + lib3.Split('\\').Last());
                File.Copy(lib4, @"D:\HandleGame\" + lib4.Split('\\').Last());
                File.Copy(lib5, @"D:\HandleGame\" + lib5.Split('\\').Last());
                File.Copy(lib6, @"D:\HandleGame\" + lib6.Split('\\').Last());
                File.Copy(lib7, @"D:\HandleGame\" + lib7.Split('\\').Last());
                File.Copy(lib8, @"D:\HandleGame\" + lib8.Split('\\').Last());
                File.Copy(lib9, @"D:\HandleGame\" + lib9.Split('\\').Last());
                File.Copy(lib10, @"D:\HandleGame\" + lib10.Split('\\').Last());
                File.Copy(lib11, @"D:\HandleGame\" + lib11.Split('\\').Last());
                File.Copy(lib12, @"D:\HandleGame\" + lib12.Split('\\').Last());
                File.Copy(lib13, @"D:\HandleGame\" + lib13.Split('\\').Last());
                File.Copy(lib14, @"D:\HandleGame\" + lib14.Split('\\').Last());
                File.Copy(lib15, @"D:\HandleGame\" + lib15.Split('\\').Last());
                File.Copy(lib16, @"D:\HandleGame\" + lib16.Split('\\').Last());
                File.Copy(lib17, @"D:\HandleGame\" + lib17.Split('\\').Last());
                File.Copy(lib18, @"D:\HandleGame\" + lib18.Split('\\').Last());
                File.Copy(lib19, @"D:\HandleGame\" + lib19.Split('\\').Last());
            }
            var KeyTest = Registry.CurrentUser.OpenSubKey("Software", true).OpenSubKey("Classes", true);
            RegistryKey key = KeyTest.CreateSubKey("HandleWebRequest");
            key.SetValue("URL Protocol", "HandleReqLaunch"); ;
            key.CreateSubKey(@"shell\open\command").SetValue("", @"D:\HandleGame\WMConsole.exe %1");
            if (args != null && args.Length > 0)
            {
                if (args[0].Contains("?ffxivhandle=yes"))
                {
                    FFXIVhandler.HandleFFXivReq(args);
                    Window1 windower = new Window1();
                    windower.Show();
                    Thread.Sleep(10000);
                    windower.Close();
                    var donots = new ToastContentBuilder().AddArgument("action", "viewConversation").AddArgument("conversationId", 9813).AddText("Game process has been launched")
    .AddText("You may now play the game");
                    donots.Show();

                }
                if (args[0].Contains("?spellbornhandle=yes"))
                {
                    spellbornsupporter splbornobj = new spellbornsupporter();
                    splbornobj.startupRoutine(args);
                    Window1 windower = new Window1();
                    windower.Show();
                    Thread.Sleep(10000);
                    windower.Close();
                    var donots = new ToastContentBuilder().AddArgument("action", "viewConversation").AddArgument("conversationId", 9813).AddText("Game process has been launched")
    .AddText("You may now play the game");
                    donots.Show();
                }

            }
            if (args == null || args.Length == 0)
            {
                {
                    var donots = new ToastContentBuilder().AddArgument("action", "viewConversation").AddArgument("conversationId", 9813).AddText("Protocol handler registered")
    .AddText("Protocol handler has been registered- you may now proceed to https://pieckenst.github.io/WebLaunch-FFXIV/ to launch the game");
                    donots.Show();
                    
                    Console.ReadLine();




                }

            }

        }
    }
}