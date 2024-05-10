using BlazorSbt.Shared.Models.ViewModels;
using Microsoft.AspNetCore.Components;

namespace BlazorSbt.Shared.Components;

public partial class ScoresSingleComponent
{
    [Parameter]
    public IList<ScheduleSubsetForScoresViewModel> Schedule { get; set; } = default!;

    [Parameter]
    public int index { get; set; }

    private void HandleHomeCheckboxClicked()
    {
        this.HandleCheckboxClicked(true);
    }

    private void HandleVisitorCheckboxClicked()
    {
        this.HandleCheckboxClicked(false);
    }

    private void HandleCheckboxClicked(bool isHome)
    {
        if(isHome && this.Schedule[index].HomeForfeit)
        {
            this.Schedule[index].HomeScore = 0;
            this.Schedule[index].VisitorScore = (short)((this.Schedule[index].VisitorForfeit == true) ? 0 : 7);
        }
        else if(isHome == false && this.Schedule[index].VisitorForfeit)
        {
            this.Schedule[index].HomeScore = (short)((this.Schedule[index].HomeForfeit == true) ? 0 : 7);
            this.Schedule[index].VisitorScore = 0;
        }

        base.StateHasChanged();
    }
}
