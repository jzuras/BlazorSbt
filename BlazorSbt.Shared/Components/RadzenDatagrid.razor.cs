using BlazorSbt.Shared.Models;
using BlazorSbt.Shared.Models.Requests;
using BlazorSbt.Shared.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace BlazorSbt.Shared.Components;

public partial class RadzenDatagrid : ComponentBaseWithLogging, IDisposable
{
    // todo - magic strings in code should be replaced with const values

    DataGridGridLines GridLines = DataGridGridLines.Both;

    [Inject]
    private IDivisionService Service { get; init; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter]
    public string Organization { get; set; } = "";

    [Parameter]
    public string Id { get; set; } = "";

    [Parameter]
    public string TeamName { get; set; } = "";

    private string PreviousSelectedTeamName { get; set; } = "All Teams";

    private List<Schedule> Schedules = default!;
    private IEnumerable<Standings> Standings = default!;

    protected override async Task OnParametersSetAsync()
    {
        if (base.RenderStateService.IsPreRender == false)
        {
            if (string.IsNullOrWhiteSpace(this.TeamName) || this.TeamName.ToUpper() == "ALL TEAMS")
            {
                this.TeamName = "All Teams";
                this.PreviousSelectedTeamName = "";
            }

            if (this.TeamName != this.PreviousSelectedTeamName)
            {
                this.PreviousSelectedTeamName = this.TeamName;

                await this.PopulateData();
            }
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        // need to init standings here to avoid null reference while rendering
        this.Standings = new List<Standings>();

        // this will show a "Loading" line in grid
        this.Schedules = new List<Schedule>();

        await base.OnInitializedAsync();
    }

    // ****************************
    // todo - if division does not exist, "Loading..." remains displayed
    // ****************************

    // ****************************
    // todo - lower case team names if typed by hand cause selection list selected item to be blank
    // ****************************


    private async Task PopulateData()
    {
        var request = new GetDivisionRequest
        {
            Organization = this.Organization,
            Abbreviation = this.Id
        };

        var response = await Service.GetDivision(request);

        if (response.Success == false || response.Division == null)
        {
            this.Standings = new List<Standings>();
            this.Schedules = new List<Schedule>();
        }
        else
        {
            Standings = response.Division.Standings.OrderBy(s => s.Name).ToList();

            if (string.IsNullOrWhiteSpace(this.TeamName) ||
                 this.TeamName.Equals("All Teams", StringComparison.CurrentCultureIgnoreCase))
            {
               this. Schedules = response.Division.Schedule.ToList();
            }
            else
            {
                this.Schedules = response.Division.Schedule
                    .Where(s => s.Home.ToLower() == this.TeamName.ToLower() ||
                         s.Visitor.ToLower() == this.TeamName.ToLower()).ToList();
            }
        }
    }

    private void SelectedTeamNameChanged()
    {
        string value = this.TeamName;

        // todo - "All Teams" should revert to original URL
        if (!string.IsNullOrEmpty(value))
        {
            var url = $"{Organization}/{Id}/{value}";
            NavigationManager.NavigateTo(url, forceLoad: false, replace: false);
        }
    }

    #region Grid Rendering Methods
    private void HeaderCellRender(DataGridCellRenderEventArgs<Schedule> args)
    {
        if (args.Column.Property == "Rescheduled")
        {
            args.Attributes.Add("colspan", 3);
        }
    }
    private Dictionary<string, object> GetCellStyle(Schedule data)
    {
        var attributes = new Dictionary<string, object>();

        if (data.Visitor.ToUpper().StartsWith("WEEK"))
        {
            attributes.Add("style", "color:red; text-decoration:underline; font-weight:bold");
        }

        return attributes;
    }

    private Dictionary<string, object> GetLinkAttributes(Schedule game)
    {
        // Creates a link to report scores.

        var attributes = new Dictionary<string, object>();
        var url = $"{Organization}/{Id}/{game.GameID}";

        attributes.Add("href", url);

        return attributes;
    }
    #endregion

    // Dispose Pattern:
    private bool Disposed { get; set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        if (disposing)
        {
            // no need to dispose of service as DI will do so
            // (in fact, doing so here can break stuff!)
            //this.Service.Dispose();
        }

        Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
