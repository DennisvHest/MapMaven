using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.ApiClients.ScoreSaber;
using MapMaven.Core.Models.Data.ScoreSaber;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Models.DynamicPlaylists.MapInfo;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Core.Services.Leaderboards;
using MapMaven.Core.Tests.Maps;
using MapMaven.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reactive.Linq;
using Playlist = MapMaven.Models.Playlist;

namespace MapMaven.Core.Tests.Playlists.DynamicPlaylists;

public class DynamicPlaylistArrangementServiceTests
{
    private readonly Mock<IApplicationSettingService> _applicationSettingServiceMock = new();
    private readonly Mock<IPlaylistService> _playlistServiceMock = new();
    private readonly Mock<IBeatSaberDataService> _beatSaberDataServiceMock = new();
    private readonly Mock<ILeaderboardService> _scoreSaberServiceMock = new();
    private readonly Mock<IMapService> _mapServiceMock = new();
    private readonly Mock<ILogger<DynamicPlaylistArrangementService>> _loggerMock = new();

    private readonly DynamicPlaylistArrangementService _sut;

    public DynamicPlaylistArrangementServiceTests()
    {
        _sut = new DynamicPlaylistArrangementService(
            _beatSaberDataServiceMock.Object,
            _mapServiceMock.Object,
            _playlistServiceMock.Object,
            _scoreSaberServiceMock.Object,
            _applicationSettingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_NoDynamicPlaylists_DoesNothing()
    {
        // Arrange
        _beatSaberDataServiceMock
            .Setup(x => x.GetAllPlaylists())
            .ReturnsAsync(new List<IPlaylist>());

        // Act
        await _sut.ArrangeDynamicPlaylists();

        // Assert
        _playlistServiceMock.Verify(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithMapPool_UsesCorrectMapPool()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { "MapPool", nameof(MapPool.Improvement) },
                    { "MapCount", 1 }
                }
            }
        });

