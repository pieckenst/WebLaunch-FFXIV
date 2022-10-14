using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using LibDalamud.Common.Dalamud;
using Microsoft.Win32.SafeHandles;
using System.Reflection;
using XIVLauncher.Common.PlatformAbstractions;
using Serilog;
using XIVLauncher.Common.Addon;
using LibDalamud;
using static XIVLauncher.Common.Game.Launcher;
using XIVLauncher.Common.Encryption;
using XIVLauncher.Common.Game.Exceptions;

namespace CoreLibLaunchSupport
{
    public enum DpiAwareness
    {
        Aware,
        Unaware,
    }
    public class GameExitedException : Exception
    {
        public GameExitedException()
            : base("Game exited prematurely.")
        {
        }
    }
    public class ExistingProcess : Process
    {
        public ExistingProcess(IntPtr handle)
        {
            SetHandle(handle);
        }

        private void SetHandle(IntPtr handle)
        {
            var baseType = GetType().BaseType;
            if (baseType == null)
                return;

            var setProcessHandleMethod = baseType.GetMethod("SetProcessHandle",
                BindingFlags.NonPublic | BindingFlags.Instance);
            setProcessHandleMethod?.Invoke(this, new object[] { new SafeProcessHandle(handle, true) });
        }
    }
    
    public interface IGameRunner
    {
        Process? Start(string path, string workingDirectory, string arguments, IDictionary<string, string> environment, DpiAwareness dpiAwareness);
    }
    public static class NativeAclFix
    {
        // Definitions taken from PInvoke.net (with some changes)
        private static class PInvoke
        {
            #region Constants
            public const UInt32 STANDARD_RIGHTS_ALL = 0x001F0000;
            public const UInt32 SPECIFIC_RIGHTS_ALL = 0x0000FFFF;
            public const UInt32 PROCESS_VM_WRITE = 0x0020;

            public const UInt32 GRANT_ACCESS = 1;

            public const UInt32 SECURITY_DESCRIPTOR_REVISION = 1;

            public const UInt32 CREATE_SUSPENDED = 0x00000004;

            public const UInt32 TOKEN_QUERY = 0x0008;
            public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;

            public const UInt32 PRIVILEGE_SET_ALL_NECESSARY = 1;

            public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
            public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;


            public enum MULTIPLE_TRUSTEE_OPERATION
            {
                NO_MULTIPLE_TRUSTEE,
                TRUSTEE_IS_IMPERSONATE
            }

            public enum TRUSTEE_FORM
            {
                TRUSTEE_IS_SID,
                TRUSTEE_IS_NAME,
                TRUSTEE_BAD_FORM,
                TRUSTEE_IS_OBJECTS_AND_SID,
                TRUSTEE_IS_OBJECTS_AND_NAME
            }

            public enum TRUSTEE_TYPE
            {
                TRUSTEE_IS_UNKNOWN,
                TRUSTEE_IS_USER,
                TRUSTEE_IS_GROUP,
                TRUSTEE_IS_DOMAIN,
                TRUSTEE_IS_ALIAS,
                TRUSTEE_IS_WELL_KNOWN_GROUP,
                TRUSTEE_IS_DELETED,
                TRUSTEE_IS_INVALID,
                TRUSTEE_IS_COMPUTER
            }

            public enum SE_OBJECT_TYPE
            {
                SE_UNKNOWN_OBJECT_TYPE,
                SE_FILE_OBJECT,
                SE_SERVICE,
                SE_PRINTER,
                SE_REGISTRY_KEY,
                SE_LMSHARE,
                SE_KERNEL_OBJECT,
                SE_WINDOW_OBJECT,
                SE_DS_OBJECT,
                SE_DS_OBJECT_ALL,
                SE_PROVIDER_DEFINED_OBJECT,
                SE_WMIGUID_OBJECT,
                SE_REGISTRY_WOW64_32KEY
            }
            public enum SECURITY_INFORMATION
            {
                OWNER_SECURITY_INFORMATION = 1,
                GROUP_SECURITY_INFORMATION = 2,
                DACL_SECURITY_INFORMATION = 4,
                SACL_SECURITY_INFORMATION = 8,
                UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
                UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
                PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000
            }
            #endregion


