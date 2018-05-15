using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows.Threading;

namespace CmdHost
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        private readonly string _nodeAppPath = "index.js";
        private Process _nodeHostProcess;

        public MainWindow()
        {
            InitializeComponent();
            InitNodeService();
            InitialTray();
        }
        /// <summary>
        /// 初始化cmd命令执行服务
        /// </summary>
        public void InitNodeService()
        {
            SetMsgNormal( "启动服务中...\r\n");
            Thread mythread = new Thread(StartNodeService);
            mythread.Start();
          
            RegisterMainExitEvent();
        }

        /// <summary>
        /// 开启node席位服务
        /// </summary>
        public void StartNodeService()
        {
            try
            {
                //var nodePros = Process.GetProcessesByName("node");
                //foreach (var nodep in nodePros)
                //{
                //    nodep.Kill();
                //}

                //SlimClientLogger.Instance.ClientOperationLogger("client cmd NodeNoticeClientHost to start ");
                _nodeHostProcess = new Process();
                _nodeHostProcess.StartInfo.FileName = "cmd.exe";
                _nodeHostProcess.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
                _nodeHostProcess.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
                _nodeHostProcess.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                _nodeHostProcess.StartInfo.RedirectStandardError = true;//重定向标准错误输出
                _nodeHostProcess.StartInfo.CreateNoWindow = true;//不显示程序窗口

                _nodeHostProcess.Start();//启动程序

                //向cmd窗口发送输入信息
                _nodeHostProcess.StandardInput.WriteLine("node " + _nodeAppPath);
                //SlimClientLogger.Instance.ClientOperationLogger(string.Format("path {0}, client node {1}", Environment.CurrentDirectory, _nodeAppPath));

                SetMsg("服务已启动...");

                //获取cmd窗口的输出信息
                string output = _nodeHostProcess.StandardOutput.ReadToEnd();
            
                _nodeHostProcess.WaitForExit();//等待程序执行完退出进程
                _nodeHostProcess.Close();

              
            }
            catch (System.Exception ex)
            {
                SetMsg(ex.Message);
                // SlimClientLogger.Instance.ClientExceptionStandard(ex);
            }

            //Thread.Sleep(2000);
        }


        /// <summary>
        /// 重启node席位服务
        /// </summary>
        public void RestartNodeHost()
        {
            CloseNodeHost();
            StartNodeService();
        }

        /// <summary>
        /// 关闭node席位服务
        /// </summary>
        public void CloseNodeHost()
        {
            if (_nodeHostProcess != null)
            {
                KillProcessAndChildren(_nodeHostProcess.Id);
                //SlimClientLogger.Instance.ClientOperationLogger("application exit,nodehost close log");
            }
            notifyIconDispose();
        }

        /// <summary>
        /// 注册卸载服务事件
        /// </summary>
        public void RegisterMainExitEvent()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Application.Current.Exit += (obj, e) =>
                {
                    CloseNodeHost();
                };
            }));

            Process current = Process.GetCurrentProcess();

            Thread mythread = new Thread(() =>
            {
                if (current != null)
                {
                    current.WaitForExit();
                    CloseNodeHost();
                }
            });
            mythread.Start();

        }

        /// <summary>
        /// 杀掉进程及子进程
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (System.Exception ex)
            {
                // SlimClientLogger.Instance.ClientExceptionStandard(ex);
            }

        }

        /// <summary>
        /// 设置UI显示消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        private void SetMsg(string msg)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
               (ThreadStart)delegate ()
                   {
                       tbText.Text += msg + "\r\n";
                   }
               );
        }
        private void SetMsgNormal(string msg)
        {
            tbText.Text += msg + "\r\n";
            
        }


        #region 右下角图标  
        private void InitialTray()
        {

            //设置托盘的各个属性
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.BalloonTipText = "网络运营系统服务开始运行";
            notifyIcon.Text = "网络运营系统";
        
            notifyIcon.Icon = new System.Drawing.Icon(System.Windows.Forms.Application.StartupPath + "\\bitbug_favicon.ico");
            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(2000);
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseClick);

           
        
            //退出菜单项
            System.Windows.Forms.MenuItem exit = new System.Windows.Forms.MenuItem("退出");
            exit.Click += new EventHandler(exit_Click);

            //关联托盘控件
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { exit };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);

            //窗体状态改变时候触发
            this.StateChanged += new EventHandler(SysTray_StateChanged);
        }
        private void SysTray_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Visibility = Visibility.Hidden;
            }
        }
        private void exit_Click(object sender, EventArgs e)
        {
            if (System.Windows.MessageBox.Show("确定要关闭吗?",
                                               "退出",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Question,
                                                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                notifyIconDispose();
                System.Windows.Application.Current.Shutdown();
            }
        }

        private void notifyIconDispose()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
            }
        }
        private void notifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (this.Visibility == Visibility.Visible)
                {
                    this.Visibility = Visibility.Hidden;
                }
                else
                {
                    this.Visibility = Visibility.Visible;
                    this.Activate();
                }
            }
        }
        #endregion
    }
}
