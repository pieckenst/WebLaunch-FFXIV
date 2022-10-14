using LibDalamud.Common.Util;
using System.Text;
using XIVLauncher.Common.Util;
using static LibDalamud.Common.Util.PlatformHelpers;

namespace LibDalamud
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SettingsDescriptionAttribute : Attribute
    {
        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public SettingsDescriptionAttribute(string friendlyName, string description)
        {
            this.FriendlyName = friendlyName;
            this.Description = description;
        }
    }
    public static class RepoExtensions
    {
        public const string BASE_GAME_VERSION = "2012.01.01.0000.0000";
        private static DirectoryInfo GetRepoPath(this Repository repo, DirectoryInfo gamePath)
        {
            switch (repo)
            {
                case Repository.Boot:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "boot"));
                case Repository.Ffxiv:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "game"));
                case Repository.Ex1:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "game", "sqpack", "ex1"));
                case Repository.Ex2:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "game", "sqpack", "ex2"));
                case Repository.Ex3:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "game", "sqpack", "ex3"));
                case Repository.Ex4:
                    return new DirectoryInfo(Path.Combine(gamePath.FullName, "game", "sqpack", "ex4"));
                default:
                    throw new ArgumentOutOfRangeException(nameof(repo), repo, null);
            }
        }

        public static FileInfo GetVerFile(this Repository repo, DirectoryInfo gamePath, bool isBck = false)
        {
            var repoPath = repo.GetRepoPath(gamePath).FullName;
            switch (repo)
            {
                case Repository.Boot:
                    return new FileInfo(Path.Combine(repoPath, "ffxivboot" + (isBck ? ".bck" : ".ver")));
                case Repository.Ffxiv:
                    return new FileInfo(Path.Combine(repoPath, "ffxivgame" + (isBck ? ".bck" : ".ver")));
                case Repository.Ex1:
                    return new FileInfo(Path.Combine(repoPath, "ex1" + (isBck ? ".bck" : ".ver")));
                case Repository.Ex2:
                    return new FileInfo(Path.Combine(repoPath, "ex2" + (isBck ? ".bck" : ".ver")));
                case Repository.Ex3:
                    return new FileInfo(Path.Combine(repoPath, "ex3" + (isBck ? ".bck" : ".ver")));
                case Repository.Ex4:
                    return new FileInfo(Path.Combine(repoPath, "ex4" + (isBck ? ".bck" : ".ver")));
                default:
                    throw new ArgumentOutOfRangeException(nameof(repo), repo, null);
            }
        }

        public static string GetVer(this Repository repo, DirectoryInfo gamePath, bool isBck = false)
        {
            var verFile = repo.GetVerFile(gamePath, isBck);

            if (!verFile.Exists)
                return BASE_GAME_VERSION;

            var ver = File.ReadAllText(verFile.FullName);
            return string.IsNullOrWhiteSpace(ver) ? BASE_GAME_VERSION : ver;
        }

        public static void SetVer(this Repository repo, DirectoryInfo gamePath, string newVer, bool isBck = false)
        {
            var verFile = GetVerFile(repo, gamePath, isBck);

            if (!verFile.Directory.Exists)
                verFile.Directory.Create();

            using var fileStream = verFile.Open(FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = Encoding.ASCII.GetBytes(newVer);
            fileStream.Write(buffer, 0, buffer.Length);
            fileStream.Flush();
        }

        public static bool IsBaseVer(this Repository repo, DirectoryInfo gamePath)
        {
            return repo.GetVer(gamePath) == BASE_GAME_VERSION;
        }

        // TODO
        public static string GetRepoHash(this Repository repo)
        {
            switch (repo)
            {
                case Repository.Boot:
                    return null;
                case Repository.Ffxiv:
                    return null;
                case Repository.Ex1:
                    return null;
                case Repository.Ex2:
                    return null;
                case Repository.Ex3:
                    return null;
                case Repository.Ex4:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(repo), repo, null);
            }
        }
    }
    public class Paths
    {
        static Paths()
        {
            RoamingPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher");
        }

        public static string RoamingPath { get; private set; }

        public static string ResourcesPath => Path.Combine(AppContext.BaseDirectory, "Resources");

        public static void OverrideRoamingPath(string path)
        {
            RoamingPath = Environment.ExpandEnvironmentVariables(path);
        }
    }
    public static class EnvironmentSettings
    {
        public static bool IsWine => CheckEnvBool("XL_WINEONLINUX");
        public static bool IsHardwareRendered => CheckEnvBool("XL_HWRENDER");
        public static bool IsDisableUpdates => CheckEnvBool("XL_NOAUTOUPDATE");
        public static bool IsPreRelease => CheckEnvBool("XL_PRERELEASE");
        public static bool IsNoRunas => CheckEnvBool("XL_NO_RUNAS");
        public static bool IsIgnoreSpaceRequirements => CheckEnvBool("XL_NO_SPACE_REQUIREMENTS");
        private static bool CheckEnvBool(string var) => bool.Parse(System.Environment.GetEnvironmentVariable(var) ?? "false");
    }
    public enum Repository
    {
        Boot,
        Ffxiv,
        Ex1,
        Ex2,
        Ex3,
        Ex4
    }
    public static class ClientLanguageExtensions
    {
        public static string GetLangCode(this ClientLanguage language)
        {
            switch (language)
            {
                case ClientLanguage.Japanese:
                    return "ja";

                case ClientLanguage.English when GameHelpers.IsRegionNorthAmerica():
                    return "en-us";

                case ClientLanguage.English:
                    return "en-gb";

                case ClientLanguage.German:
                    return "de";

                case ClientLanguage.French:
                    return "fr";

                default:
                    return "en-gb";
            }
        }
    }
    public enum ClientLanguage
    {
        Japanese,
        English,
        German,
        French
    }
    public enum DpiAwareness
    {
        Aware,
        Unaware,
    }
    public static class Constants
    {
        public const string BASE_GAME_VERSION = "2012.01.01.0000.0000";

        public const uint STEAM_APP_ID = 39210;
        public const uint STEAM_FT_APP_ID = 312060;

        public static string PatcherUserAgent => GetPatcherUserAgent(PlatformHelpers.GetPlatform());

        private static string GetPatcherUserAgent(Platform platform)
        {
            switch (platform)
            {
                case Platform.Win32:
                case Platform.Win32OnLinux:
                    return "FFXIV PATCH CLIENT";

                case Platform.Mac:
                    return "FFXIV-MAC PATCH CLIENT";

                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }
    }
}