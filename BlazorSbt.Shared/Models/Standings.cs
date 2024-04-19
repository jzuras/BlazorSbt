using Microsoft.EntityFrameworkCore;

// Razor does not play well with nullable reference types,
// but this line will still allow for null derefernce warnings
#nullable disable annotations

// Standings table handles the cumulative records for the teams.

namespace BlazorSbt.Shared;

//[PrimaryKey(nameof(Organization), nameof(Division), nameof(Name))]
public class Standings
{
    public string Organization { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;
    
    public short TeamID { get; set; }

    public string Name { get; set; }

    public short Wins { get; set; }

    public short Losses { get; set; }

    public short Ties { get; set; }

    public short OvertimeLosses { get; set; }

    public float Percentage { get; set; }

    public float GB { get; set; }

    public short RunsScored { get; set; }

    public short RunsAgainst { get; set; }

    public short Forfeits { get; set; }

    public short ForfeitsCharged { get; set; }
}
