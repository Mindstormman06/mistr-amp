using Stramp.Core.Models;

namespace Stramp.Core.Playback;

public sealed class TrackListeningStat
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
    public double SecondsListened { get; set; }
    public string FormattedTime { get; set; } = "0s";
    public int PlayCount { get; set; }
}

public sealed class MonthlyListeningRecord
{
    public string Month { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public double SecondsListened { get; set; }
    public string FormattedTime { get; set; } = "0s";
    public int TrackPlays { get; set; }
    public Dictionary<string, TrackListeningStat> TopTracks { get; set; } = [];
}

/// <summary>
/// Persistent database tracking total and monthly listening metrics, including previous months history.
/// </summary>
public sealed class ListeningHistoryDatabase
{
    public int Version { get; set; } = 1;
    public double TotalSecondsListened { get; set; }
    public string FormattedTotalTime { get; set; } = "0s";
    public int TotalTracksPlayed { get; set; }
    public DateTime? LastListenedAtUtc { get; set; }
    public Dictionary<string, MonthlyListeningRecord> Months { get; set; } = [];

    public void AddListeningTime(double seconds, Song? song = null, DateTime? now = null)
    {
        if (seconds <= 0)
            return;

        var dt = now ?? DateTime.Now;
        TotalSecondsListened += seconds;
        FormattedTotalTime = ListeningDurationFormatter.Format(TotalSecondsListened);
        LastListenedAtUtc = DateTime.UtcNow;

        var monthKey = dt.ToString("yyyy-MM");
        if (!Months.TryGetValue(monthKey, out var record))
        {
            record = new MonthlyListeningRecord
            {
                Month = monthKey,
                DisplayName = dt.ToString("MMMM yyyy")
            };
            Months[monthKey] = record;
        }

        record.SecondsListened += seconds;
        record.DisplayName = dt.ToString("MMMM yyyy");
        record.FormattedTime = ListeningDurationFormatter.Format(record.SecondsListened);

        if (song is not null)
        {
            var trackKey = string.IsNullOrWhiteSpace(song.Path) ? $"{song.Artist} - {song.Title}" : song.Path;
            if (!record.TopTracks.TryGetValue(trackKey, out var trackStat))
            {
                trackStat = new TrackListeningStat
                {
                    Title = song.Title,
                    Artist = song.Artist,
                    Album = song.Album
                };
                record.TopTracks[trackKey] = trackStat;
            }

            trackStat.SecondsListened += seconds;
            trackStat.FormattedTime = ListeningDurationFormatter.Format(trackStat.SecondsListened);
        }
    }

    public void RecordTrackPlay(Song? song = null, DateTime? now = null)
    {
        var dt = now ?? DateTime.Now;
        TotalTracksPlayed++;
        LastListenedAtUtc = DateTime.UtcNow;

        var monthKey = dt.ToString("yyyy-MM");
        if (!Months.TryGetValue(monthKey, out var record))
        {
            record = new MonthlyListeningRecord
            {
                Month = monthKey,
                DisplayName = dt.ToString("MMMM yyyy")
            };
            Months[monthKey] = record;
        }

        record.TrackPlays++;

        if (song is not null)
        {
            var trackKey = string.IsNullOrWhiteSpace(song.Path) ? $"{song.Artist} - {song.Title}" : song.Path;
            if (!record.TopTracks.TryGetValue(trackKey, out var trackStat))
            {
                trackStat = new TrackListeningStat
                {
                    Title = song.Title,
                    Artist = song.Artist,
                    Album = song.Album
                };
                record.TopTracks[trackKey] = trackStat;
            }

            trackStat.PlayCount++;
        }
    }

    public double GetCurrentMonthSeconds(DateTime? now = null)
    {
        var key = (now ?? DateTime.Now).ToString("yyyy-MM");
        return Months.TryGetValue(key, out var record) ? record.SecondsListened : 0;
    }
}
