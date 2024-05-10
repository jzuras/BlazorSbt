using BlazorSbt.Shared.Models.Requests;
using BlazorSbt.Shared.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;

namespace BlazorSbt.Shared.Components;

// todo - when running on azure, the render state being displayed shows empty for a short time,
// but longer when waiting on CSR mode like here in Scores. Not sure what this empty mode is.

public partial class ScoresHeaderComponent : ComponentBaseWithLogging
{
    [Parameter]
    public string Organization { get; set; } = "";

    [Parameter]
    [DisplayName("Division")] // This value is used for display by NameLabelComponent
    public string Id { get; set; } = "";

    [Parameter]
    public int GameId { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [Inject]
    Services.IDivisionService Service { get; set; } = default!;
    
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private ScoresViewModel Model { get; set; } = new ScoresViewModel();
    private EditContext? EditFormContext { get; set; }
    private ValidationMessageStore? MessageStore { get; set; }
    //private bool FormInvalid { get; set; } // could be used to disable submit button

    private bool ShouldRenderFlag = false;

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        // note - there are validation errors created by attributes on the Model
        // (like [Required]) and I was concerned that the line below would clear
        // those out as well as the ones I add inside AddErrorsToMessageStore(),
        // but it seems to work as i want it to - that error remains if needed,
        // and OnValidSubmit is not called.
        this.MessageStore?.Clear();

        if (this.Model.IsValid(out List<ScoresValidationError> errors) == false )
        {
            this.AddErrorsToMessageStore(errors);
        }
    }

    private void AddErrorsToMessageStore(List<ScoresValidationError> errors)
    {
        foreach (var error in errors)
        {
            string memberName = error.MemberName;
            int index = error.GameID - 1; // game number starts at 1

            var fieldIdentifier = new FieldIdentifier(this.Model.Schedule[index], memberName);

            this.MessageStore!.Add(fieldIdentifier, error.Error);
        }
    }

    private async Task OnValidSubmit()
    {
        var request = this.Model.ToScoresRequest();
        var response = await this.Service.SaveScores(request);

        if (!response.Success)
        {
            this.Model.ErrorMessage = response.Message ?? "Unknown Error while Saving Data.";
            return;
        }

        var url = $"{this.Organization}/{this.Id}";//}/{value}";
        NavigationManager.NavigateTo(url, forceLoad: false, replace: false);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var request = new GetScoresRequest
            {
                Organization = this.Organization,
                Abbreviation = this.Id,
                GameID = this.GameId
            };

            var response = await this.Service.GetGames(request);

            if (response.Success == false || response.Games == null)
            {
                // default empty list will be displayed
            }

            var games = response.Games;

            this.Model = new(games!, this.Organization, this.Id);
            this.EditFormContext = new EditContext(this.Model);
            this.MessageStore = new(this.EditFormContext);

            // be sure to dispose any added delegates:
            //this.EditContext.OnFieldChanged += this.HandleFieldChanged; // unused but saved for future reference
            this.EditFormContext.OnValidationRequested += this.HandleValidationRequested;
        }
        catch (Exception)
        {
            // default empty list will be displayed
        }

        this.ShouldRenderFlag = true;

        await base.OnInitializedAsync();
    }

    // Dispose Pattern:

    private bool Disposed { get; set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        if (disposing)
        {
            if (this.EditFormContext is not null)
            {
                //this.EditContext.OnFieldChanged -= this.HandleFieldChanged;
                this.EditFormContext.OnValidationRequested -= this.HandleValidationRequested;
            }
        }

        Disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

#region Unused Methods for Future Reference
    // not currently used
    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (this.EditFormContext is not null)
        {
            this.MessageStore!.Clear();

            //this.FormInvalid = !this.EditContext.Validate();
            bool isValid = !this.EditFormContext.Validate();
            base.StateHasChanged();
        }
    }

    protected override bool ShouldRender()
    {
        // I wanted to use this method to stop rendering during the await in Init method
        // but that did not work. Leaving it here for future reference.
        return this.ShouldRenderFlag;
    }
    #endregion
}
