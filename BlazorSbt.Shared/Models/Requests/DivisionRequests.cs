namespace BlazorSbt.Shared.Models.Requests;

public class CreateDivisionResponse : IResponse { }

public class CreateDivisionRequest : IRequest
{
    public string League { get; set; } = default!;

    public string NameOrNumber { get; set; } = default!;
}

public class DeleteDivisionResponse : IResponse { }

public class DeleteDivisionRequest : IRequest { }

public class DivisionExistsResponse : IResponse { }

public class DivisionExistsRequest : IRequest { }

public class GetDivisionResponse : IResponse
{
    public Division Division { get; set; } = default!;
}

public class GetDivisionRequest : IRequest { }

public class GetDivisionListResponse : IResponse
{
    public IList<Division> DivisionList { get; set; } = default!;
}

public class GetDivisionListRequest : IRequest { }

public class UpdateDivisionResponse : IResponse { }

public class UpdateDivisionRequest : CreateDivisionRequest
{
    public bool Locked { get; set; }
}
