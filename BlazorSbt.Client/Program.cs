using BlazorSbt.Shared.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazr.RenderState.WASM;

namespace BlazorSbt.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddTransient<IDivisionService, DivisionServiceForWasm>();

            // from https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-8.0
            builder.Services.AddHttpClient();

            // may be able to remove adding httpclient call above at some point
            builder.AddBlazrRenderStateWASMServices();

            builder.Services.AddTransient<IIsWasmProjectService, IsNotWasmProjectService>();

            // from a sample app on MSLearn:
            //builder.Services.AddScoped(sp =>
            //    new HttpClient
            //    {
            //        BaseAddress = new Uri(builder.Configuration["FrontendUrl"] ?? "https://localhost:5002")
            //    });

            await builder.Build().RunAsync();
        }
    }
}
