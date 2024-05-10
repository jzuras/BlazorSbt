// Schedules table handles the day/time/field (original and possible make-ups) for a game.
// This is also where scores are kept. Home and Visitor are string names of the teams,
// while the corresponding ID's are the values in the Standings table.
// There is also a ViewModel class here, used for reporting scores.

namespace BlazorSbt.Shared.Models;

/// <summary>
/// Schedules table contains the day/time/field (original and possible make-ups) for a game.
/// This is also where scores are recorded. Home and Visitor are string names of the teams,
/// while the corresponding ID's refer to the Standings table.
/// </summary>
public class Schedule
{
    public string Organization { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;

    public int GameID { get; set; }

    public string Home { get; set; } = string.Empty;

    public string Visitor { get; set; } = string.Empty;

    public DateTime? Day { get; set; }

    public DateTime? Time { get; set; }

    public string Field { get; set; } = string.Empty;

    public short HomeID { get; set; }

    public short VisitorID { get; set; }

    public short? HomeScore { get; set; }

    public short? VisitorScore { get; set; }

    public bool HomeForfeit { get; set; }

    public bool VisitorForfeit { get; set; }

    public bool OvertimeGame { get; set; }

    public DateTime? MakeupDay { get; set; }

    public DateTime? MakeupTime { get; set; }

    public string? MakeupField { get; set; }
}
