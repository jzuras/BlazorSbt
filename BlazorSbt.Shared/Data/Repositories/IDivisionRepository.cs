using BlazorSbt.Shared.Models;

namespace BlazorSbt.Shared.Data.Repositories;

public interface IDivisionRepositoryFactory
{
    IDivisionRepository CreateRepository(IDivisionRepository.RepositoryType repositoryType);
}

public interface IDivisionRepository : IDisposable
{
    public enum RepositoryType
    {
        Uninitialized = 0,
        Unavailable = 1,
        Cosmos = 2,
        SqlServer = 3,
        MySql = 4,
        PostgreSql = 5,
        Default = Cosmos,
    }

    public static RepositoryType RepositoryTypeFromString(string value, bool useDefault = true)
    {
        IDivisionRepository.RepositoryType returnValue;
        if (!Enum.TryParse(value, true, out returnValue))
        {
            returnValue = useDefault ? IDivisionRepository.RepositoryType.Default :
                                       IDivisionRepository.RepositoryType.Uninitialized;
        }

        return returnValue;
    }

    /// <summary>
    /// Returns true if Division exists for the given Organization and Abbreviation,
    /// false otherise.
    /// </summary>
    /// <param name="organization">Part of the primary key.</param>
    /// <param name="abbreviation">Part of the primary key.</param>
    /// <returns>True or false.</returns>
    public Task<bool> DivisionExists(string organization, string abbreviation);

    /// <summary>
    /// Returns the Division for the given Organization and Abbreviation.
    /// </summary>
    /// <param name="organization">Part of the primary key.</param>
    /// <param name="abbreviation">Part of the primary key.</param>
    /// <returns>The Division for the given Organization and Abbreviation.</returns>
    public Task<Division> GetDivision(string organization, string abbreviation);

    /// <summary>
    /// Returns a (possibly empty) list of Division for the given Organization.
    /// </summary>
    /// <param name="organization">Organization used to retreive Division List.</param>
    /// <returns>Possibly empty list of Divisions.</returns>
    public Task<List<Division>> GetDivisionList(string organization);

    /// <summary>
    /// Returns a list of all games (schedule object) on the same day and field
    /// as the game identified by the game ID parameter.
    /// </summary>
    /// <param name="organization">Part of the primary key.</param>
    /// <param name="abbreviation">Part of the primary key.</param>
    /// <param name="gameID">ID of the game.</param>
    /// <returns>List of games (schedule object).</returns>
    public Task<List<Schedule>> GetGames(string organization, string abbreviation, int gameID);

    /// <summary>
    /// Handles creating, deleting, and updating divisions. 
    /// Calling method is expected to enforce requirements such as 
    /// not creating a divsiion that already exists, if so desired.
    /// </summary>
    /// <param name="division">Division object which is being tracked, except for creating.</param>
    /// <param name="deleteDivision">True to delete this division, false otherwise.</param>
    /// <param name="createDivision">True to create this division, false otherwise.</param>
    public Task SaveDivision(Division division, bool deleteDivision = false, bool createDivision = false);
}
