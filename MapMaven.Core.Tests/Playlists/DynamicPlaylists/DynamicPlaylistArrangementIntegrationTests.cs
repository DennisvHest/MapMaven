using MapMaven.Core.Services.Interfaces;
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
using Microsoft.Extensions.DependencyInjection;
using MapMaven.Models.Data;
using MockQueryable.Moq;
using MapMaven.Core.Models.Data.RankedMaps;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Models;
using MapMaven.Core.Models.Data.Leaderboards;
using MapMaven.Core.Services.Leaderboards.ScoreEstimation;

namespace MapMaven.Core.Tests.Playlists.DynamicPlaylists;

public class DynamicPlaylistArrangementIntegrationTests
{
    private readonly Mock<IApplicationSettingService> _applicationSettingServiceMock = new();
    private readonly Mock<IPlaylistService> _playlistServiceMock = new();
    private readonly Mock<IBeatSaberDataService> _beatSaberDataServiceMock = new();
    private readonly Mock<ILeaderboardService> _leaderboardService = new();
    private readonly Mock<IMapService> _mapServiceMock = new();
    private readonly Mock<ILogger<DynamicPlaylistArrangementService>> _dynamicPlaylistArrangementServiceLoggerMock = new();
    private readonly Mock<ILogger<BeatSaberDataService>> _beatSaberDataServiceLoggerMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<IBeatmapHasher> _hasherMock = new();
    private readonly Mock<ILeaderboardDataService> _leaderboardDataService = new();
    private readonly Mock<IScoreEstimationService> _scoreEstimationService = new();
    private readonly Mock<ILeaderboardProviderService> _leaderboardProviderService = new();

    private readonly BeatSaberDataService _beatSaberDataService;
    private readonly BeatSaberFileService _beatSaberFileService;
    private readonly SongPlayerService _songPlayerService;
    private readonly MapService _mapService;

    private readonly DynamicPlaylistArrangementService _sut;

