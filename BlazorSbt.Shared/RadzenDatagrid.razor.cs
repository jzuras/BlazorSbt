using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Diagnostics;

namespace BlazorSbt.Shared;

public partial class RadzenDatagrid : IDisposable
{
    // todo - magic strings in code should be replaced with const values

    Radzen.DataGridGridLines GridLines = Radzen.DataGridGridLines.Both;

    // todo - do all of these need to be public???

    [Inject]
    public IDbContextFactory<Data.DemoContext> DbFactory { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public PersistentComponentState ApplicationState { get; set; } = default!;

    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    [Parameter]
    public string Organization { get; set; } = "";

    [Parameter]
    public string Id { get; set; } = "";

    [Parameter]
    public string TeamName { get; set; } = "";

    public string SelectedTeamName { get; set; } = "All Teams";

    private List<Schedules> Schedules = default!;
    private IEnumerable<Standings> Standings = default!;
    private PersistingComponentStateSubscription Subscription;
    private string Key = string.Empty;

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


        // end of day 4/16 summary regarding init method being called twice:
        // - this is true if prerender is true in top line of razor code:
        // @rendermode @(new InteractiveServerRenderMode(prerender: true))
        // this causes the db to be hit twice, and a flash for the user on the page
        // i found a simple cache method to handle this, so the db data is stored
        // during the first call and pulled from cache the second time.
        // (i am not quite sure why this prevents a flash, since i think 
        // the data is still loaded twice into the UI) update - I think the
        // lack of flash is because Blazor does a partial DOM update and only
        // where the DOM is different, but since the data is the same then the DOM
        // is the same, so no update and no flash.

        // i later found an article with perhaps a cleaner way, still TBD

        // end of day 4/16 summary regarding back button issue:
        // once i got the selection list working to sort by team schedule,
        // i found the back button issue still there. when i solved by
        // navigating to a URL, i lost the partial refresh as it did a full page load
        // there is a navigation lock concept that maybe could be used to save a state
        // and perhaps reload it (or maybe the persistant state article will help),
        // but that seems too complex.
        // i stopped with the solution that uses this line:
        // NavigationManager.NavigateTo(url, forceLoad: false, replace: true);
        // this also seems to add a URL but only the last one is saved,
        // so when user comes back it is only for one page using the most recent value,
        // no matter how many diff teams were selected and seemingly added to URL history.
        // not ideal but at least consistent, so moving on at least for now...

        // 4/17 update:
        // I changed persistence mechanism to ApplicationState. This caused something to break,
        // but no exception was caught. The second call to the Init method never happened,
        // which then made interaction no longer work. The issue seemed to be the call
        // to ApplicationState.PersistAsJson(this.Key, this.Schedules) - it worked fine
        // if I made it an empty List but with actual data, it would break the init somwhow.
        // It may be a problem with Blazor's JSON persistence code combined with my data, so
        // a way to use custom persistence could fix the issue, but I did not try this.
        // Instead, I now init with an empty list, which shows "loading..." which is the text
        // set via the radzen grid, and then I override OnAfterRenderAsync() to load the db data.

        // 4/17 update 2:
        // changing team in selection box still (or now) flashes, may have something to do with
        // changes made today but when I tried to go back to updating it without a new URL,
        // I could not get the code to do a refresh anymore.
        // may want to use draft code I saved at start of day and see if i can make that
        // code do a refresh (close and re-open VS so I can just ctrl-z all the changes when done.


        // todo - take this entire mess into a draft with a useful title and then clear it all out here...
        // then check out hilton article and see if that is a better way to do this...

        // this method gets called twice - once for pre-render and again once the signal R connection is complete
        // (todo - search my open tabs and verify verbiage) (update - see john hilton article below)
        // so i implemented a simple in-memory cache to hit the db once for the 2 calls
        // also, make note of where it said somewhere that the twice calling was only true
        // if the app used a per-component rendering, meaning that app-wide rendering would not do so
        // (need to test this first) - update, i think that was only refering to setting prerender mode to false
        // for the entire app.
        // see https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#prerendering

        /*
         * this may be how to force entire app one way:
         * 
         * And in App.razor:
<HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
<Routes @rendermode="InteractiveServer" />
<script src="_framework/blazor.web.js"></script>
</body>

         * */

        // when not forcing entire app one way, seems i can set this line's render param to false
        // to get this only called once, though it is a noticable delay when loading page:
        // @rendermode @(new InteractiveServerRenderMode(prerender: true))
        // line above is at top of razor code for this component

        // UPDATE !!!
        // this looks like a good summary of everything and also includes a possible
        // fix using something in blazor - @inject PersistentComponentState ApplicationState
        // https://jonhilton.net/persist-state-between-renders-net8/

        Debug.WriteLine("**************************** RadzenDatagrid:OnInitializedAsync called.");

        await base.OnInitializedAsync();
        
        var context = DbFactory.CreateDbContext();

        // Register a callback to persist data just before rendering
        this.Subscription = ApplicationState.RegisterOnPersisting(this.Persist);
        //this.Schedules = this.GetSchedules();

        this.Standings = context.Standings.Where(s => s.Organization == Organization && s.Division == Id);
        //return;

        //if (string.IsNullOrWhiteSpace(this.TeamName) || this.TeamName.ToUpper() == "ALL TEAMS")
        //{
        //    this.Schedules = context.Schedules.Where(s => s.Organization == Organization && s.Division == Id);
        //}
        //else
        //{
        //    this.Schedules = context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
        //        && (s.Home == this.TeamName || s.Visitor == this.TeamName));
        //    this.SelectedTeamName = this.TeamName;
        //}

        // todo - cahce standings too

        // cache code
        //this.Schedules = this.PullScheduleFromCache();

        this.Key = $"{this.Organization}/{this.Id}/{this.TeamName ?? "AllTeams"}";
        
        // todo - how does the partial schedule for a single team affect this cahcing? remove it after use here???
        var foundInState = ApplicationState.TryTakeFromJson<List<Schedules>>(this.Key, out var schedules);

        // what if i pull from db at diff times for full vs teamname??
//        if (string.IsNullOrWhiteSpace(this.TeamName) == false)
        {
            //foundInState = (HttpContext is not null);
        }
//        else
        {
            // schedule for all teams
            if (HttpContext is null)
            {
                this.Schedules = this.GetSchedules(onlySubset: false);
            }
            else
            {
                //this.Schedules = this.GetSchedules(onlySubset: true);
                this.Schedules = new List<Schedules>();
            }
            //foundInState = (HttpContext is null);
        }


        if (string.IsNullOrWhiteSpace(this.TeamName) == false && foundInState == false)
        {
            // for some reason, persisted data is not found when the team name is included
            // (at least as a result of the code handling the selection change - user-typed URL is still TBD)
            // so we do not always want to pull from the db again just because it wasnt found
            // (the data is persisted simply to avoid flashing between pre-render and render)
            // the goal here is really to avoid two full db data calls during init
            // (the full db pull is done in OnAfterRenderAsync).
            // so to avoid a 2nd db call, even for partial data, we will try a different
            // test to determing if this is the 2nd init call - check HttpContext
            //if (HttpContext is null)
            {
                // this is the after pre-render call (2nd call), so avoid 2nd db call
                foundInState = (HttpContext is not null);
            }
        }

        //this.Schedules = foundInState ? schedules! : this.GetSchedules(onlySubset: true);
        //this.Schedules = this.GetSchedules(onlySubset:true);

        if (foundInState)
        {
            Debug.WriteLine($"************************ key {Key} found in state.");
        }
        else
        {
            Debug.WriteLine($"************************ key {Key} NOT found in state.");
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

    // todo - move standings population to after render too???

    private Task Persist()
    {
        //ApplicationState.PersistAsJson(this.Key, new List<Schedules>());
        //ApplicationState.PersistAsJson(this.Key, this.Schedules);
        Debug.WriteLine($"************************ Persisting key {Key}.");
        return Task.CompletedTask;
    }

    private async Task<List<Schedules>> GetSchedulesAsync()
    {
        var context = DbFactory.CreateDbContext();

        if (string.IsNullOrWhiteSpace(this.TeamName) ||
            this.TeamName.Equals("All Teams", StringComparison.CurrentCultureIgnoreCase))
        {
            return await context.Schedules.Where(s => s.Organization == Organization && s.Division == Id).ToListAsync();
        }
        else
        {
            return await context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
                && (s.Home == this.TeamName || s.Visitor == this.TeamName)).ToListAsync();
        }
    }

    // update EOD 4/17
    // everything works great when I do no persisting or after render stuff
    // (no flashing, team name change works with back button, href click) - only issue
    // there is 2 db hits. multiple attemtps to avoid that has failed:
    // 1) initially empty schedule shows a flash due to adding a scroll bar when all rows loaded
    // 2) can persist a max of 50 rows before interactivity fails, but at 50 scroll bar size changes
    // 3) had to add base call for after render to fix href click, but that still breaks
    // after team name change - there is also a delay in loading that, so need to
    // use breakpoints and see what is being called there to cause delay, maybe
    // that helps with why broken
    // note - still want to clear out app state when done - maybe that will help with href?
    // note2 - not sure why I need to persist those 50 rows but when i did not the href broke - WTF???
    // note3 - commented out code at bottom of OnAfterRenderAsync is how Claude.ai said to do
    // load 50 rows at prerender and full db at render, but that didn't look right
    // MAJOR NOTE - finally noticed in debug window that SQL statements kept flying by when nothing
    // was being done - looks like i have an infinite loop on page load, which is likely the href issue
    // and possibly many other things too!!!
    // Major note update - loop was due to missing "if firstrender", now everything seems to work
    // but i am hitting breakpoint in the 2 getschedule methods a total of 3 times when should
    // only be 2? once that is fixed see if I can get by without persisting, or persisting everything, also
    // still need to clear App State.
    // todo - re-read that telerik article about app state - why do i need to persist at all to avoid flash?
    // ideally would do db call first time and re-use html but not sure that is possible
    // maybe try again with empty "loading..." on pre-render (still get flash due to scroll bar??)

    // update 4/18 - before i tried persisting entire dataset again, i tested the current state of affairs.
    // although things work as desired, I noticed that when the team name is changed, I am getting an
    // extra DB call. it seems that the persisted data is not in fact found by the 2nd init call
    // (the data is slightly diff than the original full page load which works as expected),
    // thus 3 db calls.
    // i am going to try going back to seeing if i can determine within the init call
    // if i am in render or pre-render and do different things in each case.

    // update 2 for 4/18
    // Claude's idea for how to determine render vs non-render does not work so I am using HttpContext
    // here is where I ended up with the twin goals of no flashing and minimizing db calls:
    // sequence had to be different for full schedule vs partial schedule, not sure why.
    // for full schedule, needed to persist data (I dont think this is needed for partial, but
    // current code persists in both cases anyway, though it seems persistence fails for partial).
    // for partial, need to check httpcontext to know when to hit database.
    // for both cases, the 2nd init call now avoids a db call, and the full db call is done
    // in OnAfterRenderAsync(). (note that full call is likely same for partial due to size).

    // todo - is there a way to unpersists - remove or clear app state? maybe that is why
    // persist fails for partial sched???
    // update 3 for 4/18 (EOD) - did not check on above question about clearing app state
    // i decided that since i am using the check against HttpClient's existence (from Telerik article)
    // for the partial schedule, i might as well use it for everything if i can.
    // it turns out that i can, and doing it this way is much simpler, and does not need
    // persistence. (before posting LI article, re-read that telerik article to see what their simpler option was)
    // i first verifiied that loading 50 rows would work, then tried the empty list - both are still in code here.
    // now the idea is simply to load an empty schedule for pre-render, which will show "Loading..."
    // and then do a full db load on render. no longer need after render code either.
    // this way does show a flash, but the grid changes from 1 row (loading) to a full table
    // that needs scrollbars, which is fine, and it only hits the db once.
    // if user does not like this, can easily uncomment partial load line, hitting db twice.
    // note that the same code is used for loading single-team schedules, but i never see
    // "loading" even when i copy and paste that url, not sure why.

    // I have now copied this entire project into a new copy/backup directory, so
    // i can start removing all this extra code, and put all these comments into a draft for LI article
    // (keep todo stuff here though)

    // ****************************
    // todo - lower case team names if typed by hand cause selection list selected item to be blank
    // ****************************
    // ****************************
    // todo - do anything for standings? or leave it alone since it shows blazor grid?
    // ****************************

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // IMPORTANT!! - should this call be before or after my code???
            //await base.OnAfterRenderAsync(firstRender);

            // note to self - if i do not persist data then i get flashing doing this
            // also, team name change works but clicking on game is ignored!!!
            // (try commenting out all persistence to see if href starts working)
            //this.Schedules = this.GetSchedules(onlySubset: false);

            //this.Schedules = await this.GetSchedulesAsync();
            //this.StateHasChanged();

            // IMPORTANT!! - should this call be before or after my code???
            //await base.OnAfterRenderAsync(firstRender);

        }

