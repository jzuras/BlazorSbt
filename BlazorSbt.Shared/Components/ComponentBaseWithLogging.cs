using Blazr.RenderState;
using Microsoft.AspNetCore.Components;

namespace BlazorSbt.Shared.Components;

public class ComponentBaseWithLogging : ComponentBase
{
    [Inject]
    protected IBlazrRenderStateService RenderStateService { get; set; } = default!;

    public string RenderStateForDisplay { get; set; } = "";

    private bool FirstRender = true;

     protected override Task OnParametersSetAsync()
    {
        if (this.FirstRender)
        {
            Console.WriteLine($"First Render for {this.GetType().Name} - {this.RenderStateService.RenderState} - ServiceId: {this.RenderStateService.Id} - OnParametersSetAsync");
            this.FirstRender = false;
        }
        else
        {
            Console.WriteLine($"{this.GetType().Name} - {this.RenderStateService.RenderState} - OnParametersSetAsync");
        }

        this.RenderStateForDisplay = (RenderStateService == null) ? "unknown" : this.RenderStateService.RenderState.ToString();

        return base.OnParametersSetAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        string server = "client";
        string firstRender = "false";
        if (this.RenderStateService.IsPreRender)
        {
            server = "server";
        }

        if (this.FirstRender)
        {
            firstRender = "true";
            this.FirstRender = false;
        }

        Console.Write($"*************************** {this.GetType().Name} instantiated within {server}");
        Console.WriteLine($" RenderState: {this.RenderStateService.RenderState} FirstRender: {firstRender}.");

        await base.OnInitializedAsync();
    }

}
