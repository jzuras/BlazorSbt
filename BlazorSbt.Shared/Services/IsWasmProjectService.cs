
// this is a kludge to handle the fact that I am doing weird things by having
// multiple projects, which use different render modes, and then have pages
// that use different modes at runtime for their components (for testing purposes!).

// Each project will inject a service to tell the shared library which project
// is using it, since it needs to do different things for each project.

namespace BlazorSbt.Shared.Services;

public interface IIsWasmProjectService
{
    bool IsWasmProject { get; }
}

public class IsWasmProjectService : IIsWasmProjectService
{
    public bool IsWasmProject => true;
}

public class IsNotWasmProjectService : IIsWasmProjectService
{
    public bool IsWasmProject => false;
}
