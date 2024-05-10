using BlazorSbt.Shared.Models.Requests;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace BlazorSbt.Shared.Components;

public partial class StandingsListComponent : ComponentBaseWithLogging, IDisposable
{
    [Parameter]
    public string Organization { get; set; } = "";

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    public PersistentComponentState ApplicationState { get; set; } = default!;

    [Inject]
    Services.IDivisionService Service { get; set; } = default!;

    private IQueryable<Models.Division> DivisionList { get; set; } = default!;
    private PersistingComponentStateSubscription Subscription;
    private string Key = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        // This component may be run on the server or within WASM.
        // The page is setting the render mode based on "hockey" or not.
        // The injected Service when run as WASM is the API version, but
        // if the component is pre-rendered, it will first be run on the server.
        // This means two db calls, but turning off pre-rendering is an option 
        // to avoid this if so desired.
        // Another option is to do the check against HttpContext within
        // this component to determine where it is running
        // (as is being done in the Razor code for this component).
        // (Update - other parts of the app are now using a library instead
        // of HttpContext to check render mode.)

        // Update - now using pre-render and saving to App State.
        // Doing so is not actually needed except for Hockey since
        // non-hockey only does a static page, so an enhancement
        // could be to skip persisting state for non-hockey.


        this.Subscription = ApplicationState.RegisterOnPersisting(this.Persist);

        this.Key = this.Organization;

        var foundInState = ApplicationState.TryTakeFromJson<List<Models.Division>>(this.Key, out var divisionList);

        if (foundInState)
        {
            this.DivisionList = divisionList!.AsQueryable();
        }
        else
        {
            var request = new GetDivisionListRequest
            {
                Organization = this.Organization,
            };

            var response = await Service.GetDivisionList(request);

            if (response.Success == false || response.DivisionList == null)
            {
                // default empty list will be displayed
            }
            else
            {
                this.DivisionList = response.DivisionList.AsQueryable();
            }
        }

        await base.OnParametersSetAsync();
    }

    private Task Persist()
    {
        ApplicationState.PersistAsJson(this.Key, this.DivisionList);
        return Task.CompletedTask;
    }

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
            this.Subscription.Dispose();
            Console.WriteLine("Dispose called for StandingsList Component.");
        }

        Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

