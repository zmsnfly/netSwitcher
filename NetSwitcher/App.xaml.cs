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
            Trace.WriteLine(IsExistKey(appName));
            Trace.WriteLine(appName);
            isOk = SetAutoStart(onOff, appName, appPath);
            return isOk;
        }

        public static bool SetAutoStart(bool onOff, string appName, string appPath)
        {
            bool isOk = true;
            //如果从没有设为开机启动设置到要设为开机启动
            if (!IsExistKey(appName) && onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            //如果从设为开机启动设置到不要设为开机启动
            else if (IsExistKey(appName) && !onOff)
            {
                isOk = SelfRunning(onOff, appName, @appPath);
            }
            return isOk;
        }

        /// <summary>
        /// 判断注册键值对是否存在，即是否处于开机启动状态
        /// </summary>
        /// <param name="keyName">键值名</param>
        /// <returns></returns>
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

        /// <summary>
        /// 写入或删除注册表键值对,即设为开机启动或开机不启动
        /// </summary>
        /// <param name="isStart">是否开机启动</param>
        /// <param name="exeName">应用程序名</param>
        /// <param name="path">应用程序路径带程序名</param>
        /// <returns></returns>
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
                //若开机自启动则添加键值对
                if (isStart)
                {
                    key.SetValue(exeName, path);
                    key.Close();
                }
                else//否则删除键值对
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
                //throw;
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

        public NotifyIconViewModel()
        {
            Status = "处理中";
            var connections = NetSharingMgr.EnumEveryConnection;
            foreach (INetConnection connection in connections)
            {
                INetConnectionProps connProps = NetSharingMgr.get_NetConnectionProps(connection);
                
                if (connProps.Name == "以太网")
                {
                    if (connProps.Status.ToString().Contains("DIS"))
                    {
                        Status = "未连接";
                        break;
                    }
                    else
                    {
                        Status = "已连接";
                        break;
                    }
                }
            }
        }

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
                }
                else if(status == "已连接")
                {
                    Icon = "/icon/connected.ico";
                }
                else
                {
                    Icon = "/icon/unconnect.ico";
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
            Status = "处理中";
            var connections = NetSharingMgr.EnumEveryConnection;
            foreach (INetConnection connection in connections)
            {
                INetConnectionProps connProps = NetSharingMgr.get_NetConnectionProps(connection);
                if (connProps.Name == "以太网")
                {
                    if(connProps.Status.ToString().Contains("DIS"))
                    {
                        try
                        {
                            connection.Connect();
                            Status = "已连接";
                        }
                        catch { }

                        break;
                    }
                    else
                    {
                        try
                        {
                            connection.Disconnect();
                            Status = "未连接";
                        }
                        catch { }
                        
                        
                        break;
                    }
                }
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
    }
}
