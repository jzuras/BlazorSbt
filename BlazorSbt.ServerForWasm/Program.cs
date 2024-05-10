using BlazorSbt.ServerForWasm.Components;
using BlazorSbt.Shared.Data.Repositories;
using BlazorSbt.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Blazr.RenderState.Server;

namespace BlazorSbt.ServerForWasm;


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

        builder.Services.AddTransient<IDivisionRepository, DivisionEfCoreRepository>();

        builder.Services.AddTransient<IDivisionService, DivisionServiceForWasm>();
        builder.Services.AddHttpClient();

        builder.Services.AddTransient<IIsWasmProjectService, IsWasmProjectService>();

        builder.Services.AddQuickGridEntityFrameworkAdapter();

        // see RadzenDatagrid:OnInitializedAsync() for why this is needed:
        builder.Services.AddMemoryCache();

        // may be able to remove adding httpclient call above at some point
        builder.AddBlazrRenderStateServerServices();
        
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
            .AddAdditionalAssemblies(typeof(ClientForWasm._Imports).Assembly)
            .AddAdditionalAssemblies(typeof(Shared._Imports).Assembly);

        app.Run();
    }
}
