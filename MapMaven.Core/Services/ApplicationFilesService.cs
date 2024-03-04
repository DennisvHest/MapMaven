using Microsoft.Data.Sqlite;
using Serilog;
using System.IO.Abstractions;

namespace MapMaven.Core.Services
{
    public class ApplicationFilesService
    {
        private readonly IFileSystem _fileSystem;

        public ApplicationFilesService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void DeleteApplicationFiles()
        {
            if (!_fileSystem.Directory.Exists(BeatSaberFileService.AppDataLocation))
                return;

            var fileCount = _fileSystem.Directory
                .EnumerateFiles(BeatSaberFileService.AppDataLocation, "*", SearchOption.AllDirectories)
                .Count();

            if (fileCount >= 100)
                throw new Exception("Appdata directory contains more than 50 files. Fail safe measure to prevent accidental deletion of other directories.");

            Log.CloseAndFlush(); // This is necessary to prevent the log file from being locked by the application
            SqliteConnection.ClearAllPools(); // This is necessary to prevent the database file from being locked by the application

            _fileSystem.Directory.Delete(BeatSaberFileService.AppDataLocation, recursive: true);
        }
    }
}
