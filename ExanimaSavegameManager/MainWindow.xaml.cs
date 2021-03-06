﻿using ExanimaSavegameManager.Controls;
using System;
using System.Diagnostics;
using System.Windows;

namespace ExanimaSavegameManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ILogger _logger;
        private SavegameManager _sgm;

        private bool _isGameRunning;
        private string _client;
        private string _savegameFolder;
        private string _backupFolder;
        //private int _maxBackups; TODO: Limit maximum number of backups

        public MainWindow()
        {
            InitializeComponent();

            _logger = new Logger();
            _logger.MessageAdded += OnLogMessageAdded;

            // Load and validate configuration
            LoadConfiguration();
            if (IsConfigurationValid())
            {
                g_ConfigurationOverlay.Visibility = Visibility.Hidden;
                InitializeSavegameManager();
            }
            else
            {
                g_ConfigurationOverlay.Visibility = Visibility.Visible;
                btn_ConfigurationOK.IsEnabled = IsConfigurationValid();
            }
        }

        private void InitializeSavegameManager(bool overwrite = false)
        {
            if (_sgm == null || overwrite)
            {
                _logger.LogMessage("Initializing SavegameManager...");

                // Initialize SavegameManager
                _sgm = new SavegameManager(_savegameFolder, _backupFolder, _logger);

                UpdateSavegameControls();
            }
        }

        private void UpdateSavegameControls()
        {
            sp_Controls.Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate ()
                    {
                        // Clear old controls
                        sp_Controls.Children.Clear();

                        // Generate new controls
                        foreach (var game in _sgm.GetValidGames())
                        {
                            sp_Controls.Children.Add(new SavegameControl(_sgm, game));
                        }
                    }
                ));
        }

        private void btn_Start_Click(object sender, RoutedEventArgs e)
        {
            // Skip if game is already running;
            if (_isGameRunning)
                return;

            // Start file watcher
            _sgm.Start();

            // Start game directly or via steam
            Process proc;
            if (_client.Contains("Steam.exe"))
            {
                string steamArgs = $"-applaunch {Properties.Settings.Default.SteamAppId}";
                _logger.LogMessage($"Launching via Steam...");

                proc = Process.Start(_client, steamArgs);
            }
            else
            {
                proc = Process.Start(_client);
            }

            if(proc != null)
             {
                proc.EnableRaisingEvents = true;
                proc.Exited += OnProcessExited;

                _isGameRunning = true;
                _logger.LogMessage($"Exanima launched sucessfully.");
            }
        }

        private void btn_OpenSavegameFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_savegameFolder);
        }

        private void btn_OpenBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(_backupFolder);
        }

        private void btn_Configure_Click(object sender, RoutedEventArgs e)
        {
            tb_CfgClient.Text = _client;
            tb_CfgSavegamePath.Text = _savegameFolder;
            tb_CfgBackupPath.Text = _backupFolder;

            g_ConfigurationOverlay.Visibility = Visibility.Visible;
        }


        private void OnProcessExited(object sender, EventArgs e)
        {
            _isGameRunning = false;
            _logger.LogMessage($"Exanima exited.");

            // Update SavegameControls
            UpdateSavegameControls();
        }

        private void OnLogMessageAdded(string message)
        {
            string formatedMessage = $"{message}\n";

            // Check if invoking is neccessary
            if (!tb_Log.Dispatcher.CheckAccess())
            {
                tb_Log.Dispatcher.Invoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  new Action(
                    delegate ()
                    {
                        tb_Log.AppendText(formatedMessage);
                        tb_Log.ScrollToEnd();
                    }
                ));
            }
            // If not, just write to the log
            else
            {
                tb_Log.AppendText(formatedMessage);
                tb_Log.ScrollToEnd();
            }
        }

        #region Configuration
        private void LoadConfiguration()
        {
            _client = Properties.Settings.Default.GameClient;
            _savegameFolder = Properties.Settings.Default.SavegameFolder;
            _backupFolder = Properties.Settings.Default.BackupFolder;

            // Set client selection checkbox (hacky)
            cb_CfgUseSteam.IsChecked = (_client != null && _client.Contains("Steam.exe"));
        }

        private bool SaveConfiguration()
        {
            bool configurationChanged = false;

            if (Properties.Settings.Default.GameClient != _client)
            {
                configurationChanged = true;
                Properties.Settings.Default.GameClient = _client;
            }

            if(Properties.Settings.Default.SavegameFolder != _savegameFolder)
            {
                configurationChanged = true;
                Properties.Settings.Default.SavegameFolder = _savegameFolder;
            }
            
            if(Properties.Settings.Default.BackupFolder != _backupFolder)
            {
                configurationChanged = true;
                Properties.Settings.Default.BackupFolder = _backupFolder;
            }
            
            Properties.Settings.Default.Save();

            return configurationChanged;
        }

        private bool IsConfigurationValid()
        {
            if (_client == null || _client == "")
                return false;

            if (_savegameFolder == null || _savegameFolder == "")
                return false;

            if (_backupFolder == null || _backupFolder == "")
                return false;

            return true;
        }


        private void cb_CfgUseSteam_Checked(object sender, RoutedEventArgs e)
        {
            // If client is not steam
            if(_client != null && !_client.Contains("Steam.exe"))
            {
                // Reset path
                tb_CfgClient.Text = null;
                _client = null;

                btn_ConfigurationOK.IsEnabled = false;
            }
        }

        private void cb_CfgUseSteam_Unchecked(object sender, RoutedEventArgs e)
        {
            // If client is not exanima
            if (_client != null && !_client.Contains("Exanima.exe"))
            {
                // Reset path
                tb_CfgClient.Text = null;
                _client = null;

                btn_ConfigurationOK.IsEnabled = false;
            }
        }
        private void tb_CfgClient_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.Filter = (cb_CfgUseSteam.IsChecked==true) ? "Steam client|Steam.exe" : "Exanima client|Exanima.exe";
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    tb_CfgClient.Text = file;
                    _client = file;
                    break;

                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    tb_CfgClient.Text = null;
                    break;
            }
            btn_ConfigurationOK.IsEnabled = IsConfigurationValid();
        }

        private void tb_CfgSavegamePath_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.UserProfile;
            var result = folderDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var path = folderDialog.SelectedPath;
                    tb_CfgSavegamePath.Text = path;
                    _savegameFolder = path;
                    break;

                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    tb_CfgSavegamePath.Text = null;
                    break;
            }
            btn_ConfigurationOK.IsEnabled = IsConfigurationValid();
        }

        private void tb_CfgBackupPath_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.UserProfile;
            var result = folderDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var path = folderDialog.SelectedPath;
                    tb_CfgBackupPath.Text = path;
                    _backupFolder = path;
                    break;

                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    tb_CfgBackupPath.Text = null;
                    break;
            }
            btn_ConfigurationOK.IsEnabled = IsConfigurationValid();
        }

        private void btn_ConfigurationOK_Click(object sender, RoutedEventArgs e)
        {
            if(SaveConfiguration())
            {
                // Re-Initialize SavegameManager
                InitializeSavegameManager(true);
            }
            g_ConfigurationOverlay.Visibility = Visibility.Hidden;
        }
        #endregion
    }
}
