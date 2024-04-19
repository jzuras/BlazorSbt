using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Radzen;

namespace BlazorSbt.Shared;

public partial class RadzenDatagrid
{
    // todo - magic strings in code should be replaced with const values

    Radzen.DataGridGridLines GridLines = Radzen.DataGridGridLines.Both;

    [Inject]
    private IDbContextFactory<Data.DivisionContext> DbFactory { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Parameter]
    public string Organization { get; set; } = "";

    [Parameter]
    public string Id { get; set; } = "";

    [Parameter]
    public string TeamName { get; set; } = "";

    private string SelectedTeamName { get; set; } = "All Teams";

    private List<Schedule> Schedules = default!;
    private IEnumerable<Standings> Standings = default!;

    // TODO !!!
    // maybe try using record types for data model???
    // see link below - does not have to be immutable:
    // https://www.c-sharpcorner.com/article/c-sharp-9-record-types-immutable-value-types-syntax-usage/
    // also Avoid using DbSet directly in controllers (use DTOs instead), from:
    // see link below and also note use of Dispose methods:
    // https://www.c-sharpcorner.com/article/a-comprehensive-guide-to-entity-framework-core-in-net-8/

    protected override async Task OnInitializedAsync()
    {
        // save: this telerik page has an example of how to handle server render then WASM render for data pulls:
        // https://www.telerik.com/blogs/fetching-sharing-data-between-components-blazor-auto-render-mode
        // note - it includes a "simpler" option to have data always be pulled in a host/server component

        // todo - check that division exists

        await base.OnInitializedAsync();

        var context = DbFactory.CreateDbContext();

        this.Standings = context.Divisions
            .Include(d => d.Standings)
            .Where(d => d.Organization.ToLower() == this.Organization.ToLower()
                && d.Abbreviation.ToLower() == this.Id.ToLower())
            .FirstOrDefault()!.Standings;
            //context.Divisions.Standings.Where(s => s.Organization == Organization && s.Abbreviation == Id);

        // schedule for all teams
        if (HttpContext is null)
        {
            // render after pre-render, so load entire data set
            this.Schedules = this.GetSchedules(onlySubset: false);
        }
        else
        {
            // two ways to do this during pre-render:
            // 1) load a small subset which is enough to require a scroll bar,
            // which will result in a more subtle refresh upon render.
            // 2) load an empty data set which will show the "loading..." text

            //this.Schedules = this.GetSchedules(onlySubset: true);
            this.Schedules = new List<Schedule>();
        }

        if (string.IsNullOrWhiteSpace(this.TeamName) || this.TeamName.ToUpper() == "ALL TEAMS")
        {
            this.SelectedTeamName = "All Teams";
        }
        else
        {
            this.SelectedTeamName = this.TeamName;
        }
    }

    // ****************************
    // todo - lower case team names if typed by hand cause selection list selected item to be blank
    // ****************************
    // ****************************
    // todo - do anything for standings? or leave it alone since it shows blazor grid?
    // ****************************

    private List<Schedule> GetSchedules(bool onlySubset = false)
    {
        var maxRows = 50; // this was the max rows I could persist before things started to stop working
        var context = DbFactory.CreateDbContext();

        if (string.IsNullOrWhiteSpace(this.TeamName) ||
            this.TeamName.Equals("All Teams", StringComparison.CurrentCultureIgnoreCase))
        {
            if (onlySubset)
            {
                return context.Divisions
                    .Include(d => d.Schedule)
                    .Where(d => d.Organization.ToLower() == this.Organization.ToLower()
                        && d.Abbreviation.ToLower() == this.Id.ToLower())
                    .FirstOrDefault()!.Schedule
                    .Take(maxRows)
                    .ToList();

                //return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id)
                //    .Take(maxRows)
                //    .ToList();
            }
            else
            {
                return context.Divisions
                    .Include(d => d.Schedule)
                    .Where(d => d.Organization.ToLower() == this.Organization.ToLower()
                        && d.Abbreviation.ToLower() == this.Id.ToLower())
                    .FirstOrDefault()!.Schedule
                    .ToList();

                //return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id)
                //    .ToList();
            }
        }
        else
        {
            if (onlySubset)
            {
                return context.Divisions
                    .Include(d => d.Schedule)
                    .Where(d => d.Organization.ToLower() == this.Organization.ToLower()
                        && d.Abbreviation.ToLower() == this.Id.ToLower())
                    .FirstOrDefault()!.Schedule
                    .Where(s => s.Home.ToLower() == this.TeamName.ToLower() || s.Visitor.ToLower() == this.TeamName.ToLower())
                    .Take(maxRows)
                    .ToList();

                //return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
                //    && (s.Home == this.TeamName || s.Visitor == this.TeamName))
                //        .Take(maxRows)
                //        .ToList();
            }
            else
            {
                return context.Divisions
                    .Include(d => d.Schedule)
                    .Where(d => d.Organization.ToLower() == this.Organization.ToLower()
                        && d.Abbreviation.ToLower() == this.Id.ToLower())
                    .FirstOrDefault()!.Schedule
                    .Where(s => s.Home.ToLower() == this.TeamName.ToLower() || s.Visitor.ToLower() == this.TeamName.ToLower())
                    .ToList();

                //return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
                //    && (s.Home == this.TeamName || s.Visitor == this.TeamName))
                //        .ToList();
            }
        }
    }

    private void SelectedTeamNameChanged()
    {
        string value = this.SelectedTeamName;

        // todo - All Teams should revert to original URL
        if (!string.IsNullOrEmpty(value))
        {
            var url = $"{Organization}/{Id}/{value}";
            NavigationManager.NavigateTo(url, forceLoad: false, replace: false);
        }
    }

    private void HeaderCellRender(DataGridCellRenderEventArgs<BlazorSbt.Shared.Schedule> args)
    {
        if (args.Column.Property == "Rescheduled")
        {
            args.Attributes.Add("colspan", 3);
        }
    }
    private Dictionary<string, object> GetCellStyle(BlazorSbt.Shared.Schedule data)
    {
        var attributes = new Dictionary<string, object>();

        if (data.Visitor.ToUpper().StartsWith("WEEK"))
        {
            attributes.Add("style", "color:red; text-decoration:underline; font-weight:bold");
        }

        return attributes;
    }

    private Dictionary<string, object> GetLinkAttributes(BlazorSbt.Shared.Schedule game)
    {
        var attributes = new Dictionary<string, object>();
        var url = $"{Organization}/{Id}/{game.GameID}";

        attributes.Add("href", url);

        return attributes;
    }
}
