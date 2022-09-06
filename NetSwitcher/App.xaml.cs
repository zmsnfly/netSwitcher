using CommunityToolkit.Mvvm.Input;
using Hardcodet.Wpf.TaskbarNotification;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using NETCONLib;
using System.Drawing;
using System.Diagnostics;
using System;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
using System.Net.NetworkInformation;

namespace NetSwitcher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            _taskbar = (TaskbarIcon)FindResource("Taskbar");
            base.OnStartup(e);
            SetMeStart(true);

        }

        private TaskbarIcon _taskbar;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
        }

        public static bool SetMeStart(bool onOff)
        {
            
            bool isOk = false;
            string appName = Process.GetCurrentProcess().MainModule.ModuleName;
            string appPath = Process.GetCurrentProcess().MainModule.FileName;
            isOk = SetAutoStart(onOff, appName, appPath);
            return isOk;
        }

        public static bool SetAutoStart(bool onOff, string appName, string appPath)
        {
            bool isOk = true;
            if (!IsExistKey(appName) && onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            else if (IsExistKey(appName) && !onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            return isOk;
        }

        private static bool IsExistKey(string keyName)
        {
            try
            {
                bool _exist = false;
                RegistryKey local = Registry.LocalMachine;
                RegistryKey runs = local.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (runs == null)
                {
                    RegistryKey key2 = local.CreateSubKey("SOFTWARE");
                    RegistryKey key3 = key2.CreateSubKey("Microsoft");
                    RegistryKey key4 = key3.CreateSubKey("Windows");
                    RegistryKey key5 = key4.CreateSubKey("CurrentVersion");
                    RegistryKey key6 = key5.CreateSubKey("Run");
                    runs = key6;
                }
                string[] runsName = runs.GetValueNames();
                foreach (string strName in runsName)
                {
                    if (strName.ToUpper() == keyName.ToUpper())
                    {
                        
                        _exist = true;
                        return _exist;
                    }
                }
                return _exist;

            }
            catch
            {
                return false;
            }
        }

        private static bool SelfRunning(bool isStart, string exeName, string path)
        {
            try
            {
                RegistryKey local = Registry.LocalMachine;
                RegistryKey key = local.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null)
                {
                    local.CreateSubKey("SOFTWARE//Microsoft//Windows//CurrentVersion//Run");
                }
                if (isStart)
                {
                    key.SetValue(exeName, path);
                    key.Close();
                }
                else
                {
                    string[] keyNames = key.GetValueNames();
                    foreach (string keyName in keyNames)
                    {
                        if (keyName.ToUpper() == exeName.ToUpper())
                        {
                            key.DeleteValue(exeName);
                            key.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string ss = ex.Message;
                return false;
            }

            return true;
        }
    }


    public class NotifyIconViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!(object.Equals(field, newValue)))
            {
                field = (newValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
        private bool CheckIsOnline()
        {
            Ping ping = new Ping();
            PingReply pingReply = ping.Send("192.168.31.76",500);
            if (pingReply.Status == IPStatus.Success)
            {
                return true;
            }
            return false;
        }
        private void SetState(bool state)
        {
            if (state)
            {
                Status = "已连接到DiskStation422";
            }
            else
            {
                Status = "DiskStation422已断开连接";
            }
        }
        private void ChangeNetState(bool state)
        {
            var connections = NetSharingMgr.EnumEveryConnection;
            foreach (INetConnection connection in connections)
            {
                var current_prop = NetSharingMgr.get_NetConnectionProps(connection);
                if (current_prop.Name == "以太网")
                {
                    if (current_prop.Status.ToString().Contains("DIS") && state==true)
                    {
                        try
                        {
                            connection.Connect();
                        }
                        catch { }
                    }
                    else if(!current_prop.Status.ToString().Contains("DIS") && state == false)
                    {
                        try
                        {
                            connection.Disconnect();
                        }
                        catch { }
                    }
                    break;
                }
            }
        }
        private void OpenFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;

            Process process = new Process();
            ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe");
            psi.Arguments = folderPath;
            process.StartInfo = psi;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                process?.Close();

            }

        }
        public NotifyIconViewModel()
        {
            Status = "处理中";
            SetState(CheckIsOnline());
        }
        private bool isConnectOK;

        public bool IsConnectOK { get => isConnectOK; set => SetProperty(ref isConnectOK, value); }

        private string status;

        public string Status
        {
            get => status;
            set
            {
                SetProperty(ref status, value);
                if(status == "处理中")
                {
                    Icon = "/icon/busy.ico";
                    IsConnectOK = false;
                }
                else if(status.Contains("已连接"))
                {
                    Icon = "/icon/connected.ico";
                    IsConnectOK = true;
                }
                else
                {
                    Icon = "/icon/unconnect.ico";
                    IsConnectOK = false;
                }
            }
        }

        private string icon;

        public string Icon { get => icon; set => SetProperty(ref icon, value); }

        readonly NetSharingManagerClass NetSharingMgr = new NetSharingManagerClass();

        private RelayCommand switchNetwork;

        public ICommand SwitchNetwork
        {
            get
            {
                if (switchNetwork == null)
                {
                    switchNetwork = new RelayCommand(PerformSwitchNetwork);
                }

                return switchNetwork;
            }
        }

        private void PerformSwitchNetwork()
        {
            if (Status == "处理中") return;
            Status = "处理中";
            bool is_current_connect = CheckIsOnline();
            ChangeNetState(!CheckIsOnline());

            Ping ping = new Ping();
            var iWait = 20;
            while (true && iWait>0)
            {
                PingReply pingReply = ping.Send("192.168.31.76", 500);
                if (pingReply.Status == IPStatus.Success && !is_current_connect)
                {
                    SetState(true);
                    OpenFolder(@"\\lab422");
                    return;
                }
                else if(pingReply.Status != IPStatus.Success && is_current_connect)
                {
                    SetState(false);
                    return;
                }
                iWait--;
            }

        }


        private RelayCommand exitApplication;
        public ICommand ExitApplication
        {
            get
            {
                if (exitApplication == null)
                {
                    exitApplication = new RelayCommand(PerformExit);
                }

                return exitApplication;
            }
        }
        private void PerformExit()
        {
            Application.Current.Shutdown();
        }


        private RelayCommand openHome;
        public ICommand OpenHome
        {
            get
            {
                if (openHome == null)
                {
                    openHome = new RelayCommand(PerformOpenHome);
                }

                return openHome;
            }
        }
        private void PerformOpenHome()
        {
            OpenFolder(@"\\lab422\home");
        }

        private RelayCommand openHomes;
        public ICommand OpenHomes
        {
            get
            {
                if (openHomes == null)
                {
                    openHomes = new RelayCommand(PerformOpenHomes);
                }

                return openHomes;
            }
        }
        private void PerformOpenHomes()
        {
            OpenFolder(@"\\lab422\homes");
        }

        private RelayCommand openSharedFiles;
        public ICommand OpenSharedFiles
        {
            get
            {
                if (openSharedFiles == null)
                {
                    openSharedFiles = new RelayCommand(PerformOpenSharedFiles);
                }

                return openSharedFiles;
            }
        }
        private void PerformOpenSharedFiles()
        {
            OpenFolder(@"\\lab422\sharedfiles");
        }




    }
}
