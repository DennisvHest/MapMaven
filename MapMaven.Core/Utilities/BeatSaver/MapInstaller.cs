using BeatSaberTools.Models;
using BeatSaverSharp.Models;
using System.IO.Compression;

namespace BeatSaberTools.Core.Utilities.BeatSaver
{
    /// <summary>
    /// Credit: ModAssistant map install code: https://github.com/Assistant/ModAssistant/blob/f8bd80031df8514f59dc974f855b65f989adb5c7/ModAssistant/Classes/External%20Interfaces/BeatSaver.cs
    /// </summary>
    public static class MapInstaller
    {
        public static readonly char[] IllegalCharacters = new char[]
        {
            '<', '>', ':', '/', '\\', '|', '?', '*', '"',
            '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
            '\u0008', '\u0009', '\u000a', '\u000b', '\u000c', '\u000d', '\u000e', '\u000d',
            '\u000f', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016',
            '\u0017', '\u0018', '\u0019', '\u001a', '\u001b', '\u001c', '\u001d', '\u001f',
        };

        public static async Task InstallMap(Beatmap map, string mapsLocation, IProgress<double>? progress = null)
        {
            var zipBytes = await map.LatestVersion.DownloadZIP(progress: progress);

            string directory = GetMapDirectory(map, mapsLocation);

            using var stream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(stream);

            foreach (ZipArchiveEntry file in archive.Entries)
            {
                var fileDirectory = Path.GetDirectoryName(Path.Combine(directory, file.FullName));

                if (!Directory.Exists(fileDirectory))
                    Directory.CreateDirectory(fileDirectory);

                if (!string.IsNullOrEmpty(file.Name))
                    file.ExtractToFile(Path.Combine(directory, file.FullName), true);
            }
        }

        public static bool MapIsInstalled(Map map, string mapsLocation)
        {
            return Directory.EnumerateDirectories(mapsLocation, $"{map.Id}*").Any();
        }

        public static string GetMapDirectory(Beatmap map, string mapsLocation) => GetMapDirectory(map.ID, map.Metadata.SongName, map.Metadata.LevelAuthorName, mapsLocation);

        public static string GetMapDirectory(string id, string songName, string levelAuthorName, string mapsLocation)
        {
            var mapName = string.Concat($"{id} ({songName} - {levelAuthorName})".Split(IllegalCharacters));

            var directory = Path.Combine(mapsLocation, mapName);

            return directory;
        }
    }
}