            #region Structures
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 0)]
            public struct TRUSTEE : IDisposable
            {
                public IntPtr pMultipleTrustee;
                public MULTIPLE_TRUSTEE_OPERATION MultipleTrusteeOperation;
                public TRUSTEE_FORM TrusteeForm;
                public TRUSTEE_TYPE TrusteeType;
                private IntPtr ptstrName;

                void IDisposable.Dispose()
                {
                    if (ptstrName != IntPtr.Zero) Marshal.Release(ptstrName);
                }

                public string Name { get { return Marshal.PtrToStringAuto(ptstrName); } }
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 0)]
            public struct EXPLICIT_ACCESS
            {
                uint grfAccessPermissions;
                uint grfAccessMode;
                uint grfInheritance;
                TRUSTEE Trustee;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_DESCRIPTOR
            {
                public byte Revision;
                public byte Sbz1;
                public UInt16 Control;
                public IntPtr Owner;
                public IntPtr Group;
                public IntPtr Sacl;
                public IntPtr Dacl;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct STARTUPINFO
            {
                public Int32 cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public Int32 dwX;
                public Int32 dwY;
                public Int32 dwXSize;
                public Int32 dwYSize;
                public Int32 dwXCountChars;
                public Int32 dwYCountChars;
                public Int32 dwFillAttribute;
                public Int32 dwFlags;
                public Int16 wShowWindow;
                public Int16 cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public int dwProcessId;
                public UInt32 dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public UInt32 LowPart;
                public Int32 HighPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PRIVILEGE_SET
            {
                public UInt32 PrivilegeCount;
                public UInt32 Control;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LUID_AND_ATTRIBUTES[] Privilege;
            }

            public struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public UInt32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TOKEN_PRIVILEGES
            {
                public UInt32 PrivilegeCount;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LUID_AND_ATTRIBUTES[] Privileges;
            }
            #endregion


            #region Methods
            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern void BuildExplicitAccessWithName(
                ref EXPLICIT_ACCESS pExplicitAccess,
                string pTrusteeName,
                uint AccessPermissions,
                uint AccessMode,
                uint Inheritance);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int SetEntriesInAcl(
                int cCountOfExplicitEntries,
                ref EXPLICIT_ACCESS pListOfExplicitEntries,
                IntPtr OldAcl,
                out IntPtr NewAcl);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool InitializeSecurityDescriptor(
                out SECURITY_DESCRIPTOR pSecurityDescriptor,
                uint dwRevision);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool SetSecurityDescriptorDacl(
                ref SECURITY_DESCRIPTOR pSecurityDescriptor,
                bool bDaclPresent,
                IntPtr pDacl,
                bool bDaclDefaulted);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern bool CreateProcess(
               string lpApplicationName,
               string lpCommandLine,
               ref SECURITY_ATTRIBUTES lpProcessAttributes,
               IntPtr lpThreadAttributes,
               bool bInheritHandles,
               UInt32 dwCreationFlags,
               IntPtr lpEnvironment,
               string lpCurrentDirectory,
               [In] ref STARTUPINFO lpStartupInfo,
               out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint ResumeThread(IntPtr hThread);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool OpenProcessToken(
                IntPtr ProcessHandle,
                UInt32 DesiredAccess,
                out IntPtr TokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool PrivilegeCheck(
                IntPtr ClientToken,
                ref PRIVILEGE_SET RequiredPrivileges,
                out bool pfResult);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool AdjustTokenPrivileges(
                IntPtr TokenHandle,
                bool DisableAllPrivileges,
                ref TOKEN_PRIVILEGES NewState,
                UInt32 BufferLengthInBytes,
                IntPtr PreviousState,
                UInt32 ReturnLengthInBytes);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern uint GetSecurityInfo(
                IntPtr handle,
                SE_OBJECT_TYPE ObjectType,
                SECURITY_INFORMATION SecurityInfo,
                IntPtr pSidOwner,
                IntPtr pSidGroup,
                out IntPtr pDacl,
                IntPtr pSacl,
                IntPtr pSecurityDescriptor);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern uint SetSecurityInfo(
                IntPtr handle,
                SE_OBJECT_TYPE ObjectType,
                SECURITY_INFORMATION SecurityInfo,
                IntPtr psidOwner,
                IntPtr psidGroup,
                IntPtr pDacl,
                IntPtr pSacl);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetCurrentProcess();
            #endregion
        }

        public static Process LaunchGame(string workingDir, string exePath, string arguments, IDictionary<string, string> envVars, DpiAwareness dpiAwareness, Action<Process> beforeResume)
        {
            Process process = null;

            var userName = Environment.UserName;

            var pExplicitAccess = new PInvoke.EXPLICIT_ACCESS();
            PInvoke.BuildExplicitAccessWithName(
                ref pExplicitAccess,
                userName,
                PInvoke.STANDARD_RIGHTS_ALL | PInvoke.SPECIFIC_RIGHTS_ALL & ~PInvoke.PROCESS_VM_WRITE,
                PInvoke.GRANT_ACCESS,
                0);

            if (PInvoke.SetEntriesInAcl(1, ref pExplicitAccess, IntPtr.Zero, out var newAcl) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var secDesc = new PInvoke.SECURITY_DESCRIPTOR();

            if (!PInvoke.InitializeSecurityDescriptor(out secDesc, PInvoke.SECURITY_DESCRIPTOR_REVISION))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!PInvoke.SetSecurityDescriptorDacl(ref secDesc, true, newAcl, false))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var psecDesc = Marshal.AllocHGlobal(Marshal.SizeOf<PInvoke.SECURITY_DESCRIPTOR>());
            Marshal.StructureToPtr<PInvoke.SECURITY_DESCRIPTOR>(secDesc, psecDesc, true);

            var lpProcessInformation = new PInvoke.PROCESS_INFORMATION();
            var lpEnvironment = IntPtr.Zero;

            try
            {
                if (envVars.Count > 0)
                {
                    string envstr = string.Join("\0", envVars.Select(entry => entry.Key + "=" + entry.Value));

                    lpEnvironment = Marshal.StringToHGlobalAnsi(envstr);
                }

                var lpProcessAttributes = new PInvoke.SECURITY_ATTRIBUTES
                {
                    nLength = Marshal.SizeOf<PInvoke.SECURITY_ATTRIBUTES>(),
                    lpSecurityDescriptor = psecDesc,
                    bInheritHandle = false
                };

                var lpStartupInfo = new PInvoke.STARTUPINFO
                {
                    cb = Marshal.SizeOf<PInvoke.STARTUPINFO>()
                };

                var compatLayerPrev = Environment.GetEnvironmentVariable("__COMPAT_LAYER");

                var compat = "RunAsInvoker ";
                compat += dpiAwareness switch
                {
                    DpiAwareness.Aware => "HighDPIAware",
                    DpiAwareness.Unaware => "DPIUnaware",
                    _ => throw new ArgumentOutOfRangeException()
                };
                Environment.SetEnvironmentVariable("__COMPAT_LAYER", compat);

                if (!PInvoke.CreateProcess(
                        null,
                        $"\"{exePath}\" {arguments}",
                        ref lpProcessAttributes,
                        IntPtr.Zero,
                        false,
                        PInvoke.CREATE_SUSPENDED,
                        IntPtr.Zero,
                        workingDir,
                        ref lpStartupInfo,
                        out lpProcessInformation))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Environment.SetEnvironmentVariable("__COMPAT_LAYER", compatLayerPrev);

                DisableSeDebug(lpProcessInformation.hProcess);

                process = new ExistingProcess(lpProcessInformation.hProcess);

                beforeResume?.Invoke(process);

                PInvoke.ResumeThread(lpProcessInformation.hThread);

                // Ensure that the game main window is prepared
                try
                {
                    do
                    {
                        process.WaitForInputIdle();

                        Thread.Sleep(100);
                    } while (IntPtr.Zero == TryFindGameWindow(process));
                }
                catch (InvalidOperationException)
                {
                    throw new GameExitedException();
                }

                if (PInvoke.GetSecurityInfo(
                        PInvoke.GetCurrentProcess(),
                        PInvoke.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        PInvoke.SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                        IntPtr.Zero, IntPtr.Zero,
                        out var pACL,
                        IntPtr.Zero, IntPtr.Zero) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (PInvoke.SetSecurityInfo(
                        lpProcessInformation.hProcess,
                        PInvoke.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        PInvoke.SECURITY_INFORMATION.DACL_SECURITY_INFORMATION |
                        PInvoke.SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION,
                        IntPtr.Zero, IntPtr.Zero, pACL, IntPtr.Zero) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "[NativeAclFix] Uncaught error during initialization, trying to kill process");

                try
                {
                    process?.Kill();
                }
                catch (Exception killEx)
                {
                    Console.WriteLine(killEx.Message, "[NativeAclFix] Could not kill process");
                }

                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(psecDesc);

                if (!IntPtr.Equals(lpEnvironment, IntPtr.Zero))
                {
                    Marshal.FreeHGlobal(lpEnvironment);
                }

                PInvoke.CloseHandle(lpProcessInformation.hThread);
            }

            return process;
        }

        private static void DisableSeDebug(IntPtr ProcessHandle)
        {
            if (!PInvoke.OpenProcessToken(ProcessHandle, PInvoke.TOKEN_QUERY | PInvoke.TOKEN_ADJUST_PRIVILEGES, out var TokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var luidDebugPrivilege = new PInvoke.LUID();
            if (!PInvoke.LookupPrivilegeValue(null, "SeDebugPrivilege", ref luidDebugPrivilege))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var RequiredPrivileges = new PInvoke.PRIVILEGE_SET
            {
                PrivilegeCount = 1,
                Control = PInvoke.PRIVILEGE_SET_ALL_NECESSARY,
                Privilege = new PInvoke.LUID_AND_ATTRIBUTES[1]
            };

            RequiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
            RequiredPrivileges.Privilege[0].Attributes = PInvoke.SE_PRIVILEGE_ENABLED;

            if (!PInvoke.PrivilegeCheck(TokenHandle, ref RequiredPrivileges, out bool bResult))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (bResult) // SeDebugPrivilege is enabled; try disabling it
            {
                var TokenPrivileges = new PInvoke.TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new PInvoke.LUID_AND_ATTRIBUTES[1]
                };

                TokenPrivileges.Privileges[0].Luid = luidDebugPrivilege;
                TokenPrivileges.Privileges[0].Attributes = PInvoke.SE_PRIVILEGE_REMOVED;

                if (!PInvoke.AdjustTokenPrivileges(TokenHandle, false, ref TokenPrivileges, 0, IntPtr.Zero, 0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            PInvoke.CloseHandle(TokenHandle);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, IntPtr windowTitle);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        private static IntPtr TryFindGameWindow(Process process)
        {
            IntPtr hwnd = IntPtr.Zero;
            while (IntPtr.Zero != (hwnd = FindWindowEx(IntPtr.Zero, hwnd, "FFXIVGAME", IntPtr.Zero)))
            {
                GetWindowThreadProcessId(hwnd, out uint pid);

                if (pid == process.Id && IsWindowVisible(hwnd))
                {
                    break;
                }
            }
            return hwnd;
        }
    }
    public class WindowsGameRunner : IGameRunner
    {
        private readonly DalamudLauncher dalamudLauncher;
        private readonly bool dalamudOk;
        private readonly DirectoryInfo dotnetRuntimePath;

        public WindowsGameRunner(DalamudLauncher dalamudLauncher, bool dalamudOk, DirectoryInfo dotnetRuntimePath)
        {
            this.dalamudLauncher = dalamudLauncher;
            this.dalamudOk = dalamudOk;
            this.dotnetRuntimePath = dotnetRuntimePath;
        }

        public Process Start(string path, string workingDirectory, string arguments, IDictionary<string, string> environment, DpiAwareness dpiAwareness)
        {
            if (dalamudOk)
            {
                var compat = "RunAsInvoker ";
                compat += dpiAwareness switch
                {
                    DpiAwareness.Aware => "HighDPIAware",
                    DpiAwareness.Unaware => "DPIUnaware",
                    _ => throw new ArgumentOutOfRangeException()
                };
                environment.Add("__COMPAT_LAYER", compat);

                var prevDalamudRuntime = Environment.GetEnvironmentVariable("DALAMUD_RUNTIME");
                if (string.IsNullOrWhiteSpace(prevDalamudRuntime))
                    environment.Add("DALAMUD_RUNTIME", dotnetRuntimePath.FullName);

                return this.dalamudLauncher.Run(new FileInfo(path), arguments, environment);
            }
            else
            {
                return NativeAclFix.LaunchGame(workingDirectory, path, arguments, environment, dpiAwareness, process => { });
            }
        }
    }
    public enum LoginAction
    {
        Game,
        GameNoDalamud,
        GameNoLaunch,
        Repair,
        Fake,
    }
    public enum ClientLanguage
    {
        Japanese,
        English,
        German,
        French
    }
    public class launchers
    {
        public Process? LaunchGame(IGameRunner runner, string sessionId, int region, int expansionLevel,
                               bool isSteamServiceAccount, string additionalArguments,
                               DirectoryInfo gamePath, bool isDx11, ClientLanguage language,
                               bool encryptArguments, DpiAwareness dpiAwareness)
        {
            Log.Information(
                $"XivGame::LaunchGame(steamServiceAccount:{isSteamServiceAccount}, args:{additionalArguments})");

            var exePath = Path.Combine(gamePath.FullName, "game", "ffxiv_dx11.exe");
            if (!isDx11)
                exePath = Path.Combine(gamePath.FullName, "game", "ffxiv.exe");

            var environment = new Dictionary<string, string>();

            var argumentBuilder = new ArgumentBuilder()
                                  .Append("DEV.DataPathType", "1")
                                  .Append("DEV.MaxEntitledExpansionID", expansionLevel.ToString())
                                  .Append("DEV.TestSID", sessionId)
                                  .Append("DEV.UseSqPack", "1")
                                  .Append("SYS.Region", region.ToString())
                                  .Append("language", ((int)language).ToString())
                                  .Append("resetConfig", "0")
                                  .Append("ver", Repository.Ffxiv.GetVer(gamePath));

            if (isSteamServiceAccount)
            {
                // These environment variable and arguments seems to be set when ffxivboot is started with "-issteam" (27.08.2019)
                environment.Add("IS_FFXIV_LAUNCH_FROM_STEAM", "1");
                argumentBuilder.Append("IsSteam", "1");
            }

            // This is a bit of a hack; ideally additionalArguments would be a dictionary or some KeyValue structure
            if (!string.IsNullOrEmpty(additionalArguments))
            {
                var regex = new Regex(@"\s*(?<key>[^\s=]+)\s*=\s*(?<value>([^=]*$|[^=]*\s(?=[^\s=]+)))\s*", RegexOptions.Compiled);
                foreach (Match match in regex.Matches(additionalArguments))
                    argumentBuilder.Append(match.Groups["key"].Value, match.Groups["value"].Value.Trim());
            }

            if (!File.Exists(exePath))
                throw new BinaryNotPresentException(exePath);

            var workingDir = Path.Combine(gamePath.FullName, "game");

            var arguments = encryptArguments
                ? argumentBuilder.BuildEncrypted()
                : argumentBuilder.Build();

            return runner.Start(exePath, workingDir, arguments, environment, dpiAwareness);
        }
    }
    public class networklogic
    {
        private static Storage storage;
        
        public static CommonUniqueIdCache UniqueIdCache;
        private static readonly string UserAgentTemplate = "SQEXAuthor/2.0.0(Windows 6.2; ja-jp; {0})";
        public List<AddonEntry>? Addons { get; set; }
        static string DalamudRolloutBucket { get; set; }
        private static readonly string UserAgent = GenerateUserAgent();
        public static DalamudUpdater DalamudUpdater { get; private set; }
        public static DalamudOverlayInfoProxy DalamudLoadInfo { get; private set; }
        

        public static async Task<Process> LaunchGameAsync(string gamePath, string realsid, int language, bool dx11, int expansionlevel, bool isSteam, int region)
        {
            storage = new Storage("protocolhandle");
            var dalamudOk = false;
            var gameArgs = string.Empty;
            IDalamudRunner dalamudRunner;
            launchers launcher = new launchers();
            IDalamudCompatibilityCheck dalamudCompatCheck;
            dalamudRunner = new WindowsDalamudRunner();
            dalamudCompatCheck = new WindowsDalamudCompatibilityCheck();
            string hardcodeddir = "D:\\HandleGame\\Dalamud";
            if (!Directory.Exists(hardcodeddir))
            {
                System.IO.Directory.CreateDirectory(hardcodeddir);
            }
            DirectoryInfo dalamudpath = new DirectoryInfo(hardcodeddir);
            Troubleshooting.LogTroubleshooting(gamePath);
            DirectoryInfo gamePather = new DirectoryInfo(gamePath);
            DalamudLoadInfo = new DalamudOverlayInfoProxy();
            try
            {
                DalamudUpdater = new DalamudUpdater(storage.GetFolder("dalamud"), storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), storage.Root, null, null)
                {
                    Overlay = DalamudLoadInfo
                };
                DalamudUpdater.Run();
            }
            
            
            catch (Exception ex)
            {
                Log.Error(ex, "Could not start dalamud updater");
            }
            var dalamudLauncher = new DalamudLauncher(dalamudRunner, DalamudUpdater, DalamudLoadMethod.DllInject,
                gamePather, dalamudpath, (LibDalamud.ClientLanguage)ClientLanguage.English, 0, false, false, false,
                Troubleshooting.GetTroubleshootingJson(gamePath));
            
            try
            {
                dalamudCompatCheck.EnsureCompatibility();
            }
            catch (IDalamudCompatibilityCheck.NoRedistsException ex)
            {
                Log.Error(ex, "No Dalamud Redists found");

                throw;
                /*
                CustomMessageBox.Show(
                    Loc.Localize("DalamudVc2019RedistError",
                        "The XIVLauncher in-game addon needs the Microsoft Visual C++ 2015-2019 redistributable to be installed to continue. Please install it from the Microsoft homepage."),
                    "XIVLauncher", MessageBoxButton.OK, MessageBoxImage.Exclamation, parentWindow: _window);
                    */
            }
            catch (IDalamudCompatibilityCheck.ArchitectureNotSupportedException ex)
            {
                Log.Error(ex, "Architecture not supported");

                throw;
                /*
                CustomMessageBox.Show(
                    Loc.Localize("DalamudArchError",
                        "Dalamud cannot run your computer's architecture. Please make sure that you are running a 64-bit version of Windows.\nIf you are using Windows on ARM, please make sure that x64-Emulation is enabled for XIVLauncher."),
                    "XIVLauncher", MessageBoxButton.OK, MessageBoxImage.Exclamation, parentWindow: _window);
                    */
            }
            try
            {
                try
                {
                    dalamudOk = dalamudLauncher.HoldForUpdate(gamePather) == DalamudLauncher.DalamudInstallState.Ok;
                }
                catch (DalamudRunnerException ex)
                {
                    Log.Error(ex, "Couldn't ensure Dalamud runner");

                    

                    throw;
                    /*
                    CustomMessageBox.Builder
                                    .NewFrom(runnerErrorMessage)
                                    .WithImage(MessageBoxImage.Error)
                                    .WithButtons(MessageBoxButton.OK)
                                    .WithShowHelpLinks()
                                    .WithParentWindow(_window)
                                    .Show();
                                    */
                }
                IGameRunner runner;
                runner = new WindowsGameRunner(dalamudLauncher, dalamudOk, DalamudUpdater.Runtime);
                Process ffxivgame = launcher.LaunchGame(runner, realsid,
                region, expansionlevel, isSteam,gameArgs, gamePather, dx11, ClientLanguage.English,true,
            DpiAwareness.Unaware);

                var addonMgr = new AddonManager();
                try
                {
                    List<AddonEntry> xex = new List<AddonEntry>();

                    var addons = xex.Where(x => x.IsEnabled).Select(x => x.Addon).Cast<IAddon>().ToList();

                    addonMgr.RunAddons(ffxivgame.Id, addons);
                }
                catch (Exception ex)
                {
                    /*
                    CustomMessageBox.Builder
                                    .NewFrom(ex, "Addons")
                                    .WithAppendText("\n\n")
                                    .WithAppendText(Loc.Localize("AddonLoadError",
                                        "This could be caused by your antivirus, please check its logs and add any needed exclusions."))
                                    .WithParentWindow(_window)
                                    .Show();
                                    */

                    

                    addonMgr.StopAddons();
                    throw;
                }

                Log.Debug("Waiting for game to exit");
                
                await Task.Run(() => ffxivgame!.WaitForExit()).ConfigureAwait(false);


                Log.Verbose("Game has exited");

                if (addonMgr.IsRunning)
                    addonMgr.StopAddons();
                return ffxivgame;
            }
            catch (Exception exc)
            {
                if (language == 0)
                {
                    Debug.WriteLine("実行可能ファイルを起動できませんでした。 ゲームパスは正しいですか? " + exc);
                }
                if (language == 1)
                {
                    Debug.WriteLine("Could not launch executable. Is your game path correct? " + exc);
                }
                if (language == 2)
                {
                    Debug.WriteLine("Die ausführbare Datei konnte nicht gestartet werden. Ist dein Spielpfad korrekt? " + exc);
                }
                if (language == 3)
                {
                    Debug.WriteLine("Impossible de lancer l'exécutable. Votre chemin de jeu est-il correct? " + exc);
                }
                if (language == 4)
                {
                    Debug.WriteLine("Не удалось запустить файл. Ввели ли вы корректный путь к игре? " + exc);
                }

            }

            return null;
        }

        public static string GetRealSid(string gamePath, string username, string password, string otp, bool isSteam)
        {
            string hashstr = "";
            try
            {
                // make the string of hashed files to prove game version//make the string of hashed files to prove game version
                hashstr = "ffxivboot.exe/" + GenerateHash(gamePath + "/boot/ffxivboot.exe") +
                          ",ffxivboot64.exe/" + GenerateHash(gamePath + "/boot/ffxivboot64.exe") +
                          ",ffxivlauncher.exe/" + GenerateHash(gamePath + "/boot/ffxivlauncher.exe") +
                          ",ffxivlauncher64.exe/" + GenerateHash(gamePath + "/boot/ffxivlauncher64.exe") +
                          ",ffxivupdater.exe/" + GenerateHash(gamePath + "/boot/ffxivupdater.exe") +
                          ",ffxivupdater64.exe/" + GenerateHash(gamePath + "/boot/ffxivupdater64.exe");
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Could not generate hashes. Is your game path correct? " + exc);
            }

            WebClient sidClient = new WebClient();
            sidClient.Headers.Add("X-Hash-Check", "enabled");
            sidClient.Headers.Add("user-agent", UserAgent);
            sidClient.Headers.Add("Referer", "https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3");
            sidClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            InitiateSslTrust();

            try
            {
                var localGameVer = GetLocalGamever(gamePath);
                var localSid = GetSid(username, password, otp, isSteam);

                if (localGameVer.Equals("BAD") || localSid.Equals("BAD"))
                {
                    return "BAD";
                }

                var url = "https://patch-gamever.ffxiv.com/http/win32/ffxivneo_release_game/" + localGameVer + "/" + localSid;
                sidClient.UploadString(url, hashstr); //request real session id
            }
            catch (Exception exc)
            {
                Debug.WriteLine($"Unable to retrieve a session ID from the server.\n" + exc);
            }

            return sidClient.ResponseHeaders["X-Patch-Unique-Id"];
        }

        private static string GetStored(bool isSteam) //this is needed to be able to access the login site correctly
        {
            WebClient loginInfo = new WebClient();
            loginInfo.Headers.Add("user-agent", UserAgent);
            string reply = loginInfo.DownloadString(string.Format("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3&isft=0&issteam={0}", isSteam ? 1 : 0));

            Regex storedre = new Regex(@"\t<\s*input .* name=""_STORED_"" value=""(?<stored>.*)"">");

            var stored = storedre.Matches(reply)[0].Groups["stored"].Value;
            return stored;
        }

        public static string GetSid(string username, string password, string otp, bool isSteam)
        {
            using (WebClient loginData = new WebClient())
            {
                loginData.Headers.Add("user-agent", UserAgent);
                loginData.Headers.Add("Referer", string.Format("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/top?lng=en&rgn=3&isft=0&issteam={0}", isSteam ? 1 : 0));
                loginData.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                try
                {
                    byte[] response =
                        loginData.UploadValues("https://ffxiv-login.square-enix.com/oauth/ffxivarr/login/login.send", new NameValueCollection() //get the session id with user credentials
                        {
                            { "_STORED_", GetStored(isSteam) },
                            { "sqexid", username },
                            { "password", password },
                            { "otppw", otp }
                        });

                    string reply = System.Text.Encoding.UTF8.GetString(response);
                    //Debug.WriteLine(reply);
                    Regex sidre = new Regex(@"sid,(?<sid>.*),terms");
                    var matches = sidre.Matches(reply);
                    if (matches.Count == 0)
                    {
                        if (reply.Contains("ID or password is incorrect"))
                        {
                            Debug.WriteLine("Incorrect username or password.");
                            return "BAD";
                        }
                    }

                    var sid = sidre.Matches(reply)[0].Groups["sid"].Value;
                    return sid;
                }
                catch (Exception exc)
                {
                    Debug.WriteLine($"Something failed when attempting to request a session ID.\n" + exc);
                    return "BAD";
                }
            }
        }

        private static string GetLocalGamever(string gamePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(gamePath + @"/game/ffxivgame.ver"))
                {
                    string line = sr.ReadToEnd();
                    return line;
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Unable to get local game version.\n" + exc);
                return "BAD";
            }
        }

        private static string GenerateHash(string file)
        {
            byte[] filebytes = File.ReadAllBytes(file);

            var hash = (new SHA1Managed()).ComputeHash(filebytes);
            string hashstring = string.Join("", hash.Select(b => b.ToString("x2")).ToArray());

            long length = new FileInfo(file).Length;

            return length + "/" + hashstring;
        }

        public static bool GetGateStatus()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string reply = client.DownloadString("http://frontier.ffxiv.com/worldStatus/gate_status.json");

                    return Convert.ToBoolean(int.Parse(reply[10].ToString()));
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine("Failed getting gate status. " + exc);
                return false;
            }

        }

        private static void InitiateSslTrust()
        {
            //Change SSL checks so that all checks pass, squares gamever server does strange things
            ServicePointManager.ServerCertificateValidationCallback =
                new RemoteCertificateValidationCallback(
                    delegate
                    { return true; }
                );
        }


        private static string GenerateUserAgent()
        {
            return string.Format(UserAgentTemplate, MakeComputerId());
        }

        private static string MakeComputerId()
        {
            var hashString = Environment.MachineName + Environment.UserName + Environment.OSVersion +
                             Environment.ProcessorCount;

            using (var sha1 = HashAlgorithm.Create("SHA1"))
            {
                var bytes = new byte[5];

                Array.Copy(sha1.ComputeHash(Encoding.Unicode.GetBytes(hashString)), 0, bytes, 1, 4);

                var checkSum = (byte)-(bytes[1] + bytes[2] + bytes[3] + bytes[4]);
                bytes[0] = checkSum;

                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}