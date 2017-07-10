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

            CreateFileBackup(e.Name);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            LogMessage($"File '{e.Name}' changed.");

            CreateFileBackup(e.Name);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            LogMessage("File: " + e.FullPath + " " + e.ChangeType);
        }
        #endregion

        #region File Manipulation
        private bool CreateFileBackup(string sourceFilename)
        {
            // If backup folder does not exist, create it
            if (!Directory.Exists(BackupFolder))
            {
                LogMessage($"Creating missing Backup Folder '{SavegameFolder}'...");
                Directory.CreateDirectory(BackupFolder);
            }

            string targetFilename = $"{sourceFilename}_{DateTime.Now.ToFileTime()}";
            return CopyFile(BackupFolder, targetFilename, SavegameFolder, sourceFilename);
        }

        private bool CopyFile(string targetFolder, string targetFilename, string sourceFolder, string sourceFilename)
        {
            // Will not overwrite if the destination file already exists.
            try
            {
                File.Copy(Path.Combine(sourceFolder, sourceFilename), Path.Combine(targetFolder, targetFilename));
            }
            // Catch exception if the file was already copied.
            catch (IOException copyError)
            {
                _logger.LogMessage(copyError.Message);
                return false;
            }

            return true;
        }

        private bool ReplaceFile(string targetFilename, string sourceFilename)
        {
            string backupFilename = $"{targetFilename}_{DateTime.Now.ToFileTime()}";
            // Replace target file with source file (creating backup)
            try
            {
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

        public void LoadBackup(string gameName, string sourceFilename)
        {
            _logger.LogMessage($"Restoring backup from {GetBackupDate(sourceFilename)} for game '{gameName}'...");

            bool success = false;
            string targetFilename = $"{gameName}.rsg";
            // Check if game save exists
            if (File.Exists(Path.Combine(SavegameFolder, targetFilename)))
            {
                // If so, replace
                success = ReplaceFile(targetFilename, sourceFilename);
            }
            // Otherwise, copy backup
            else
            {
                success = CopyFile(SavegameFolder, targetFilename, BackupFolder, sourceFilename);
            }

            if (success)
                _logger.LogMessage("...done.");
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

        public IEnumerable<string> GetGameBackups(string gameName, bool verbose = false)
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

                        if(verbose)
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
    }
}
