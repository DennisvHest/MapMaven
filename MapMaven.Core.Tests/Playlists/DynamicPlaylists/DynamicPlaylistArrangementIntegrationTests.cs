﻿using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Models.DynamicPlaylists;
using Newtonsoft.Json.Linq;
using MapMaven.Models;
using Playlist = MapMaven.Models.Playlist;
using MapMaven.Services;
using BeatSaber.SongHashing;
using System.Reactive.Linq;
using MapMaven.Core.Models.Data;
using System.IO.Abstractions.TestingHelpers;
using MapMaven.Core.ApiClients;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Utilities.Scoresaber;

namespace MapMaven.Core.Tests.Playlists.DynamicPlaylists;

public class DynamicPlaylistArrangementIntegrationTests
{
    private readonly Mock<IApplicationSettingService> _applicationSettingServiceMock = new();
    private readonly Mock<IPlaylistService> _playlistServiceMock = new();
    private readonly Mock<IBeatSaberDataService> _beatSaberDataServiceMock = new();
    private readonly Mock<IScoreSaberService> _scoreSaberServiceMock = new();
    private readonly Mock<IMapService> _mapServiceMock = new();
    private readonly Mock<ILogger<DynamicPlaylistArrangementService>> _dynamicPlaylistArrangementServiceLoggerMock = new();
    private readonly Mock<ILogger<BeatSaberDataService>> _beatSaberDataServiceLoggerMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ScoreSaberApiClient> _scoreSaberApiClientMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<IApplicationEventService> _applicationEventServiceMock = new();

    private readonly BeatSaberDataService _beatSaberDataService;
    private readonly BeatSaberFileService _beatSaberFileService;
    private readonly MapService _mapService;

    private readonly DynamicPlaylistArrangementService _sut;

    public DynamicPlaylistArrangementIntegrationTests()
    {
        _applicationSettingServiceMock
            .SetupGet(x => x.ApplicationSettings)
            .Returns(Observable.Empty<Dictionary<string, ApplicationSetting>>());

        _beatSaberFileService = new(_applicationSettingServiceMock.Object);


        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
        });

        _beatSaberDataService = new(new Hasher(), _beatSaberFileService, _serviceProviderMock.Object, _beatSaberDataServiceLoggerMock.Object, fileSystem);

        _beatSaberDataServiceMock
            .Setup(x => x.LoadAllMapInfo())
            .Callback(() => _beatSaberDataService.LoadAllMapInfo());

        _beatSaberDataServiceMock
            .SetupGet(x => x.MapInfo)
            .Returns(() => _beatSaberDataService.MapInfo);

        _scoreSaberServiceMock
            .SetupGet(x => x.PlayerScores)
            .Returns(() => Observable.Return(Enumerable.Empty<PlayerScore>()));

        _scoreSaberServiceMock
            .SetupGet(x => x.RankedMaps)
            .Returns(() => Observable.Return(Enumerable.Empty<RankedMap>()));

        _scoreSaberServiceMock
            .SetupGet(x => x.ScoreEstimates)
            .Returns(() => Observable.Return(Enumerable.Empty<ScoreEstimate>()));

        _scoreSaberServiceMock
            .SetupGet(x => x.RankedMapScoreEstimates)
            .Returns(() => Observable.Return(Enumerable.Empty<ScoreEstimate>()));

        _scoreSaberServiceMock
            .SetupGet(x => x.PlayerProfile)
            .Returns(() => Observable.Return(new ApiClients.Player { Id = "1" }));

        _mapService = new(_beatSaberDataService, _scoreSaberServiceMock.Object, null!, _beatSaberFileService, _serviceProviderMock.Object);

        _mapServiceMock
            .SetupGet(x => x.CompleteMapData)
            .Returns(() => _mapService.CompleteMapData);

        _mapServiceMock
            .SetupGet(x => x.CompleteRankedMapData)
            .Returns(() => _mapService.CompleteRankedMapData);

        _sut = new DynamicPlaylistArrangementService(
            _beatSaberDataServiceMock.Object,
            _mapServiceMock.Object,
            _playlistServiceMock.Object,
            _scoreSaberServiceMock.Object,
            _applicationSettingServiceMock.Object,
            _dynamicPlaylistArrangementServiceLoggerMock.Object);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_ArrangesPlaylistBasedOnConfig()
    {
        return; //WIP

        var playlistMock = new Mock<IPlaylist>();

        object customData = new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 1 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.SongAuthorName) },
                                { nameof(FilterOperation.Value), "Camellia" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Pp) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) }
                            }
                        }
                    }
                }
            }
        };

        playlistMock
            .Setup(x => x.TryGetCustomData("mapMaven", out customData))
            .Returns(true);

        _beatSaberDataServiceMock
            .Setup(x => x.GetAllPlaylists())
            .ReturnsAsync(new[] { playlistMock.Object });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Equal(2, resultMaps.Count());
    }
}