        // IMPORTANT!! - should this call be before or after my code???
        // (need to call it for non-first render also)
        await base.OnAfterRenderAsync(firstRender);


        //if (firstRender)
        //{
        //    // Load a limited number of rows for prerendering
        //    //Schedules = await GetSchedules(maxRows: 50);
        //    Schedules = GetSchedules(onlySubset: true);
        //}
        //else
        //{
        //    // Load the full data set after initial render
        //    Schedules = await GetSchedulesAsync();
        //}
    }

    private List<Schedules> GetSchedules(bool onlySubset = false)
    {
        var maxRows = 50;
        var context = DbFactory.CreateDbContext();

        if (string.IsNullOrWhiteSpace(this.TeamName) ||
            this.TeamName.Equals("All Teams", StringComparison.CurrentCultureIgnoreCase))
        {
            // todo - may need to do this for teamname section too - try with saving and loading that full URL directly

            if (onlySubset)
            {
                return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id)
                    .Take(maxRows)
                    .ToList();
            }
            else
            {
                return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id)
                    .ToList();
            }
        }
        else
        {
            if (onlySubset)
            {
                return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
                    && (s.Home == this.TeamName || s.Visitor == this.TeamName))
                        .Take(maxRows)
                        .ToList();
            }
            else
            {
                return context.Schedules.Where(s => s.Organization == Organization && s.Division == Id
                    && (s.Home == this.TeamName || s.Visitor == this.TeamName))
                        .ToList();
            }
        }
    }

    //private IEnumerable<BlazorSbt.Shared.Schedules> PullScheduleFromCache()
    //{
    //    string key = $"{this.Organization}/{this.Id}/{this.TeamName ?? "AllTeams"}";

    //    IEnumerable<BlazorSbt.Shared.Schedules>? result = MemoryCache.GetOrCreate(key, entry =>
    //    {
    //        entry.SetOptions(new MemoryCacheEntryOptions
    //        {
    //            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
    //        });

    //        return GetSchedules();
    //    });

    //    return result ?? Enumerable.Empty<BlazorSbt.Shared.Schedules>();
    //}


    //  update 4/16 - got select element working with what seems to be the expected way to do this in blazor,
    // but also needed to have page use interactive mode
    // HOWEVER - current method is just like old one where it goes to a new URL, whereas for blazor 
    // we want to just update the table for a partial refresh (then issue becomes back button AGAIN!!)

    //<InputSelect value="SelectedTeamName" ValueChanged="@((string value) => OnValueChanged(value ))" ValueExpression="@(()=>SelectedTeamName)">
    //<select value="@SelectedTeamName" @onchange="HandleChange">

    //protected override async Task OnAfterRenderAsync(bool firstRender)
    //{
    //    if (firstRender)
    //    {
    //        this.Schedules = await this.GetSchedules();
    //        this.StateHasChanged();
    //    }
    //}

    private void SelectedTeamNameChanged()//string value)
    {
        // todo - make context a property??

        //this.TeamName = this.SelectedTeamName;
        //this.Schedules = this.GetSchedules().Result;
        ////this.Schedules = new List<Schedules>();
        //this.Standings = Enumerable.Empty<Standings>();
        //this.StateHasChanged();
        //return;

        string value = this.SelectedTeamName;

        // todo - All Teams should revery to original URL
        if (!string.IsNullOrEmpty(value))
        {
            var url = $"{Organization}/{Id}/{value}";
            NavigationManager.NavigateTo(url, forceLoad: false, replace: false);
        }
    }

    private void HeaderCellRender(DataGridCellRenderEventArgs<BlazorSbt.Shared.Schedules> args)
    {
        if (args.Column.Property == "Rescheduled")
        {
            args.Attributes.Add("colspan", 3);
        }
    }

    /*
    void CellRender(DataGridCellRenderEventArgs<BlazorSbt.Shared.Schedules> args)
    {
        Debug.WriteLine("in cell render, property is " + args.Column.Property);
        if (args.Column.Property == "Visitor")
        {
        //Debug.WriteLine("in cell render, value is " + args.Data.Visitor);
            if (args.Data.Visitor.ToLower().StartsWith("week"))
            {
                // <td style="color:red; text-decoration:underline; font-weight:bold">@game.Visitor.ToUpper()</td>
                //args.Attributes.Add("style", "color:red; text-decoration:underline; font-weight:bold");
                //args.Attributes.Add("style", $"font-weight: {(args.Data.Quantity > 20 ? "bold" : "normal")};");

            //    args.Attributes.Add("style", "background-color:red; text-decoration:underline; font-weight:bold");
            }
        }
    }
    */

    private Dictionary<string, object> GetCellStyle(BlazorSbt.Shared.Schedules data)
    {
        var attributes = new Dictionary<string, object>();

        if (data.Visitor.ToUpper().StartsWith("WEEK"))
        {
            attributes.Add("style", "color:red; text-decoration:underline; font-weight:bold");
        }

        return attributes;
    }

    private Dictionary<string, object> GetLinkAttributes(BlazorSbt.Shared.Schedules game)
    {
        var attributes = new Dictionary<string, object>();
        var url = $"{Organization}/{Id}/{game.GameID}";
        
        attributes.Add("href", url);
        
        return attributes;
    }

    public void Dispose()
    {
        this.Subscription.Dispose();
    }
}
