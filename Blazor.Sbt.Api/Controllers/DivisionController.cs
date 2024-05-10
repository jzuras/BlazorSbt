using BlazorSbt.Shared.Data;
using BlazorSbt.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.IO;

namespace BlazorSbt.Api.Controllers;

[ApiController]
[Route("/")]
public class DivisionController : ControllerBase, IDisposable
{
    protected DivisionContext DbContext { get; init; } = default!;

    private readonly ILogger<DivisionController> Logger;

    public DivisionController(DivisionContext context,
        ILogger<DivisionController> logger)
    {
        this.DbContext = context;
        this.Logger = logger;
    }

    // GET: /
    [HttpGet("{organization}")]
    public ActionResult<IEnumerable<Division>> GetDivisionList(string organization)
    {
        var list = this.DbContext.Divisions
                .Where(d => d.Organization.ToLower() == organization.ToLower());

        return Ok(list);
    }

    // GET: /
    [HttpGet("{organization}/{abbreviation}")]
    public ActionResult<Division> GetDivision(string organization, string abbreviation)
    {
        var division = this.GetDivisionHelperMethod(organization, abbreviation);

        return Ok(division);
    }

    // GET: /
    [HttpGet("{organization}/{abbreviation}/{gameId:int}")]
    public ActionResult<List<Schedule>> GetGames(string organization, string abbreviation, int gameID)
    {
        var list = new List<Schedule>();

        try
        {
            // Step 1: do a query returning 1 game based on the game id.
            // Step 2: do a second query using that game's day and field.

            // var schedule = await this.Divisions
            //    .Where(d => d.Organization == organization && d.ID == divisionID.ToLower())
            //    .SelectMany(d => d.Schedule)
            //    .Where(s => s.GameID == gameID)
            //    .FirstOrDefaultAsync();

            // EF Core could not handle the query above (threw exception about inability to translate)
            // so now we just get the entire division and query the schedule list directly.

            var division = this.GetDivisionHelperMethod(organization, abbreviation);

            var games = division!.Schedule
                .Where(s => s.GameID == gameID)
                .SelectMany(s => division.Schedule.Where(inner => inner.Day == s.Day && inner.Field == s.Field))
                .ToList();

            if (games != null)
            {
                foreach (var game in games)
                {
                    list.Add(game);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }

        return Ok(list);
    }

    // PUT: /{organization}/{abbreviation}
    [HttpPut("{organization}/{abbreviation}/standings")]
    public async Task<IActionResult> UpdateDivsionAndStandings(string organization, string abbreviation, Division division)
    {
        if (division == null || division.Standings.Count == 0)
        {
            return BadRequest("Invalid division object.");
        }

        if (division.Organization != organization || division.Abbreviation != abbreviation)
        {
            return BadRequest();
        }

        this.DbContext.Entry(division).State = EntityState.Modified;

        // Mark standings as modified
        foreach (var standing in division.Standings)
        {
            this.DbContext.Entry(standing).State = EntityState.Modified;
        }

        try
        {
            await this.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        return NoContent();
    }

    // PUT: /{organization}/{abbreviation}
    [HttpPut("{organization}/{abbreviation}/schedule")]
    public async Task<IActionResult> UpdateSchedule(string organization, string abbreviation, List<Schedule> schedules)
    {
        if (schedules == null || schedules.Count == 0)
        {
            return BadRequest("Invalid standings object.");
        }

        if (schedules[0].Organization != organization || schedules[0].Abbreviation != abbreviation)
        {
            return BadRequest();
        }

        // Mark standings as modified
        foreach (var schedule in schedules)
        {
            this.DbContext.Entry(schedule).State = EntityState.Modified;
        }

        try
        {
            await this.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }

        return NoContent();
    }

    // PUT: /{organization}/{abbreviation}
    [HttpPut("{organization}/{abbreviation}")]
    public async Task<IActionResult> UpdateDivision(string organization, string abbreviation, Division division)
    {
        if (division == null)
        {
            return BadRequest("Invalid division object.");
        }

        if (division.Organization != organization || division.Abbreviation != abbreviation)
        {
            return BadRequest();
        }

        this.DbContext.Entry(division).State = EntityState.Modified;


        // Mark related entities as modified
        // it is fine to do this for standings, since they are probably all changed anyway,
        // but doing this for each game in the schedule is overkill.
        // not sure how to fix - even if we are passed in the list of games that changed,
        // we would want to avoid the loop.
        //foreach (var standing in division.Standings)
        //{
        //    this.DbContext.Entry(standing).State = EntityState.Modified;
        //}

        //foreach (var scheduleItem in division.Schedule)
        //{
        //    this.DbContext.Entry(scheduleItem).State = EntityState.Modified;
        //}

        try
        {
            await this.DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
            //if (!StreamExists(name))
            //{
            //    return NotFound();
            //}
            //else
            //{
            //    throw;
            //}
        }

        return NoContent();
    }

    #region Private Helper Methods
    private Division? GetDivisionHelperMethod(string organization, string abbreviation)
    {
        var division = this.DbContext.Divisions
            .Include(d => d.Schedule)
            .Include(d => d.Standings)
            .Where(d => d.Organization.ToLower() == organization.ToLower()
                && d.Abbreviation.ToLower() == abbreviation.ToLower())
            .FirstOrDefault();

        return division;
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
            Debug.WriteLine("*************** API is disposing of DbContext.");
            this.DbContext.Dispose();
        }

        this.Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
