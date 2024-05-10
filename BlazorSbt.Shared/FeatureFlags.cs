namespace BlazorSbt.Shared;

public static class FeatureFlags
{
    // instead of project-defined variables like below, could get this from appSettings.json
   
#if USE_INTERACTIVE_ROUTING
    public static bool UseInteractiveRouting { get; set; } = true;
#else
    public static bool UseInteractiveRouting { get; set; } = false;
#endif
}
