namespace Stramp.Core.Playback;

/// <summary>
/// Formats listening duration into user-friendly strings (e.g. "45s", "12m 30s", "3h 15m", "2d 5h 20m").
/// </summary>
public static class ListeningDurationFormatter
{
    public static string Format(double totalSeconds)
    {
        if (totalSeconds < 0)
            totalSeconds = 0;

        var span = TimeSpan.FromSeconds(totalSeconds);
        var days = (int)span.TotalDays;
        var hours = span.Hours;
        var minutes = span.Minutes;
        var seconds = span.Seconds;

        if (days > 0)
            return $"{days}d {hours}h {minutes}m";
        if (hours > 0)
            return $"{hours}h {minutes}m";
        if (minutes > 0)
            return $"{minutes}m {seconds}s";
        return $"{seconds}s";
    }

    public static string FormatTooltip(double totalSeconds, string? prefix = null)
    {
        if (totalSeconds < 0)
            totalSeconds = 0;

        var span = TimeSpan.FromSeconds(totalSeconds);
        var totalHours = (int)span.TotalHours;
        var minutes = span.Minutes;
        var seconds = span.Seconds;

        string detail;
        if (totalHours > 0)
            detail = $"{totalHours:N0} hr{(totalHours == 1 ? "" : "s")} {minutes} min{(minutes == 1 ? "" : "s")}";
        else if (minutes > 0)
            detail = $"{minutes} min{(minutes == 1 ? "" : "s")} {seconds} sec{(seconds == 1 ? "" : "s")}";
        else
            detail = $"{seconds} second{(seconds == 1 ? "" : "s")}";

        return string.IsNullOrEmpty(prefix) ? detail : $"{prefix}: {detail}";
    }
}
