using BlazorSbt.Shared.Models;
using BlazorSbt.Shared.Models.Requests;
using System.Text;
using System.Text.Json;

namespace BlazorSbt.Shared.Services;

public class DivisionServiceForWasm : IDivisionService, IDisposable
{
    private HttpClient Client { get; init; }

    private string BasePath = "https://blazorsbt.azurewebsites.net/DivisionApi"; // running on azure
    //private string BasePath = "https://localhost:7145"; // running locally

    public DivisionServiceForWasm(HttpClient client)
    {
        this.Client = client ?? throw new ArgumentNullException(nameof(client));
    }
    
    public Task<CreateDivisionResponse> CreateDivision(CreateDivisionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<DeleteDivisionResponse> DeleteDivision(DeleteDivisionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<DivisionExistsResponse> DivisionExists(DivisionExistsRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<GetDivisionResponse> GetDivision(GetDivisionRequest request)
    {
        try
        {
            var division = await this.RepositoryGetDivision(request.Organization, request.Abbreviation);

            return new GetDivisionResponse
            {
                Success = true,
                Division = division
            };
        }
        catch (Exception ex)
        {
            return new GetDivisionResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    public async Task<GetDivisionListResponse> GetDivisionList(GetDivisionListRequest request)
    {
        try
        {
            var list = await this.RepositoryGetDivisionList(request.Organization);

            return new GetDivisionListResponse
            {
                Success = true,
                DivisionList = list
            };
        }
        catch (Exception ex)
        {
            return new GetDivisionListResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    public async Task<GetScoresResponse> GetGames(GetScoresRequest request)
    {
        try
        {
            var games = await this.RepositoryGetGames(
                request.Organization, request.Abbreviation, request.GameID);

            return new GetScoresResponse
            {
                Success = true,
                Games = games
            };
        }
        catch (Exception ex)
        {
            return new GetScoresResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    public Task<LoadScheduleResponse> LoadScheduleFileAsync(LoadScheduleRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<UpdateScoresResponse> SaveScores(UpdateScoresRequest request)
    {
        // because this is using an API as a repository, it is slightly
        // different than the DivisionService implementation.
        // saving scores separately to avoid sending 100 rows 
        // across the wire when only 1 or 2 have changed.

        try
        {
            var division = await this.RepositoryGetDivision(request.Organization, request.Abbreviation);

            if (division == null)
            {
                return new UpdateScoresResponse
                {
                    Success = false,
                    Message = "Unable to save scores: no division exists with this Abbreviation."
                };
            }

            var list = this.ProcessScores(division, request.Scores);
            division.Updated = this.GetEasternTime();
            await this.RepositorySaveDivisionAndStandings(division);
            await this.RepositorySaveScores(list);

            return new UpdateScoresResponse
            {
                Success = true,
                Message = $"Successfully updated \"{request.Abbreviation}\"",
            };
        }
        catch (Exception ex)
        {
            return new UpdateScoresResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    public Task<UpdateDivisionResponse> UpdateDivision(UpdateDivisionRequest request)
    {
        // todo - because this is using an API as a repository, it is slightly
        // different than the DivisionService implementation.
    
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // todo - anything need disposal in this class?
        //throw new NotImplementedException();
    }

    #region Private Repository Mtehods
    private async Task<IList<Schedule>> RepositoryGetGames(string organization, string abbreviation, int gameID)
    {
        var response = await this.Client.GetAsync($"{this.BasePath}/{organization}/{abbreviation}/{gameID}");

        var games = await response.ReadJsonContentAsync<List<Schedule>>();

        if (games != null)
            return games!;
        else
            return new List<Schedule>();
    }

    private async Task<Division> RepositoryGetDivision(string organization, string abbreviation)
    {
        var response = await this.Client.GetAsync(this.BasePath + "/" + organization + "/" + abbreviation);
    
        var division = await response.ReadJsonContentAsync<Division>();

        if (division != null)
            return division!;
        else
            return new Division();
    }

    private async Task<List<Division>> RepositoryGetDivisionList(string organization)
    {
        var response = await this.Client.GetAsync(this.BasePath + "/" + organization);

        var list = await response.ReadJsonContentAsync<List<Division>>();

        if (list != null)
            return list!;
        else
            return new List<Division>();
    }

    private async Task<bool> RepositorySaveScores(IList<Schedule> schedules)
    {
        if (schedules.Count == 0)
        {
            // we could flag as an error if we wanted to...
            return true;
        }

        try
        {
            string jsonString = JsonSerializer.Serialize(schedules);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await this.Client.PutAsync(
                BasePath + "/" + schedules[0].Organization + "/" + schedules[0].Abbreviation + "/schedule",
                content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<bool> RepositorySaveDivisionAndStandings(Division division)
    {
        try
        {
            division.Schedule.Clear();
            string jsonString = JsonSerializer.Serialize(division);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            var response = await this.Client.PutAsync(
                BasePath + "/" + division.Organization + "/" + division.Abbreviation + "/standings",
                content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Updates the division's schedule and standings to reflect the game results.
    /// </summary>
    /// <param name="division">Division from the repository.</param>
    /// <param name="scores">One or more game results.</param>
    private List<Schedule> ProcessScores(Division division, IList<ScheduleSubsetForUpdateScoresRequest> scores)
    {
        // added to this version from SbtMulti - keeping track of the list of items being changed...
        var list  = new List<Schedule>();

        try
        {
            for (int i = 0; i < scores.Count; i++)
            {
                // Find matching game ID.
                var gameToUpdate = division.Schedule.FirstOrDefault(s => s.GameID == scores[i].GameID);

                if (gameToUpdate != null)
                {
                    gameToUpdate.HomeForfeit = scores[i].HomeForfeit;
                    gameToUpdate.HomeScore = scores[i].HomeScore;
                    gameToUpdate.VisitorForfeit = scores[i].VisitorForfeit;
                    gameToUpdate.VisitorScore = scores[i].VisitorScore;
                    list.Add(gameToUpdate);
                }
            }

            this.ReCalcStandings(division);

            return list;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Re-calculates the standings for a division, to reflect changes in scores.
    /// </summary>
    /// <param name="division">Division to update.</param>
    private void ReCalcStandings(Division division)
    {
        var standings = division.Standings;

        var schedule = division.Schedule;

        // Zero-out standings before calculations.
        foreach (var stand in standings)
        {
            stand.Forfeits = stand.Losses = stand.OvertimeLosses = stand.Ties = stand.Wins = 0;
            stand.RunsAgainst = stand.RunsScored = stand.ForfeitsCharged = 0;
            stand.GB = stand.Percentage = 0;
        }

        foreach (var sched in schedule)
        {
            // Skip week boundary.
            if (sched.Visitor.ToUpper().StartsWith("WEEK") == true) continue;

            this.UpdateStandings(standings, sched);
        }

        this.CalculateGamesBehind(standings);
    }

    /// <summary>
    /// Updates team information in standings for a specific game result.
    /// (Except for Games Behind which can be calculated after 
    /// calling this method for each game in the schedule.)
    /// </summary>
    /// <param name="standings">Standings records for the division.</param>
    /// <param name="sched">A row from the schedule (which includes the game result).</param>
    private void UpdateStandings(List<Standings> standings, Schedule sched)
    {
        // Note - IList starts at 0, team IDs start at 1.
        var homeTeam = standings[sched.HomeID - 1];
        var visitorTeam = standings[sched.VisitorID - 1];

        // This will catch null values (no scores reported yet).
        if (sched.HomeScore > -1)
        {
            homeTeam.RunsScored += (short)sched.HomeScore!;
            homeTeam.RunsAgainst += (short)sched.VisitorScore!;
            visitorTeam.RunsScored += (short)sched.VisitorScore!;
            visitorTeam.RunsAgainst += (short)sched.HomeScore!;
        }

        if (sched.HomeForfeit)
        {
            homeTeam.Forfeits++;
            homeTeam.ForfeitsCharged++;
        }
        if (sched.VisitorForfeit)
        {
            visitorTeam.Forfeits++;
            visitorTeam.ForfeitsCharged++;
        }

        if (sched.VisitorForfeit && sched.HomeForfeit)
        {
            // Special case - not a tie - double-forfeit is counted as a loss for both teams.
            homeTeam.Losses++;
            visitorTeam.Losses++;
        }
        else if (sched.HomeScore > sched.VisitorScore)
        {
            homeTeam.Wins++;
            visitorTeam.Losses++;
        }
        else if (sched.HomeScore < sched.VisitorScore)
        {
            homeTeam.Losses++;
            visitorTeam.Wins++;
        }
        else if (sched.HomeScore > -1)
        {
            homeTeam.Ties++;
            visitorTeam.Ties++;
        }
    }

    /// <summary>
    /// Calculates Games Behind for each team in the standings.
    /// </summary>
    /// <param name="standings">Standings records for the division.</param>
    private void CalculateGamesBehind(List<Standings> standings)
    {
        // Calculate Games Behind (GB).
        var sortedTeams = standings.OrderByDescending(t => t.Wins).ToList();
        var maxWins = sortedTeams.First().Wins;
        var maxLosses = sortedTeams.First().Losses;
        foreach (var team in sortedTeams)
        {
            team.GB = ((maxWins - team.Wins) + (team.Losses - maxLosses)) / 2.0f;
            if ((team.Wins + team.Losses) == 0)
            {
                team.Percentage = 0.0f;
            }
            else
            {
                team.Percentage = (float)team.Wins / (team.Wins + team.Losses + team.Ties);
            }
        }
    }

    private DateTime GetEasternTime()
    {
        DateTime utcTime = DateTime.UtcNow;

        //TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, easternTimeZone);
    }
    #endregion
}

internal static class HttpClientExtensions
{
    public static async Task<T> ReadJsonContentAsync<T>(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode == false)
            throw new ApplicationException($"Something went wrong calling the API: {response.ReasonPhrase}");

        var dataAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return JsonSerializer.Deserialize<T>(
            dataAsString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
    }
}