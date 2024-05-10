namespace BlazorSbt.Shared.Models.Requests;

public class GetScoresResponse : IResponse
{
    public IList<Schedule> Games { get; set; } = default!;
}

public class GetScoresRequest : IRequest
{
    public int GameID { get; set; }
}

public class UpdateScoresResponse : IResponse { }

public class UpdateScoresRequest : IRequest
{
    public IList<ScheduleSubsetForUpdateScoresRequest> Scores { get; set; } = default!;
}

public class ScheduleSubsetForUpdateScoresRequest
{
    public int GameID { get; set; }

    public short? HomeScore { get; set; }

    public short? VisitorScore { get; set; }

    public bool HomeForfeit { get; set; }

    public bool VisitorForfeit { get; set; }
}
