using BeatSaberPlaylistsLib.Types;
using MapMaven.Core.Models.Data;
using MapMaven.Core.Models.DynamicPlaylists;
using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MapMaven.Models;
using Moq;
using Newtonsoft.Json.Linq;
using System.Reactive.Linq;
using Playlist = MapMaven.Models.Playlist;

public class DynamicPlaylistArrangementServiceTests
{
    private readonly Mock<IApplicationSettingService> _applicationSettingServiceMock = new Mock<IApplicationSettingService>();
    private readonly Mock<IPlaylistService> _playlistServiceMock = new Mock<IPlaylistService>();
    private readonly Mock<IBeatSaberDataService> _beatSaberDataServiceMock = new Mock<IBeatSaberDataService>();
    private readonly Mock<IScoreSaberService> _scoreSaberServiceMock = new Mock<IScoreSaberService>();
    private readonly Mock<IMapService> _mapServiceMock = new Mock<IMapService>();

    private readonly DynamicPlaylistArrangementService _sut;

    public DynamicPlaylistArrangementServiceTests()
    {
        _sut = new DynamicPlaylistArrangementService(
            _beatSaberDataServiceMock.Object,
            _mapServiceMock.Object,
            _playlistServiceMock.Object,
            _scoreSaberServiceMock.Object,
            _applicationSettingServiceMock.Object);
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
        var playlistMock = new Mock<IPlaylist>();


        object customData = new JObject
        {
            {
                "dynamicPlaylistConfiguration", new JObject
                {
                    { "MapPool", nameof(MapPool.Improvement) },
                    { "MapCount", 1 }
                }
            }
        };

        playlistMock
            .Setup(x => x.TryGetCustomData("mapMaven", out customData))
            .Returns(true);

        _beatSaberDataServiceMock
            .Setup(x => x.GetAllPlaylists())
            .ReturnsAsync(new List<IPlaylist>
            {
                playlistMock.Object
            });

        _mapServiceMock
            .SetupGet(x => x.CompleteMapData)
            .Returns(Observable.Return(new Map[]
            {
                new Map
                {
                    Id = "1",
                }
            }));

        _mapServiceMock
            .SetupGet(x => x.CompleteRankedMapData)
            .Returns(Observable.Return(new Map[]
            {
                new Map
                {
                    Id = "2",
                }
            }));

        var resultMaps = Enumerable.Empty<Map>();

        _playlistServiceMock
            .Setup(x => x.ReplaceMapsInPlaylist(It.IsAny<IEnumerable<Map>>(), It.IsAny<Playlist>(), It.IsAny<bool>()))
            .Callback((IEnumerable<Map> maps, Playlist playlist, bool loadPlaylist) => resultMaps = maps);

        await _sut.ArrangeDynamicPlaylists();

        Assert.Contains(resultMaps, x => x.Id == "2");
    }
}
