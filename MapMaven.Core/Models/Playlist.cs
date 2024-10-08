﻿using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.LivePlaylists;
using MapMaven.Extensions;
using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json.Linq;
using Image = System.Drawing.Image;

namespace MapMaven.Models
{
    public class Playlist
    {
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public IEnumerable<PlaylistMap> Maps { get; set; }

        public LivePlaylistConfiguration LivePlaylistConfiguration { get; set; }

        public bool IsLivePlaylist => LivePlaylistConfiguration != null;

        public Lazy<string?> CoverImage { get; private set; }
        public Lazy<string?> CoverImageSmall { get; private set; }
        public bool HasCover => _playlist.HasCover;

        public string PlaylistFilePath { get; private set; }

        public PlaylistManager PlaylistManager { get; private set; }

        private IPlaylist _playlist;

        public Playlist(IPlaylist playlist, PlaylistManager playlistManager)
        {
            _playlist = playlist;
            PlaylistManager = playlistManager;
            FileName = playlist.Filename;

            PlaylistFilePath = Path.Join(PlaylistManager?.PlaylistPath, FileName).NormalizePath();

            Title = playlist.Title;
            Description = playlist.Description;

            CoverImage = new Lazy<string?>(() => GetCoverImage());
            CoverImageSmall = new Lazy<string?>(() => GetCoverImage(50));

            Maps = playlist.Select(s => new PlaylistMap(s));

            if (playlist.TryGetCustomData("mapMaven", out dynamic customData))
            {
                if (customData.dynamicPlaylistConfiguration is LivePlaylistConfiguration livePlaylistConfiguration)
                {
                    LivePlaylistConfiguration = livePlaylistConfiguration;
                }
                else if (customData.dynamicPlaylistConfiguration is JObject configuration)
                {
                    LivePlaylistConfiguration = configuration.ToObject<LivePlaylistConfiguration>();
                }
            }
        }

        public string? GetCoverImage(int size = 0)
        {
            try
            {
                Image? coverImage = null;

                using (var coverImageStream = _playlist.GetCoverStream())
                {
                    if (coverImageStream != null && _playlist.HasCover)
                        coverImage = Image.FromStream(coverImageStream);

                    using (coverImage)
                    {
                        if (size > 0)
                            coverImage = coverImage?.GetResizedImage(size, size);

                        return coverImage?.ToDataUrl();
                    }
                }
            }
            catch
            {
                /* Ignore invalid cover images */
                return null;
            }
        }

        public Stream? GetCoverImageStream()
        {
            return _playlist.GetCoverStream();
        }

        public override bool Equals(object? obj)
        {
            var otherPlaylist = obj as Playlist;

            if (otherPlaylist == null)
                return false;

            return FileName == otherPlaylist.FileName && PlaylistManager?.PlaylistPath == otherPlaylist.PlaylistManager?.PlaylistPath;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, PlaylistManager?.PlaylistPath);
        }
    }
}
