﻿namespace MapMaven.Core.Models.LivePlaylists
{
    public class LivePlaylistFieldOption
    {
        public string Value { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool HasPredefinedOptions { get; set; }
        public bool Sortable { get; set; }
    }
}
