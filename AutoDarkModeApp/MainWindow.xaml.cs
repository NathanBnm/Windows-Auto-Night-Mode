﻿using System;
using System.Windows;
using System.Windows.Shell;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Navigation;
using AutoDarkModeApp.Properties;
using NetMQ;
using AutoDarkModeSvc.Communication;
using System.Windows.Media;

namespace AutoDarkModeApp
{
    public partial class MainWindow
    {
        private ScaleTransform Transform { get; set;  }
        private int BasewindowX { get; } = 640;
        private int BaseWindowY { get; } = 560;
        public string WindowX { get; }
        public string WindowY { get; }
        public string ScaleStringX { get; }
        public string ScaleStringY { get; }


        public MainWindow()
        {
            DataContext = this;

            WindowX = ((int)(Settings.Default.UIScale * BasewindowX)).ToString();
            WindowY = ((int)((Settings.Default.UIScale) * BaseWindowY)).ToString();
            ScaleStringX = Settings.Default.UIScale.ToString("N2", CultureInfo.CreateSpecificCulture("en-US"));
            ScaleStringY = Settings.Default.UIScale.ToString("N2", CultureInfo.CreateSpecificCulture("en-US"));

            Console.WriteLine("--------- AppStart");

            LanguageHelper(); //set current UI language

            InitializeComponent();

            //only run at first startup
            if (Settings.Default.FirstRun)
            {
                SystemTimeFormat(); //check if system uses 12 hour clock
                AddJumpList(); //create jump list entries

                //set taskfolder name with username for multiple user environments
                try
                {
                    Settings.Default.TaskFolderTitle = "ADM_" + Environment.UserName;
                    Settings.Default.TaskFolderTitleMultiUser = true;
                }
                catch
                {
                    Settings.Default.TaskFolderTitle = "Auto Dark Mode";
                    Settings.Default.TaskFolderTitleMultiUser = false;
                }

                //finished first startup code
                Settings.Default.FirstRun = false; 
            }

            if (Settings.Default.LanguageChanged)
            {
                AddJumpList();
                Settings.Default.LanguageChanged = false;
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LanguageHelper();

            try
            {
                Updater updater = new Updater(false);
                updater.CheckNewVersion(); //check github xaml file for a higher version number than installed
            }
            catch
            {

            }
        }

        //region time and language
        private void LanguageHelper()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.Language.ToString()))
            {
                Settings.Default.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToString();
            }
            var langCode = new CultureInfo(Settings.Default.Language);
            CultureInfo.CurrentUICulture = langCode;
            CultureInfo.CurrentCulture = langCode;
            CultureInfo.DefaultThreadCurrentUICulture = langCode;
            CultureInfo.DefaultThreadCurrentCulture = langCode;
        }

        private void SystemTimeFormat()
        {
            try
            {
                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                sysFormat = sysFormat.Substring(0, sysFormat.IndexOf(":"));
                if (sysFormat.Equals("hh") | sysFormat.Equals("h"))
                {
                    Settings.Default.AlterTime = true;
                }
            }
            catch
            {

            }
        }

        //jump list
        private void AddJumpList()
        {
            JumpTask darkJumpTask = new JumpTask
            {
                //Dark theme
                Title = Properties.Resources.lblDarkTheme,
                Arguments = Command.Dark,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };
            JumpTask lightJumpTask = new JumpTask
            {
                //Light theme
                Title = Properties.Resources.lblLightTheme,
                Arguments = Command.Light,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };
            JumpTask resetJumpTask = new JumpTask
            {
                //Reset
                Title = Properties.Resources.lblReset,
                Arguments = Command.NoForce,
                CustomCategory = Properties.Resources.lblSwitchTheme
            };

            JumpList jumpList = new JumpList();
            jumpList.JumpItems.Add(darkJumpTask);
            jumpList.JumpItems.Add(lightJumpTask);
            jumpList.JumpItems.Add(resetJumpTask);
            jumpList.ShowFrequentCategory = false;
            jumpList.ShowRecentCategory = false;

            JumpList.SetJumpList(Application.Current, jumpList);
        }

        //application close behaviour
        private void Window_Closed(object sender, EventArgs e)
        {
            NetMQConfig.Cleanup();
            Settings.Default.Save();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill(); //needs kill if user uses location service
        }

        /// <summary>
        /// Navbar / NavigationView
        /// </summary>
        
        //change displayed page based on selection
        private void NavBar_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                var navItemTag = args.SelectedItemContainer.Tag.ToString();

                switch (navItemTag)
                {
                    case "time":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageTime.xaml", UriKind.Relative));
                        break;
                    case "apps":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageApps.xaml", UriKind.Relative));
                        break;
                    case "wallpaper":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageWallpaper.xaml", UriKind.Relative));
                        break;
                    case "settings":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageSettings.xaml", UriKind.Relative));
                        break;
                    case "donation":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageDonation.xaml", UriKind.Relative));
                        break;
                    case "about":
                        FrameNavbar.Navigate(new Uri(@"/Pages/PageAbout.xaml", UriKind.Relative));
                        break;
                }
            }
        }

        //startup page
        private void NavBar_Loaded(object sender, RoutedEventArgs e)
        {
            NavBar.SelectedItem = NavBar.MenuItems[1];
        }

        //Block back and forward on the frame
        private void FrameNavbar_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Forward | e.NavigationMode == NavigationMode.Back)
            {
                e.Cancel = true;
            }
        }
    }
}