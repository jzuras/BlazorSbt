using BlazorSbt.Shared.Data.Repositories;
using BlazorSbt.Shared.Models;
using BlazorSbt.Shared.Models.Requests;
using System.Diagnostics;

namespace BlazorSbt.Shared.Services;

public class DivisionService : IDivisionService, IDisposable
{
    private record FirstAndLastGameDates(DateTime FirstGameDate, DateTime LastGameDate);

    private IDivisionRepository Repository { get; init; } = default!;

    public DivisionService(IDivisionRepository repository)
    {
        this.Repository = repository;
    }

    /// <summary>
    /// Queries the repository using the information in the request
    /// to determine if a division exists.
    /// </summary>
    /// <param name="request">Information about the division.</param>
    /// <returns>Response.Success is true if division exists, false otherwise.</returns>
    public async Task<DivisionExistsResponse> DivisionExists(DivisionExistsRequest request)
    {
        try
        {
            var exists = await this.Repository.DivisionExists(request.Organization, request.Abbreviation);

            return new DivisionExistsResponse
            {
                Success = exists,
            };
        }
        catch (Exception ex)
        {
            return new DivisionExistsResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    /// <summary>
    /// Creates the division after verifying that it does not already exist.
    /// </summary>
    /// <param name="request">Information about the division.</param>
    /// <returns>Response.Success is true if division was created, false otherwise.</returns>
    public async Task<CreateDivisionResponse> CreateDivision(CreateDivisionRequest request)
    {
        try
        {
            var exists = await this.Repository.DivisionExists(request.Organization, request.Abbreviation);

            if (!exists)
            {
                var division = new Division
                {
                    Organization = request.Organization,
                    Abbreviation = request.Abbreviation,
                    League = request.League,
                    NameOrNumber = request.NameOrNumber,
                    Updated = this.GetEasternTime(),
                };

                await this.Repository.SaveDivision(division, false, true);

                return new CreateDivisionResponse
                {
                    Success = true,
                };
            }
            else
            {
                return new CreateDivisionResponse
                {
                    Success = false,
                    Message = "Unable to create division because a division already exists with " +
                        "the Abbreviation '" + request.Abbreviation + "'."
                };
            }
        }
        catch (Exception ex)
        {
            return new CreateDivisionResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    /// <summary>
    /// Loads a schedule from the provided file.
    /// </summary>
    /// <param name="request">Information about the division to load and the file to use.</param>
    /// <returns>Response.Success is true if schedule was loaded, false otherwise.</returns>
    public async Task<LoadScheduleResponse> LoadScheduleFileAsync(LoadScheduleRequest request)
    {
        try
        {
            var divisionExists = await this.Repository.DivisionExists(request.Organization, request.Abbreviation);

            if (divisionExists)
            {
                using (var stream = request.ScheduleFile.OpenReadStream())
                {
                    var firstAndLastGameDates = await this.LoadScheduleFileAsync(stream,
                        request.Organization, request.Abbreviation, request.UsesDoubleHeaders);

                    return new LoadScheduleResponse
                    {
                        Success = true,
                        FirstGameDate = firstAndLastGameDates.FirstGameDate,
                        LastGameDate = firstAndLastGameDates.LastGameDate
                    };
                }
            }
            else
            {
                return new LoadScheduleResponse
                {
                    Success = false,
                    Message = "Unable to load schedule - division not found."
                };
            }
        }
        catch (Exception ex)
        {
            return new LoadScheduleResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    /// <summary>
    /// Returns a (possibly empty) list of Division for the given Organization.
    /// </summary>
    /// <param name="request">Information about the division list to load.</param>
    /// <returns>Response.Success is true if no errors occured, false otherwise.</returns>
    public async Task<GetDivisionListResponse> GetDivisionList(GetDivisionListRequest request)
    {
        try
        {
            var list = await this.Repository.GetDivisionList(request.Organization);

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

    /// <summary>
    /// Delete a division, if found.
    /// </summary>
    /// <param name="request">Information about the division to delete.</param>
    /// <returns>Response.Success is true if division was deleted, false for errors (including not found).</returns>
    public async Task<DeleteDivisionResponse> DeleteDivision(DeleteDivisionRequest request)
    {
        try
        {
            var division = await this.Repository.GetDivision(request.Organization, request.Abbreviation);

            if (division == null)
            {
                return new DeleteDivisionResponse
                {
                    Success = false,
                    Message = "Unable to delete division because no division exists with this Abbreviation."
                };
            }

            await this.Repository.SaveDivision(division, true, false);

            return new DeleteDivisionResponse
            {
                Success = true,
            };
        }
        catch (Exception ex)
        {
            return new DeleteDivisionResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    /// <summary>
    /// Returns a single division from the repository. (It will be null if not found.)
    /// </summary>
    /// <param name="request">Information about the division to retrieve.</param>
    /// <returns>Response.Success is true if no errors occured, false otherwise.</returns>
    public async Task<GetDivisionResponse> GetDivision(GetDivisionRequest request)
    {
        try
        {
            var division = await this.Repository.GetDivision(request.Organization, request.Abbreviation);

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

    /// <summary>
    /// Returns a list of all games (schedule object) on the same day and field
    /// as the game identified by the game ID in the request.
    /// </summary>
    /// <param name="request">Information about the games to retrieve.</param>
    /// <returns>Response.Success is true if no errors occured, false otherwise.</returns>
    public async Task<GetScoresResponse> GetGames(GetScoresRequest request)
    {
        try
        {
            var games = await this.Repository.GetGames(
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

    /// <summary>
    /// Updates the information for a division, such as League, Updated, etc.
    /// This will not update the associated Standings or Schedule.
    /// </summary>
    /// <param name="request">Information about the division to update.</param>
    /// <returns>Response.Success is true if the update was successful, false otherwise.</returns>
    public async Task<UpdateDivisionResponse> UpdateDivision(UpdateDivisionRequest request)
    {
        try
        {
            var division = await this.Repository.GetDivision(request.Organization, request.Abbreviation);

            if (division != null)
            {
                division.League = request.League;
                division.NameOrNumber = request.NameOrNumber;
                division.Locked = request.Locked;
                division.Updated = this.GetEasternTime();

                await this.Repository.SaveDivision(division, false, false);

                return new UpdateDivisionResponse
                {
                    Success = true,
                };
            }
            else
            {
                return new UpdateDivisionResponse
                {
                    Success = false,
                    Message = "Unable to update division: no division exists with this Abbreviation."
                };
            }
        }
        catch (Exception ex)
        {
            return new UpdateDivisionResponse
            {
                Success = false,
                Message = ex.Message,
            };
        }
    }

    /// <summary>
    /// Updates the division's schedule and standings to reflect the game results.
    /// The Updated value for the division is also changed.
    /// </summary>
    /// <param name="request">Information about the division and scores to update.</param>
    /// <returns>Response.Success is true if update was successful, false otherwise.</returns>
    public async Task<UpdateScoresResponse> SaveScores(UpdateScoresRequest request)
    {
        try
        {
            var division = await this.Repository.GetDivision(request.Organization, request.Abbreviation);

            if (division == null)
            {
                return new UpdateScoresResponse
                {
                    Success = false,
                    Message = "Unable to save scores: no division exists with this Abbreviation."
                };
            }

            this.ProcessScores(division, request.Scores);
            division.Updated = this.GetEasternTime();
            await this.Repository.SaveDivision(division, false, false);

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

    #region Processing Methods
    /// <summary>
    /// Updates the division's schedule and standings to reflect the game results.
    /// </summary>
    /// <param name="division">Division from the repository.</param>
    /// <param name="scores">One or more game results.</param>
    private void ProcessScores(Division division, IList<ScheduleSubsetForUpdateScoresRequest> scores)
    {
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
                }
            }

            this.ReCalcStandings(division);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Helper method to load a schedule from the file stream. This method handles the
    /// database-related areas, and calls another helper method to process the file.
    /// </summary>
    /// <param name="scheduleFileStream">FileStream to process.</param>
    /// <param name="organization">Part of primary key for the division to load.</param>
    /// <param name="abbreviation">Part of primary key for the division to load.</param>
    /// <param name="usesDoubleHeaders">True to double-up the games in the schedule.</param>
    /// <returns>A record with earlest and latest game dates.</returns>
    /// <exception cref="Exception">Division does not already exist or an error in processing.</exception>
    private async Task<FirstAndLastGameDates> LoadScheduleFileAsync(Stream scheduleFileStream,
        string organization, string abbreviation, bool usesDoubleHeaders)
    {
        string errorMessage;
        Division? division;

        // Out params for ProcessScheduleFile().
        DateTime firstGameDate;
        DateTime lastGameDate;
        List<Standings> standings;
        List<Schedule> schedule;

        try
        {
            division = await this.Repository.GetDivision(organization, abbreviation);

            if (division == null)
            {
                throw new Exception("Unable to load schedule - division not found.");
            }
        }
        catch (Exception)
        {
            throw;
        }

        if (this.ProcessScheduleFile(scheduleFileStream, usesDoubleHeaders,
            organization, abbreviation,
            out standings, out schedule, out firstGameDate, out lastGameDate, out errorMessage))
        {

            division.Schedule = schedule;
            division.Standings = standings;

            try
            {
                division.Updated = this.GetEasternTime();
                await this.Repository.SaveDivision(division, false, false);

            }
            catch (Exception)
            {
                throw;
            }
        }
        else
        {
            // Processing failed.
            throw new Exception(errorMessage);
        }

        return new FirstAndLastGameDates(firstGameDate, lastGameDate);
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

    /// <summary>
    /// Helper method to load a schedule from the file stream. This method handles the
    /// actual processing of the file, using out parameters to return the data.
    /// </summary>
    /// <param name="scheduleFileStream">FileStream to process.</param>
    /// <param name="usesDoubleHeaders">True to double-up the games in the schedule.</param>
    /// <param name="organization">Part of primary key for the division to load.</param>
    /// <param name="abbreviation">Part of primary key for the division to load.</param>
    /// <param name="standings">Standings for the division based on the team names in the file.</param>
    /// <param name="schedule">Schedule for the division based on the games in the file.</param>
    /// <param name="firstGameDate">Earliest game in the schedule file.</param>
    /// <param name="lastGameDate"><Latest game in the schedule file./param>
    /// <param name="errorMessage">Exceptions during provessing are mentioned here.</param>
    /// <returns>True if no issues during processing, false otherwise.</returns>
    private bool ProcessScheduleFile(Stream scheduleFileStream, bool usesDoubleHeaders,
        string organization, string abbreviation,
        out List<Standings> standings, out List<Schedule> schedule,
        out DateTime firstGameDate, out DateTime lastGameDate,
        out string errorMessage)
    {
        int gameID = 0;
        int lineNumber = 0;
        List<string> lines = new();

        standings = new List<Standings>();
        schedule = new List<Schedule>();
        firstGameDate = DateTime.MinValue;
        lastGameDate = DateTime.MinValue;

        try
        {
            // Note - expecting a properly formatted file since it is self-created,
            // solely for the purposes of populating demo data for the website.
            // Therefore no error-checking is done here - just wrapping in try-catch
            // and returning exceptions to the calling method.

            using (var reader = new StreamReader(scheduleFileStream))
            {
                while (reader.Peek() >= 0)
                    lines.Add(reader.ReadLine()!);
            }

            List<string> teams = new();
            short teamID = 1;

            // Skip first 4 lines which are simply for ease of reading the file.
            lineNumber = 4;

            // Nnext lines are teams - ended by blank line.
            // Team IDs are created here, starting at 1.
            while (lines[lineNumber].Length > 0)
            {
                teams.Add(lines[lineNumber].Trim());

                // Create standings row for each team.
                var standingsRow = new Standings
                {
                    Organization = organization,
                    Abbreviation = abbreviation,
                    Wins = 0,
                    Losses = 0,
                    Ties = 0,
                    OvertimeLosses = 0,
                    Percentage = 0,
                    GB = 0,
                    RunsAgainst = 0,
                    RunsScored = 0,
                    Forfeits = 0,
                    ForfeitsCharged = 0,
                    Name = lines[lineNumber].Trim(),
                    TeamID = teamID++
                };
                standings.Add(standingsRow);
                lineNumber++;
            }

            // The rest of file is the actual schedule, in this format:
            // Date,Day,Time,Home,Visitor,Field.
            for (int index = lineNumber + 1; index < lines.Count; index++)
            {
                string[] data = lines[index].Split(',');

                if (data[0].ToLower().StartsWith("week"))
                {
                    schedule.Add(this.AddWeekBoundary(data[0], gameID, organization, abbreviation));
                    gameID++;
                    continue;
                }
                DateTime gameDate = DateTime.Parse(data[0]);
                // Skipping value at [1] - not currently used in this version of the website.
                DateTime gameTime = DateTime.Parse(data[2]);
                short homeTeamID = short.Parse(data[3]);
                short visitorTeamID = short.Parse(data[4]);
                string field = data[5];

                // Create schedule row for each game.
                var scheduleRow = new Schedule
                {
                    Organization = organization,
                    Abbreviation = abbreviation,
                    GameID = gameID++,
                    Day = gameDate,
                    Field = field,
                    Home = teams[homeTeamID - 1],
                    HomeForfeit = false,
                    HomeID = homeTeamID,
                    Time = gameTime,
                    Visitor = teams[visitorTeamID - 1],
                    VisitorForfeit = false,
                    VisitorID = visitorTeamID,
                };
                schedule.Add(scheduleRow);

                if (usesDoubleHeaders)
                {
                    // Add a second game 90 minutes later, swapping home/visitor.
                    scheduleRow = new Schedule
                    {
                        Organization = organization,
                        Abbreviation = abbreviation,
                        GameID = gameID++,
                        Day = gameDate,
                        Field = field,
                        Home = teams[visitorTeamID - 1],
                        HomeForfeit = false,
                        HomeID = visitorTeamID,
                        Time = gameTime.AddMinutes(90),
                        Visitor = teams[homeTeamID - 1],
                        VisitorForfeit = false,
                        VisitorID = homeTeamID,
                    };
                    schedule.Add(scheduleRow);
                }

                // Keep track of first and last games so user can verify the entire schedule was loaded.
                if (index == lineNumber + 2)
                {
                    firstGameDate = gameDate;
                }
                else if (index == lines.Count - 1)
                {
                    lastGameDate = gameDate;
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = "Line number: " + lineNumber + " " + ex.Message;
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Gets UTC Now and converts it to Eastern Standard Time.
    /// </summary>
    /// <returns>Current EST.</returns>
    private DateTime GetEasternTime()
    {
        DateTime utcTime = DateTime.UtcNow;

        TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, easternTimeZone);
    }

    /// <summary>
    /// This creates a mostly empty "WEEK #" row to make it easier to show
    /// week boundaries when displaying the schedule.
    /// </summary>
    /// <param name="week">Week number.</param>
    /// <param name="gameID">Game ID for the week boundary row.</param>
    /// <param name="organization">Part of primary key for the division to load.</param>
    /// <param name="abbreviation">Part of primary key for the division to load.</param>
    /// <returns>Newly created Schedule record.</returns>
    private Schedule AddWeekBoundary(string week, int gameID, string organization, string abbreviation)
    {
        var scheduleRow = new Schedule
        {
            Organization = organization,
            Abbreviation = abbreviation,
            GameID = gameID,
            HomeForfeit = false,
            Visitor = week,
            VisitorForfeit = false,
        };

        return scheduleRow;
    }
    #endregion

    // Dispose Pattern:
    private bool Disposed { get; set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (this.Disposed)
            return;

        if (disposing)
        {
            Debug.WriteLine("*************** Service is disposing of Repository.");
            this.Repository.Dispose();
        }

        this.Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
