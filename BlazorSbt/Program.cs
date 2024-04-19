using BlazorSbt.Components;
using Microsoft.EntityFrameworkCore;

namespace BlazorSbt;

// save this note until i can put it into draft...
// when I added a css file for standings it looked like I caused a loss of all css loading,
// but the problem was actually due to a change in the @page route - it started matching
// for css files in wwwroot (considered top level for those files for some reason).
// the fix was adding :nonfile to the end of org in standingslist.razor and id in standings.razor
// what helped me figure out that this was happening was viewing page source then clicking on the
// css file links and seeing that it loaded my standingslist page instead. finding a fix was
// more difficult as this seems to be a new issue with v8 of Blazor and Chat could not help.

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddDbContextFactory<BlazorSbt.Shared.Data.DivisionContext>(options =>
            //options.UseSqlServer(builder.Configuration.GetConnectionString("Local_Sql_ConnectionString")
            options.UseSqlServer(builder.Configuration.GetConnectionString("Azure_Sql_ConnectionString")
                ?? throw new InvalidOperationException("Connection string not found.")));

        builder.Services.AddQuickGridEntityFrameworkAdapter();

        // see RadzenDatagrid:OnInitializedAsync() for why this is needed:
        builder.Services.AddMemoryCache();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<BlazorSbt.Shared.Data.DivisionContext>();
            context.Database.EnsureCreated();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
    }
}