    public DynamicPlaylistArrangementIntegrationTests()
    {
        _applicationSettingServiceMock
            .SetupGet(x => x.ApplicationSettings)
            .Returns(Observable.Empty<Dictionary<string, ApplicationSetting>>());

        _applicationSettingServiceMock
            .SetupGet(x => x.ApplicationSettings)
            .Returns(Observable.Return(new Dictionary<string, ApplicationSetting>()
            {
                { "BeatSaberInstallLocation", new() { StringValue = "C:/" } }
            }));

        _beatSaberFileService = new(_applicationSettingServiceMock.Object);
        var fileSystem = MockFileSystem();

        var serviceScopeMock = new Mock<IServiceScope>();

        serviceScopeMock
            .SetupGet(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();

        serviceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);

        MockMapInfoCache();

        _hasherMock = new Mock<IBeatmapHasher>();

        _beatSaberDataService = new(_hasherMock.Object, _beatSaberFileService, _serviceProviderMock.Object, _beatSaberDataServiceLoggerMock.Object, fileSystem);

        _songPlayerService = new(_beatSaberDataService);

        _beatSaberDataServiceMock
            .Setup(x => x.LoadAllMapInfo())
            .Returns(() =>
            {
                MockMapInfoCache();
                return _beatSaberDataService.LoadAllMapInfo();
            });

        _beatSaberDataServiceMock
            .SetupGet(x => x.MapInfo)
            .Returns(() => _beatSaberDataService.MapInfo);

        _leaderboardService
            .SetupGet(x => x.LeaderboardProviders)
            .Returns(() => new() { { LeaderboardProvider.ScoreSaber, _leaderboardProviderService.Object } });

        _leaderboardService
            .SetupGet(x => x.PlayerScores)
            .Returns(() => Observable.Return(Enumerable.Empty<Models.PlayerScore>()));

        _leaderboardProviderService
            .SetupGet(x => x.PlayerScores)
            .Returns(() => Observable.Return(Enumerable.Empty<Models.PlayerScore>()));

        MockRankedMaps();

        MockScoreEstimates();

        _leaderboardService
            .SetupGet(x => x.PlayerProfile)
            .Returns(() => Observable.Return(new PlayerProfile { Id = "1" }));

        _mapService = new(_beatSaberDataService, _leaderboardService.Object, null!, _beatSaberFileService, _serviceProviderMock.Object, _songPlayerService, _leaderboardDataService.Object);

        _mapServiceMock
            .SetupGet(x => x.CompleteMapData)
            .Returns(() => _mapService.CompleteMapData);

        _mapServiceMock
            .SetupGet(x => x.CompleteRankedMapData)
            .Returns(() => _mapService.CompleteRankedMapData);

        _mapServiceMock
            .Setup(x => x.GetCompleteRankedMapDataForLeaderboardProvider(It.IsAny<LeaderboardProvider>()))
            .Returns<LeaderboardProvider>(provider => _mapService.GetCompleteRankedMapDataForLeaderboardProvider(provider));

        _mapServiceMock
            .Setup(x => x.RefreshDataAsync(It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(() =>
            {
                MockMapInfoCache();
                return _mapService.RefreshDataAsync(true, true);
            });

        _sut = new DynamicPlaylistArrangementService(
            _beatSaberDataServiceMock.Object,
            _mapServiceMock.Object,
            _playlistServiceMock.Object,
            _leaderboardService.Object,
            _applicationSettingServiceMock.Object,
            _dynamicPlaylistArrangementServiceLoggerMock.Object);
    }

    private void MockScoreEstimates()
    {
        var scoreEstimates = new ScoreEstimate[]
        {
            new() { MapHash = "1", PPIncrease = 5, Difficulty = "Expert" },
            new() { MapHash = "2", PPIncrease = 10, Difficulty = "ExpertPlus" },
        };

        _scoreEstimationService
            .SetupGet(x => x.RankedMapScoreEstimates)
            .Returns(() => Observable.Return(scoreEstimates));

        _leaderboardService
            .SetupGet(x => x.ScoreEstimationServices)
            .Returns(() => new() { { LeaderboardProvider.ScoreSaber, _scoreEstimationService.Object } });

        _leaderboardService
            .SetupGet(x => x.RankedMapScoreEstimates)
            .Returns(() => Observable.Return(scoreEstimates));
    }

    private void MockRankedMaps()
    {
        var rankedMaps = new Dictionary<string, RankedMapInfoItem>()
        {
            { 
                "1", new()
                {
                    SongHash = "1",
                    Name = "KillerToy",
                    SongAuthorName = "Camellia",
                    Difficulties = new RankedMapDifficultyInfo[]
                    {
                        new()
                        {
                            Difficulty = "Expert"
                        }
                    },
                    Duration = TimeSpan.FromSeconds(60)
                }
            },
            {
                "2", new()
                {
                    SongHash = "2",
                    Name = "Nacreous Snowmelt",
                    SongAuthorName = "Camellia",
                    Difficulties = new RankedMapDifficultyInfo[]
                    {
                        new()
                        {
                            Difficulty = "ExpertPlus"
                        }
                    },
                    Duration = TimeSpan.FromSeconds(125)
                }
            }
        };

        _leaderboardService
            .SetupGet(x => x.RankedMaps)
            .Returns(() => Observable.Return(rankedMaps));

        _leaderboardProviderService
            .SetupGet(x => x.RankedMaps)
            .Returns(() => Observable.Return(rankedMaps));
    }

    private void MockMapInfoCache()
    {
        var dataStoreMock = new Mock<IDataStore>();

        var mockMapInfoCache = new List<MapInfo>
        {
            new() { Hash = "test123", SongName = "Come Alive", SongAuthorName = "test123", DirectoryPath = "188b1 (Come Alive - Fatalution)" }
        };

        dataStoreMock
            .Setup(x => x.Set<MapInfo>())
            .Returns(mockMapInfoCache.AsQueryable().BuildMockDbSet().Object);

        dataStoreMock
            .Setup(x => x.Set<HiddenMap>())
            .Returns(Enumerable.Empty<HiddenMap>().AsQueryable().BuildMockDbSet().Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IDataStore)))
            .Returns(dataStoreMock.Object);
    }

    private static MockFileSystem MockFileSystem()
    {
        var maps = new TestMap[]
        {
            new()
            {
                MapInfo = new() { SongName = "KillerToy", SongAuthorName = "Camellia", DirectoryPath = "1A466 ([Queue] Camellia - KillerToy - Jabob)" },
                Hash = "1"
            },
            new()
            {
                MapInfo = new() { SongName = "Nacreous Snowmelt", SongAuthorName = "Camellia", DirectoryPath = "6F01 (Camellia - Nacreous Snowmelt - Dack)" },
                Hash = "2"
            },
            new()
            {
                MapInfo = new() { SongName = "Come Alive", SongAuthorName = "Pendulum", DirectoryPath = "188b1 (Come Alive - Fatalution)" },
                Hash = "test123"
            },
            new()
            {
                MapInfo = new() { SongName = "Halcyon", SongAuthorName = "xi", DirectoryPath = "235b (Halcyon - splake)" },
                HasSongCoreHash = false
            },
            new()
            {
                MapInfo = new() { SongName = "Duplicate ID", SongAuthorName = "TEST 1", DirectoryPath = "ABCD1 (Duplicate ID 1)" },
                CreatedDateTime = new DateTime(2023, 02, 10, 0, 0, 0)
            },
            new()
            {
                MapInfo = new() { SongName = "Duplicate ID", SongAuthorName = "TEST 2", DirectoryPath = "ABCD1 (Duplicate ID 2)" },
                CreatedDateTime = new DateTime(2023, 02, 11, 12, 30, 0)
            }
        };

        var songHashData = GetMockSongHashData(maps);

        var mockFiles = new Dictionary<string, MockFileData>
        {
            { "/UserData/SongCore/SongHashData.dat", new MockFileData(songHashData) },
        };

        AddMockMapInfo(mockFiles, maps);

        return new MockFileSystem(mockFiles);
    }

