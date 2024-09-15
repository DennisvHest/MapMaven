using MapMaven.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace MapMaven
{
    public class MapMavenBlazorWebView : BlazorWebView
    {
        public override IFileProvider CreateFileProvider(string contentRootDir)
        {
            return new CompositeFileProvider(new MapMavenFileProvider(), base.CreateFileProvider(contentRootDir));
        }
    }

    public class MapMavenFileProvider : IFileProvider
    {
        private static readonly char[] _pathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath))
                return new NotFoundFileInfo(subpath);

            // Relative paths starting with leading slashes are okay
            subpath = subpath.TrimStart(_pathSeparators);

            if (!subpath.StartsWith("__MAPIMAGES__"))
                return new NotFoundFileInfo(subpath);

            var mapId = subpath
                .Replace("__MAPIMAGES__", string.Empty)
                .TrimStart(_pathSeparators);

            var fullPath = BeatSaberDataService.Instance.GetMapCoverImageFilePath(mapId);

            if (string.IsNullOrEmpty(fullPath))
                return new NotFoundFileInfo(subpath);

            var fileInfo = new FileInfo(fullPath);

            return new PhysicalFileInfo(fileInfo);
        }

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
