using System.Text.Json;
using Stramp.Core.Playback;
using Stramp.Core.Settings;

namespace Stramp.Core.Tests;

public class ListeningStatsTests
{
    [Fact]
    public void AppSettings_DefaultListeningTime_IsZero()
    {
        var settings = new AppSettings();
        Assert.Equal(0, settings.TotalListenedSeconds);
        Assert.Empty(settings.MonthlyListenedSeconds);
        Assert.Equal(0, settings.GetCurrentMonthListenedSeconds());
    }

    [Fact]
    public void AddListeningTime_AccumulatesTotalAndMonthly()
    {
        var settings = new AppSettings();
        var date1 = new DateTime(2026, 9, 15);
        var date2 = new DateTime(2026, 9, 20);
        var dateNextMonth = new DateTime(2026, 10, 5);

        settings.AddListeningTime(100.5, date1);
        settings.AddListeningTime(200.0, date2);
        settings.AddListeningTime(50.0, dateNextMonth);

        Assert.Equal(350.5, settings.TotalListenedSeconds);
        Assert.Equal(300.5, settings.GetCurrentMonthListenedSeconds(date1));
        Assert.Equal(50.0, settings.GetCurrentMonthListenedSeconds(dateNextMonth));
    }

    [Fact]
    public void AppSettings_SerializesListeningTimeCorrectly()
    {
        var settings = new AppSettings();
        var date = new DateTime(2026, 9, 1);
        settings.AddListeningTime(1234.5, date);

        var json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(1234.5, deserialized.TotalListenedSeconds);
        Assert.Equal(1234.5, deserialized.GetCurrentMonthListenedSeconds(date));
    }

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(-5, "0s")]
    [InlineData(45, "45s")]
    [InlineData(60, "1m 0s")]
    [InlineData(125, "2m 5s")]
    [InlineData(3599, "59m 59s")]
    [InlineData(3600, "1h 0m")]
    [InlineData(3665, "1h 1m")]
    [InlineData(7200, "2h 0m")]
    [InlineData(86400, "1d 0h 0m")]
    [InlineData(90060, "1d 1h 1m")]
    public void ListeningDurationFormatter_FormatsCorrectly(double seconds, string expected)
    {
        var formatted = ListeningDurationFormatter.Format(seconds);
        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void ListeningDurationFormatter_FormatTooltip_FormatsCorrectly()
    {
        Assert.Equal("0 seconds", ListeningDurationFormatter.FormatTooltip(0));
        Assert.Equal("45 seconds", ListeningDurationFormatter.FormatTooltip(45));
        Assert.Equal("1 min 0 secs", ListeningDurationFormatter.FormatTooltip(60));
        Assert.Equal("1 hr 1 min", ListeningDurationFormatter.FormatTooltip(3665));
        Assert.Equal("Total: 25 hrs 0 mins", ListeningDurationFormatter.FormatTooltip(90000, "Total"));
    }

    [Fact]
    public void ListeningHistoryDatabase_TracksMultipleMonthsAndTracks()
    {
        var db = new ListeningHistoryDatabase();
        var date1 = new DateTime(2026, 7, 10);
        var date2 = new DateTime(2026, 8, 15);
        var date3 = new DateTime(2026, 9, 20);

        var song1 = new Models.Song { Path = "c:/music/a.mp3", Title = "Song A", Artist = "Artist 1" };
        var song2 = new Models.Song { Path = "c:/music/b.mp3", Title = "Song B", Artist = "Artist 2" };

        db.RecordTrackPlay(song1, date1);
        db.AddListeningTime(180, song1, date1);

        db.RecordTrackPlay(song2, date2);
        db.AddListeningTime(240, song2, date2);

        db.RecordTrackPlay(song1, date3);
        db.AddListeningTime(200, song1, date3);

        Assert.Equal(3, db.TotalTracksPlayed);
        Assert.Equal(620, db.TotalSecondsListened);
        Assert.Equal(3, db.Months.Count);

        Assert.True(db.Months.ContainsKey("2026-07"));
        Assert.True(db.Months.ContainsKey("2026-08"));
        Assert.True(db.Months.ContainsKey("2026-09"));

        Assert.Equal(180, db.Months["2026-07"].SecondsListened);
        Assert.Equal(240, db.Months["2026-08"].SecondsListened);
        Assert.Equal(200, db.Months["2026-09"].SecondsListened);

        Assert.Equal("July 2026", db.Months["2026-07"].DisplayName);
        Assert.Equal("August 2026", db.Months["2026-08"].DisplayName);
        Assert.Equal("September 2026", db.Months["2026-09"].DisplayName);

        Assert.Single(db.Months["2026-07"].TopTracks);
        Assert.Equal(180, db.Months["2026-07"].TopTracks["c:/music/a.mp3"].SecondsListened);
        Assert.Equal(1, db.Months["2026-07"].TopTracks["c:/music/a.mp3"].PlayCount);
    }

    [Fact]
    public void ListeningHistoryStore_RoundtripSavesAndLoads()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"listening-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new ListeningHistoryStore(tempFile);
            var db = store.Load();
            Assert.Empty(db.Months);

            var song = new Models.Song { Path = "c:/music/test.mp3", Title = "Test Title", Artist = "Test Artist" };
            db.RecordTrackPlay(song, new DateTime(2026, 8, 1));
            db.AddListeningTime(300, song, new DateTime(2026, 8, 1));
            store.Save(db);

            var loadedStore = new ListeningHistoryStore(tempFile);
            var loaded = loadedStore.Load();

            Assert.Equal(1, loaded.TotalTracksPlayed);
            Assert.Equal(300, loaded.TotalSecondsListened);
            Assert.Single(loaded.Months);
            Assert.True(loaded.Months.ContainsKey("2026-08"));
            Assert.Equal("August 2026", loaded.Months["2026-08"].DisplayName);
            Assert.Equal(300, loaded.Months["2026-08"].SecondsListened);
            Assert.Equal("5m 0s", loaded.Months["2026-08"].FormattedTime);
            Assert.Equal(1, loaded.Months["2026-08"].TopTracks["c:/music/test.mp3"].PlayCount);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