    private static string GetMockSongHashData(IEnumerable<TestMap> maps)
    {
        var mapsJson = maps.Where(m => m.HasSongCoreHash).Select(m => $$"""
            ".\\Beat Saber_Data\\CustomLevels\\{{m.MapInfo.DirectoryPath}}": {
                "directoryHash": 1,
                "songHash": "{{m.Hash ?? Guid.NewGuid().ToString()}}"
            }
        """);

        return $$"""
        {
            {{string.Join(",", mapsJson)}}
        }
        """;
    }

    private static void AddMockMapInfo(Dictionary<string, MockFileData> mockFiles, TestMap[] maps)
    {
        foreach (var map in maps)
        {
            AddMockMapInfo(mockFiles, map);
        }
    }

    private static void AddMockMapInfo(Dictionary<string, MockFileData> mapInfoDictionary, TestMap mapInfo)
    {
        var mapInfoJson =
        $$"""
        {
          "_songName": "{{mapInfo.MapInfo.SongName}}",
          "_songAuthorName": "{{mapInfo.MapInfo.SongAuthorName}}"
        }
        """;

        var mockDirectoryData = new MockDirectoryData { CreationTime = mapInfo.CreatedDateTime };

        mapInfoDictionary.Add($"/Beat Saber_Data/CustomLevels/{mapInfo.MapInfo.DirectoryPath}/", mockDirectoryData);
        mapInfoDictionary.Add($"/Beat Saber_Data/CustomLevels/{mapInfo.MapInfo.DirectoryPath}/Info.dat", new MockFileData(mapInfoJson));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_ArrangesPlaylistBasedOnConfig()
    {
        AddMockPlaylistWithConfig(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Improvement) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 20 },
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
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) }
                            }
                        }
                    }
                }
            }
        });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Equal(2, resultMaps.Count());

        var firstMap = resultMaps.First();
        var secondMap = resultMaps.Last();

        Assert.Equal("Nacreous Snowmelt", firstMap.Name);
        Assert.Equal("KillerToy", secondMap.Name);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_FetchesMapsFromCache_IfHashIsFoundInCache()
    {
        AddMockPlaylistWithConfig(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 20 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(FilterOperation.Value), "Come Alive" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Single(resultMaps);

        var resultMap = resultMaps.First();

        Assert.Equal("Come Alive", resultMap.Name);
        Assert.Equal("test123", resultMap.SongAuthorName);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_HashesDirectory_IfHashWasNotFoundSongCoreHash()
    {
        AddMockPlaylistWithConfig(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 20 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(FilterOperation.Value), "Halcyon" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        });

        const string songDirectory = @"c:\beat saber_data\customlevels\235b (halcyon - splake)";

        _hasherMock
            .Setup(x => x.HashDirectoryAsync(songDirectory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashResult { ResultType = HashResultType.Success, Hash = "112233" });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        _hasherMock.Verify(x => x.HashDirectoryAsync(songDirectory, It.IsAny<CancellationToken>()), Times.AtLeastOnce());

        Assert.Single(resultMaps);

        var resultMap = resultMaps.First();

        Assert.Equal("112233", resultMap.Hash);
        Assert.Equal("Halcyon", resultMap.Name);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_AddsLatestAddedMap_IfContainsMapsWithDuplicateIds()
    {
        AddMockPlaylistWithConfig(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 20 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(FilterOperation.Value), "Duplicate ID" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Single(resultMaps);

        var resultMap = resultMaps.First();

        Assert.Equal("TEST 2", resultMap.SongAuthorName);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_ArrangesRankedMaps_IfImprovementMapPoolIsConfigured()
    {
        AddMockPlaylistWithConfig(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Improvement) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 20 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), $"{nameof(DynamicPlaylistMap.ScoreEstimate)}.{nameof(DynamicPlaylistScoreEstimate.PPIncrease)}" },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) },
                            }
                        }
                    }
                }
            }
        });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Equal(2, resultMaps.Count());

        var firstMap = resultMaps.First();
        var secondMap = resultMaps.Last();

        Assert.Equal("Nacreous Snowmelt", firstMap.Name);
        Assert.Equal("KillerToy", secondMap.Name);
    }

    private void AddMockPlaylistWithConfig(object customData)
    {
        var playlistMock = new Mock<IPlaylist>();

        playlistMock
            .Setup(x => x.TryGetCustomData("mapMaven", out customData))
            .Returns(true);

        _beatSaberDataServiceMock
            .Setup(x => x.GetAllPlaylists())
            .ReturnsAsync(new[] { playlistMock.Object });
    }

    private class TestMap
    {
        public MapInfo MapInfo { get; set; }
        public string? Hash { get; set; }
        public bool HasSongCoreHash { get; set; } = true;
        public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.Now;
    }
}
