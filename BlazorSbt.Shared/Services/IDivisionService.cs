using BlazorSbt.Shared.Models.Requests;

namespace BlazorSbt.Shared.Services;

public interface IDivisionService : IDisposable
{
    public Task<CreateDivisionResponse> CreateDivision(CreateDivisionRequest request);

    public Task<DivisionExistsResponse> DivisionExists(DivisionExistsRequest request);

    public Task<LoadScheduleResponse> LoadScheduleFileAsync(LoadScheduleRequest request);

    public Task<GetDivisionListResponse> GetDivisionList(GetDivisionListRequest request);

    public Task<DeleteDivisionResponse> DeleteDivision(DeleteDivisionRequest request);

    public Task<GetDivisionResponse> GetDivision(GetDivisionRequest request);

    public Task<GetScoresResponse> GetGames(GetScoresRequest request);

    public Task<UpdateDivisionResponse> UpdateDivision(UpdateDivisionRequest request);

    public Task<UpdateScoresResponse> SaveScores(UpdateScoresRequest request);
}