        Assert.Contains(resultMaps, x => x.Id == "102");
    }


    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithStringFilters_FiltersStringValue()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.SongAuthorName) },
                                { nameof(FilterOperation.Value), "Camellia" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(FilterOperation.Value), "Other test map" },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.NotEquals) }
                            }
                        }
                    }
                }
            }
        });

        Assert.Single(resultMaps);
        Assert.Contains(resultMaps, x => x.Id == "1");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDoubleFilters_FiltersDoubleRange()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(FilterOperation.Value), 3.3 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.GreaterThan) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(FilterOperation.Value), 5.2 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.GreaterThanOrEqual) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(FilterOperation.Value), 5.21 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.LessThan) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(FilterOperation.Value), 5.2 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.LessThanOrEqual) }
                            }
                        }
                    }
                }
            }
        });

        Assert.Single(resultMaps);
        Assert.Contains(resultMaps, x => x.Id == "3");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDoubleFilters_FiltersDoubleEqual()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Pp) },
                                { nameof(FilterOperation.Value), 20 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(FilterOperation.Value), 43.2 },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.NotEquals) }
                            },
                        }
                    }
                }
            }
        });

        Assert.Single(resultMaps);
        Assert.Contains(resultMaps, x => x.Id == "1");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDateTimeFilters_FiltersDateTimeRange()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2022, 1, 1).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.GreaterThan) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2022, 3, 25).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.GreaterThanOrEqual) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2023, 1, 1).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.LessThan) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2022, 3, 25).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.LessThanOrEqual) }
                            }
                        }
                    }
                }
            }
        });

        Assert.Single(resultMaps);
        Assert.Contains(resultMaps, x => x.Id == "3");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDateTimeFilters_FiltersDateTimeEqual()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2022, 3, 25).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            },
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), new DateTime(2023, 1, 12).ToString(CultureInfo.InvariantCulture) },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.NotEquals) }
                            },
                        }
                    }
                }
            }
        });

        Assert.Single(resultMaps);
        Assert.Contains(resultMaps, x => x.Id == "3");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithBooleanFilters_FiltersBoolean()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Played) },
                                { nameof(FilterOperation.Value), true },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        });

        Assert.Equal(2, resultMaps.Count());
        Assert.Contains(resultMaps, x => x.Id == "1");
        Assert.Contains(resultMaps, x => x.Id == "3");
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithStringSort_SortsByString()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Ascending) }
                            }
                        }
                    }
                }
            }
        });

        var mapOrder = new[] { "2", "3", "1" };

        resultMaps = resultMaps.Where(m => mapOrder.Contains(m.Id));

        Assert.Equal(3, resultMaps.Count());
        Assert.Equal(mapOrder, resultMaps.Select(x => x.Id));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithMultipleSorts_SortsByAllSorts()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.SongAuthorName) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) }
                            },
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Name) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Ascending) }
                            }
                        }
                    }
                }
            }
        });

        var mapOrder = new[] { "3", "2", "1" };

        resultMaps = resultMaps.Where(m => mapOrder.Contains(m.Id));

        Assert.Equal(3, resultMaps.Count());
        Assert.Equal(mapOrder, resultMaps.Select(x => x.Id));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDoubleSort_SortsByDouble()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Stars) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Ascending) }
                            }
                        }
                    }
                }
            }
        });

        var mapOrder = new[] { "1", "3", "2" };

        resultMaps = resultMaps.Where(m => mapOrder.Contains(m.Id));

        Assert.Equal(3, resultMaps.Count());
        Assert.Equal(mapOrder, resultMaps.Select(x => x.Id));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithDateTimeSort_SortsByDateTime()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) }
                            }
                        }
                    }
                }
            }
        });

        var mapOrder = new[] { "1", "3", "2" };

        resultMaps = resultMaps.Where(m => mapOrder.Contains(m.Id));

        Assert.Equal(3, resultMaps.Count());
        Assert.Equal(mapOrder, resultMaps.Select(x => x.Id));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylists_DynamicPlaylistWithBooleanSort_SortsByBoolean()
    {
        var resultMaps = await CallArrangeDynamicPlaylistWith(new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.SortOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(SortOperation.Field), nameof(DynamicPlaylistMap.Played) },
                                { nameof(SortOperation.Direction), nameof(SortDirection.Descending) }
                            }
                        }
                    }
                }
            }
        });

        var mapOrder = new[] { "1", "3", "2" };

        resultMaps = resultMaps.Where(m => mapOrder.Contains(m.Id));

        Assert.Equal(3, resultMaps.Count());
        Assert.Equal(mapOrder, resultMaps.Select(x => x.Id));
    }

    [Fact]
    public async Task ArrangeDynamicPlaylist_OneFailingPlaylist_StillArrangesOtherPlaylist()
    {
        var playlistMock1 = new Mock<IPlaylist>();

        object customData1 = new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), $"{nameof(DynamicPlaylistMap.Score)}.{nameof(DynamicPlaylistScore.TimeSet)}" },
                                { nameof(FilterOperation.Value), "99-99-9999 99:99:99" }, // Invalid DateTime
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        };

        playlistMock1
            .Setup(x => x.TryGetCustomData("mapMaven", out customData1))
            .Returns(true);

        playlistMock1.SetupGet(x => x.Title).Returns("Playlist 1");

        var playlistMock2 = new Mock<IPlaylist>();

        object customData2 = new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { nameof(DynamicPlaylistConfiguration.MapPool), nameof(MapPool.Standard) },
                    { nameof(DynamicPlaylistConfiguration.MapCount), 100 },
                    {
                        nameof(DynamicPlaylistConfiguration.FilterOperations), new JArray
                        {
                            new JObject
                            {
                                { nameof(FilterOperation.Field), nameof(DynamicPlaylistMap.Played) },
                                { nameof(FilterOperation.Value), true },
                                { nameof(FilterOperation.Operator), nameof(FilterOperator.Equals) }
                            }
                        }
                    }
                }
            }
        };

        playlistMock2
            .Setup(x => x.TryGetCustomData("mapMaven", out customData2))
            .Returns(true);

        playlistMock2.SetupGet(x => x.Title).Returns("Playlist 2");

        SetupMocksAndData(new[] { playlistMock1, playlistMock2 });

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) =>
            {
                if (playlist.Title == "Playlist 2")
                    resultMaps = maps;
            });

        await _sut.ArrangeDynamicPlaylists();

        _loggerMock.Verify(logger => logger.Log(
            It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
            It.Is<EventId>(eventId => eventId.Id == 0),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once
        );

        Assert.Equal(2, resultMaps.Count());
        Assert.Contains(resultMaps, x => x.Id == "1");
        Assert.Contains(resultMaps, x => x.Id == "3");
    }

    private async Task<IEnumerable<Map>> CallArrangeDynamicPlaylistWith(object customData)
    {
        var playlistMock = new Mock<IPlaylist>();

        playlistMock
            .Setup(x => x.TryGetCustomData("mapMaven", out customData))
            .Returns(true);

        SetupMocksAndData(playlistMock);

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        return resultMaps;
    }

    private void SetupMocksAndData(Mock<IPlaylist> playlistMock)
    {
        SetupMocksAndData(new[] { playlistMock });
    }

    private void SetupMocksAndData(Mock<IPlaylist>[] playlistMocks)
    {
        _beatSaberDataServiceMock
            .Setup(x => x.GetAllPlaylists())
            .ReturnsAsync(playlistMocks.Select(m => m.Object));

        _mapServiceMock
            .SetupGet(x => x.CompleteMapData)
            .Returns(Observable.Return(MapTestData.Maps));

        _mapServiceMock
            .SetupGet(x => x.CompleteRankedMapData)
            .Returns(Observable.Return(new Map[]
            {
                new Map
                {
                    Id = "102",
                }
            }));
    }
}
