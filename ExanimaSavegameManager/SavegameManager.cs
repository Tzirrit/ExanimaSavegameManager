using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace ExanimaSavegameManager
{
    public class SavegameManager
    {
        public string SavegameFolder { get; private set; }
        public string BackupFolder { get; private set; }

        private ILogger _logger;
        private bool _isWatching;

        public SavegameManager(string savegameFolder, string backupFolder, ILogger logger = null)
        {
            SavegameFolder = savegameFolder;
            BackupFolder = backupFolder;

            _logger = logger;
        }

        public void Start()
        {
            // Skip, if already watching
            if (_isWatching)
                return;

            LogMessage($"Watching for file changes at {SavegameFolder}...");
            StartWatcher();
        }

        #region FileSystemWatcher
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void StartWatcher()
        {
            // If directory is not specified or does not exist, abort.
            if (SavegameFolder == null || !Directory.Exists(SavegameFolder))
            {
                LogMessage($"SaveGame Folder '{SavegameFolder}' does not exist.");
                return;
            }

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = SavegameFolder;
            watcher.NotifyFilter = 
                NotifyFilters.LastWrite | 
                NotifyFilters.LastAccess;
            // Only watch Exanima savegame files. TODO: make configurable
            watcher.Filter = "*.rsg";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnFileChanged);
            watcher.Created += new FileSystemEventHandler(OnFileCreated);
            watcher.Deleted += new FileSystemEventHandler(OnFileDeleted);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            _isWatching = true;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            LogMessage($"New file '{e.Name}' created.");

            TryBackupFile(e.Name);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            LogMessage($"File '{e.Name}' changed.");

            TryBackupFile(e.Name);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            LogMessage("File: " + e.FullPath + " " + e.ChangeType);
        }
        #endregion

        #region File Manipulation
        private bool TryBackupFile(string filename)
        {
            // If backup folder does not exist, create it
            if (!Directory.Exists(BackupFolder))
            {
                LogMessage($"Creating missing Backup Folder '{SavegameFolder}'...");
                Directory.CreateDirectory(BackupFolder);
            }

            string targetFilename = $"{filename}_{DateTime.Now.ToFileTime()}";

            LogMessage($"Copying '{filename}' to '{targetFilename}'...");

            try
            {
                // Will not overwrite if the destination file already exists.
                File.Copy(Path.Combine(SavegameFolder, filename), Path.Combine(BackupFolder, targetFilename));
            }
            // Catch exception if the file was already copied.
            catch (IOException copyError)
            {
                _logger.LogMessage(copyError.Message);
                return false;
            }

            return true;
        }

        private bool SwapFiles(string targetFilename, string sourceFilename)
        {
            string backupFilename = $"{targetFilename}_{DateTime.Now.ToFileTime()}";

            try
            {
                // Replace target file with source file (creating backup)
                File.Replace(
                    Path.Combine(BackupFolder, sourceFilename),
                    Path.Combine(SavegameFolder, targetFilename),
                    Path.Combine(BackupFolder, backupFilename));
            }
            // Catch exception if the file was already copied.
            catch (IOException replaceError)
            {
                _logger.LogMessage(replaceError.Message);
                return false;
            }

            return true;
        }

        public IEnumerable<string> GetValidGames()
        {
            List<string> validGames = new List<string>();

            string[] filePaths = Directory.GetFiles(BackupFolder);
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                var split = fileName.Split('.');

                // If file is valid savegame
                if (split[1].Contains("rsg_"))
                {
                    // Add it to list, if not already contained
                    if(!validGames.Contains(split[0]))
                        validGames.Add(split[0]);
                }
            }
            return validGames;
        }

        public IEnumerable<string> GetGameBackups(string gameName)
        {
            List<string> backups = new List<string>();

            string[] filePaths = Directory.GetFiles(BackupFolder);
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                var split = fileName.Split('.');

                // If file is 1) for desired game and 2) valid savegame
                if(split[0] == gameName && split[1].Contains("rsg_"))
                {
                    // Add it to list, if not already contained
                    if (!backups.Contains(fileName))
                        backups.Add(fileName);

                    if (split[1].Length > 3)
                    {
                        long filetime;
                        long.TryParse(split[1].Substring(4), out filetime);
                        var time = DateTime.FromFileTime(filetime).ToLocalTime();

                        _logger.LogMessage($"Found backup '{split[1]}' for game '{gameName}' from {time}");
                    }
                }
            }
            return backups;
        }

        public DateTime GetBackupDate(string filename)
        {
            var split = filename.Split('.');

            // If file is valid savegame
            if (split[1].Contains("rsg_"))
            {
                if (split[1].Length > 3)
                {
                    long filetime;
                    long.TryParse(split[1].Substring(4), out filetime);

                    return DateTime.FromFileTime(filetime); ;
                }
            }
            return new DateTime();
        }

        #endregion


        #region Logging
        private void LogMessage(string message)
        {
            if (_logger != null)
                _logger.LogMessage(message);
            else
                Console.WriteLine(message);
        }
        #endregion

        // TESTING
        public void CheckBackups()
        {
            string[] filePaths = Directory.GetFiles(BackupFolder);
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                var split = fileName.Split('.');
                
                if (split[1].Contains("rsg"))
                {
                    _logger.LogMessage($"Exanima file {split[1]} for game {split[0]}");

                    if (split[1].Length > 3)
                    {
                        long filetime;
                        long.TryParse(split[1].Substring(4), out filetime);
                        var time = DateTime.FromFileTime(filetime).ToLocalTime();

                        _logger.LogMessage($"saved at filetime: {time}");
                    }
                }
            }
                
        }

    }
}
