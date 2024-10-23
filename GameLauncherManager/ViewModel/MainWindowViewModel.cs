using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using SimpleMvvm;
using SimpleMvvm.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GameLauncherManager.Model;
using GameLauncherManager.Utils;
using GameLauncherManager.View;
using GameLauncherManager.View.Dialogs;
using IW = IWshRuntimeLibrary;

namespace GameLauncherManager.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        public DelegateCommand? NewGameLauncherCommand { get; private set; }
        public DelegateCommand? RemoveGameLauncherCommand { get; private set; }
        public DelegateCommand? EditGameLauncherCommand { get; private set; }
        public DelegateCommand? RenameGameLauncherCommand { get; private set; }
        public DelegateCommand? ShowInExplorerCommand { get; private set; }
        public DelegateCommand? AddGamePluginCommand { get; private set; }
        public DelegateCommand? OpenGameLauncherCommand { get; private set; }
        public DelegateCommand? EditGameLauncherPathCommand { get; private set; }
        public DelegateCommand? ShowAboutCommand { get; private set; }
        public DelegateCommand? CloseWindowCommand { get; private set; }
        public DelegateCommand? MinWindowCommand { get; private set; }
        public DelegateCommand? CreateDesktopShortcutsCommand { get; private set; }
        public DelegateCommand? OpenSettingsCommand { get; private set; }
        public DelegateCommand? HelpCommand { get; private set; }
        public DelegateCommand? ViewGithubCommand { get; private set; }

        private void NewGameLauncher(object obj)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "应用程序|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    StaticData.AddGameLauncher(ofd.FileName);
                }
            }
            catch (Exception e)
            {
                MsgBoxHelper.ShowError(e.Message);
            }
        }

        private async void RemoveGameLauncher(GameLauncher gameLauncher)
        {
            //创建对话框用户控件
            var dialog = new RemoveLauncherDialog();
            //打开对话框，接收关闭返回值
            var result = await DialogHost.Show(dialog);
            //判断是否确定
            if (Convert.ToBoolean(result))
            {
                StaticData.GameLaunchers?.Remove(gameLauncher);
            }
        }

        private async void EditGameLauncher(GameLauncher gameLauncher)
        {
            var dialog = new EditLauncherDialog()
            {
                DataContext = gameLauncher
            };
            var result = await DialogHost.Show(dialog);
            if (Convert.ToBoolean(result))
            {
                if (string.IsNullOrWhiteSpace(dialog.LauncherName.Text))
                {
                    //MsgBoxHelper.ShowError("名称不能为空。");
                    var dialog2 = new MessageDialog("名称不能为空");
                    await DialogHost.Show(dialog2);
                    return;
                }

                if (!File.Exists(dialog.LauncherPath.Text) ||
                    PathHelper.GetSuffix(dialog.LauncherPath.Text).ToUpper() != "EXE")
                {
                    //MsgBoxHelper.ShowError("文件不存在或不支持。");
                    var dialog3 = new MessageDialog("文件不存在或不支持");
                    await DialogHost.Show(dialog3);
                    return;
                }

                gameLauncher.Name = dialog.LauncherName.Text;
                gameLauncher.GamePlugins[0].Path = dialog.LauncherPath.Text;
            }
        }

        private async void RenameGameLauncher(GameLauncher gameLauncher)
        {
            var dialog = new RenameLauncherDialog()
            {
                DataContext = gameLauncher
            };
            var result = await DialogHost.Show(dialog);
            if (Convert.ToBoolean(result))
            {
                gameLauncher.Name = dialog.LauncherName.Text;
            }
        }

        private void ShowInExplorer(GameLauncher gameLauncher)
        {
            Process.Start(new ProcessStartInfo("Explorer.exe")
            {
                Arguments = "/e,/select," + gameLauncher.GamePlugins[0].Path
            });
        }

        private void AddGamePlugin(GameLauncher gameLauncher)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "应用程序|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    gameLauncher.AddPlugin(ofd.FileName);
                }
            }
            catch (Exception e)
            {
                MsgBoxHelper.ShowError(e.Message);
            }
        }

        private void OpenGameLauncher(object LauncherId)
        {
            var pluginSelector = new PluginSelector()
            {
                LauncherId = (int)LauncherId
            };

            var _mainWindow =
                Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
            if (_mainWindow != null) _mainWindow.WindowState = WindowState.Minimized;

            pluginSelector.ShowDialog();
        }

        private void EditGameLauncherPath(object obj)
        {
            try
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "应用程序|*.exe"
                };
                if (ofd.ShowDialog() == true)
                {
                    ((TextBox)obj).Text = ofd.FileName;
                }
            }
            catch (Exception ex)
            {
                MsgBoxHelper.ShowError(ex.Message);
            }
        }


        private async void ShowAbout(object obj)
        {
            var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName();
            if (assemblyName == null) return;
            var dialog = new MessageDialog($"{assemblyName.Name} v{assemblyName.Version} \nBy FurryMonster", "关于");
            await DialogHost.Show(dialog);
        }

        private void CloseWindow(object obj)
        {
            ((Window)obj).Close();
        }

        private void MinWindow(object obj)
        {
            ((Window)obj).WindowState = WindowState.Minimized;
        }

        public async void CreateDesktopShortcuts(GameLauncher gameLauncher)
        {
            string LnkName = $"{gameLauncher.Name}启动器";
            string GamePath = gameLauncher.GamePlugins[0].Path;
            string Arguments = gameLauncher.Id.ToString();
            string exePath = AppDomain.CurrentDomain.BaseDirectory + "LauncherAssist" + ".exe";

            if (!File.Exists(exePath))
            {
                var dialog = new MessageDialog("LauncherAssist.exe不存在，无法创建桌面快捷方式");
                await DialogHost.Show(dialog);
                return;
            }

            string shortcutPath =
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    LnkName + ".lnk");
            // 确定是否已创建快捷方式
            if (System.IO.File.Exists(shortcutPath))
            {
                var dialog = new MessageDialog("快捷方式已存在，请勿重复创建");
                await DialogHost.Show(dialog);
                return;
            }
            //string AppName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
            // 获取当前应用程序目录地址

            IW.IWshShell shell = new IW.WshShell();

            //foreach (var item in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "*.lnk"))
            //{
            //    IW.WshShortcut tempShortcut = (IW.WshShortcut)shell.CreateShortcut(item);
            //    if (tempShortcut.TargetPath == exePath&&tempShortcut.Arguments == Arguments&& tempShortcut.Description == GamePath)
            //    {
            //        var dialog = new MessageDialog("快捷方式已存在，请勿重复创建");
            //        await DialogHost.Show(dialog);
            //        return;
            //    }
            //}
            IW.WshShortcut shortcut = (IW.WshShortcut)shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath; //应用程序路径
            shortcut.Arguments = Arguments; // 参数  
            shortcut.Description = $"{GamePath}"; //描述
            shortcut.WorkingDirectory = Environment.CurrentDirectory; //程序所在文件夹，在快捷方式图标点击右键可以看到此属性  
            shortcut.IconLocation = GamePath; //图标，该图标是应用程序的资源文件  
            shortcut.WindowStyle = 1;
            shortcut.Save();
            var dialog2 = new MessageDialog("桌面快捷方式已创建");
            await DialogHost.Show(dialog2);
        }

        private async void OpenSettings(object obj)
        {
            var dialog = new SettingsDialog();
            await DialogHost.Show(dialog);
        }

        private async void Help(object obj)
        {
            var dialog = new MessageDialog($"因为作者正在忙着玩中国大作《原神》,所以懒得写帮助文档\n如果Github有足够多的Star，会考虑写一个",$"帮助文档");
            await DialogHost.Show(dialog);
        }

        private void ViewGithub(object obj)
        {
            ProcessStartInfo info = new ProcessStartInfo("https://github.com/Furry-Monster")
            {
                UseShellExecute = true
            };
            Process.Start(info)?.Dispose();
        }

        protected override void Init()
        {
            base.Init();

            NewGameLauncherCommand = new DelegateCommand(NewGameLauncher);
            RemoveGameLauncherCommand = new DelegateCommand<GameLauncher>(RemoveGameLauncher);
            EditGameLauncherCommand = new DelegateCommand<GameLauncher>(EditGameLauncher);
            RenameGameLauncherCommand = new DelegateCommand<GameLauncher>(RenameGameLauncher);
            ShowInExplorerCommand = new DelegateCommand<GameLauncher>(ShowInExplorer);
            AddGamePluginCommand = new DelegateCommand<GameLauncher>(AddGamePlugin);
            OpenGameLauncherCommand = new DelegateCommand(OpenGameLauncher);
            EditGameLauncherPathCommand = new DelegateCommand(EditGameLauncherPath);
            ShowAboutCommand = new DelegateCommand(ShowAbout);
            CloseWindowCommand = new DelegateCommand(CloseWindow);
            MinWindowCommand = new DelegateCommand(MinWindow);
            CreateDesktopShortcutsCommand = new DelegateCommand<GameLauncher>(CreateDesktopShortcuts);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);
            HelpCommand = new DelegateCommand(Help);
            ViewGithubCommand = new DelegateCommand(ViewGithub);
        }
    }
}