# A savegame backup tool for Exanima

Exanima by http://www.baremettle.com/ is a great game. However, if you want more control over your savegame files, this tool is for you.

The tool monitors Exanimas savegame folder for updates and generates a backup every time a savegame is created or updated. In the inevitable event that unpleasant things happen, you can just "restore" an older savegame.

## Usage
Download the most recent release, or build the project yourself.

### Configuration
When starting for the first time, you need do configure the tool as follows

**Exanima game client**:
Where Exanima.exe is located. Usually this is within your Steam folder, e.g. C:\Steam\steamapps\common\Exanima\Exanima.exe

**Savegame path**:
Where Exanima stores its savegames. Usually this is within the AppData folder of your User, e.g. C:\Users\YOURUSERNAME\AppData\Roaming\Exanima

**Backup path**:
Folder where the tool will store your backups. This can be anywhere, e.g. C:\Users\YOURUSERNAME\AppData\Roaming\Exanima\Backups

###
When properly configured, you now start Exanima by clicking on the **Start Game** button. Whenever a new savegame file is created (usually on exiting the game), a backup is created.
You can see all games by their name (e.g. Arena001, or Exanima003) and available backups by date of creation. Simply select the backup you want to restore and click on the **Restore** button. The tool will now restore that backup (and create a backup of your original savegame, because I hear you like backups).


## Note
This tool monitors .rsg files only.
Exanima only saves progress when exiting the game. It is recommended that you completely exit the game to save your progress and create a new backup file.
