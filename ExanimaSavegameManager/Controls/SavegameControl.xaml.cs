﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExanimaSavegameManager.Controls
{
    /// <summary>
    /// Interaction logic for SavegameControl.xaml
    /// </summary>
    public partial class SavegameControl : UserControl
    {
        public string GameName { get; private set; }

        private SavegameManager _sgm;
        private List<string> _backupFiles;
        private string _selectedBackupFile;

        public SavegameControl(SavegameManager saveGameManager, string gameName)
        {
            InitializeComponent();

            _sgm = saveGameManager;
            _backupFiles = new List<string>();
            GameName = gameName;

            UpdateBackups();
            UpdateVisuals();
        }

        public void UpdateBackups()
        {
            _backupFiles = _sgm.GetGameBackups(GameName).ToList();

            if(_backupFiles.Count > 0)
            {
                if(_selectedBackupFile != null)
                {
                    // Select first backup
                    _selectedBackupFile = _backupFiles[0];
                }
            }
            else if (_selectedBackupFile != null)
            {
                // Select no backup
                _selectedBackupFile = null;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            lbl_GameName.Content = GameName;

            btn_Swap.IsEnabled = (_selectedBackupFile != null);

            if (_selectedBackupFile != null)
            {
                var time = _sgm.GetBackupDate(_selectedBackupFile);
                lbl_BackupDate.Content = $"{time.ToShortDateString()} {time.ToLongTimeString()}";
            }
            else
            {
                lbl_BackupDate.Content = "No backup selected";
            }

            btn_PreviousFile.IsEnabled = (_backupFiles.Count > 1);
            btn_NextFile.IsEnabled = (_backupFiles.Count > 1);
        }

        private void btn_PreviousFile_Click(object sender, RoutedEventArgs e)
        {
            int index = _backupFiles.IndexOf(_selectedBackupFile) - 1;

            if (index < 0)
                index = _backupFiles.Count - 1;

            _selectedBackupFile = _backupFiles[index];

            UpdateVisuals();
        }

        private void btn_NextFile_Click(object sender, RoutedEventArgs e)
        {
            int index = _backupFiles.IndexOf(_selectedBackupFile) + 1;

            if (index >= _backupFiles.Count)
                index = 0;

            _selectedBackupFile = _backupFiles[index];

            UpdateVisuals();
        }

        private void btn_Swap_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
