using Microsoft.AspNetCore.Http;

namespace BlazorSbt.Shared.Models.Requests;

public class LoadScheduleResponse : IResponse
{
    public DateTime FirstGameDate { get; init; }
    public DateTime LastGameDate { get; init; }
}

public class LoadScheduleRequest : IRequest
{
    public bool UsesDoubleHeaders { get; set; }

    public IFormFile ScheduleFile { get; set; } = default!;
}
