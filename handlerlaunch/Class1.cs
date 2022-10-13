using LibLaunchSupport;
using SpinningWheelLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WMConsole;

namespace handlerlaunch
{
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
