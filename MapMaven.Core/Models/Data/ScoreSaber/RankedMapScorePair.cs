﻿using MapMaven.Core.ApiClients.ScoreSaber;

namespace MapMaven.Core.Models.Data.ScoreSaber
{
    public class RankedMapScorePair
    {
        public RankedMap Map { get; set; }
        public PlayerScore PlayerScore { get; set; }
    }
}
