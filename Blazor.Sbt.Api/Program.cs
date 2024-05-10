using BlazorSbt.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace Blazor.Sbt.Api2;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<DivisionContext>(options =>
        {
            //options.UseSqlServer(builder.Configuration.GetConnectionString("Local_Sql_ConnectionString")
            options.UseSqlServer(builder.Configuration.GetConnectionString("Azure_Sql_ConnectionString")
                ?? throw new InvalidOperationException("Connection string not found."));
        }, ServiceLifetime.Scoped);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowBlazorApp",
                builder =>
                {
                    builder.WithOrigins("https://localhost:7149")
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
        });

        // this is how an MS sample app did it:
        //builder.Services.AddCors(
        //    options => options.AddDefaultPolicy(
        //        policy => policy.WithOrigins([builder.Configuration["BackendUrl"] ?? "https://localhost:5001",
        //            builder.Configuration["FrontendUrl"] ?? "https://localhost:5002"])
        //            .AllowAnyMethod()
        //            .AllowAnyHeader()));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.UseCors("AllowBlazorApp");

        app.MapControllers();

        app.Run();
    }
}
