using BlazorSbt.Shared.Models.Requests;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BlazorSbt.Shared.Models.ViewModels;

public record ScoresValidationError(int GameID, string MemberName, string Error);

public class ScoresViewModel
{
    public string Organization { get; set; } = default!;

    public string Abbreviation { get; set; } = default!;

    //[ValidateEachItemAttribute] // class implemented at bottom of this file
    [ValidateComplexType]
    public IList<ScheduleSubsetForScoresViewModel> Schedule { get; set; } = default!;

    // used by view component to display db errors, etc, to user
    public string ErrorMessage { get; set; } = string.Empty;

    public ScoresViewModel() { }

    public ScoresViewModel(IList<Schedule> schedule, string organization, string abbreviation)
    {
        this.Organization = organization;
        this.Abbreviation = abbreviation;
        this.Schedule = new List<ScheduleSubsetForScoresViewModel>();
        for (int i = 0; i < schedule.Count; i++)
        {
            var subset = new ScheduleSubsetForScoresViewModel
            {
                GameID = schedule[i].GameID,
                HomeScore = schedule[i].HomeScore,
                VisitorScore = schedule[i].VisitorScore,
                HomeForfeit = schedule[i].HomeForfeit,
                VisitorForfeit = schedule[i].VisitorForfeit,
                Home = schedule[i].Home,
                Visitor = schedule[i].Visitor,
                Day = schedule[i].Day,
                Time = schedule[i].Time,
                Field = schedule[i].Field,
                OvertimeGame = schedule[i].OvertimeGame,
            };
            this.Schedule.Add(subset);
        }
    }

    public UpdateScoresRequest ToScoresRequest()
    {
        var request = new UpdateScoresRequest
        {
            Organization = this.Organization,
            Abbreviation = this.Abbreviation,
            Scores = this.Schedule.Select(game => new ScheduleSubsetForUpdateScoresRequest
            {
                GameID = game.GameID,
                HomeScore = game.HomeScore,
                VisitorScore = game.VisitorScore,
                HomeForfeit = game.HomeForfeit,
                VisitorForfeit = game.VisitorForfeit
            }).ToList(),
        };

        return request;
    }

    // note - the List<string> could be just a string here since
    // we are only ever adding a single error to a given key in this example.
    public bool IsValid(out List<ScoresValidationError> errors)
    {
        // Check that forfeit scores are 7-0 or 0-0 if checkboxes are selected.

        int gameNumber = 0;
        var errorMessage = "";
        errors = new List<ScoresValidationError>();

        foreach (var game in this.Schedule)
        {
            gameNumber++;
            if (game.HomeForfeit)
            {
                if (game.VisitorForfeit)
                {
                    if (game.HomeScore != 0 || game.VisitorScore != 0)
                    {
                        errorMessage = "Score must be 0-0 for double forfeit.";
                        
                        var error = new ScoresValidationError(gameNumber, 
                            nameof(ScheduleSubsetForScoresViewModel.HomeScore), errorMessage);
                        errors.Add(error);

                        error = new ScoresValidationError(gameNumber,
                            nameof(ScheduleSubsetForScoresViewModel.VisitorScore), errorMessage);
                        errors.Add(error);
                    }
                }
                else
                {
                    if (game.HomeScore != 0 || game.VisitorScore != 7)
                    {
                        errorMessage = "Score must be 7-0 for forfeit.";

                        var error = new ScoresValidationError(gameNumber,
                            nameof(ScheduleSubsetForScoresViewModel.HomeScore), errorMessage);
                        errors.Add(error);
                    }
                }
            }
            else if (game.VisitorForfeit)
            {
                if (game.HomeScore != 7 || game.VisitorScore != 0)
                {
                    errorMessage = "Score must be 7-0 for forfeit.";

                    var error = new ScoresValidationError(gameNumber,
                        nameof(ScheduleSubsetForScoresViewModel.VisitorScore), errorMessage);
                    errors.Add(error);
                }
            }
        }

        // return true for valid if no errors found within this method
        return (errors.Any() == false);
    }
}

public class ScheduleSubsetForScoresViewModel
{
    public int GameID { get; set; }

    public string Home { get; set; } = string.Empty;

    public string Visitor { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? Day { get; set; }

    public DateTime? Time { get; set; }

    [DisplayName("Field")]
    public string Field { get; set; } = string.Empty;

    [Required]
    [Range(0, 999, ErrorMessage = "Home Score invalid (0-999).")]
    //[ForfeitScoreValidator(nameof(HomeForfeit), nameof(VisitorForfeit), nameof(VisitorScore))]
    public short? HomeScore { get; set; }

    [Required]
    [Range(0, 999, ErrorMessage = "Visitor Score invalid (0-999).")]
    //[ForfeitScoreValidator(nameof(VisitorForfeit), nameof(HomeForfeit), nameof(HomeScore))]
    public short? VisitorScore { get; set; }

    public bool HomeForfeit { get; set; }

    public bool VisitorForfeit { get; set; }

    public bool OvertimeGame { get; set; }
}

// not needed, saved for future reference: (tried to use on list)
// (note - this method does not cause a user-friendly message in the validation summary)
public class ValidateEachItemAttribute : ValidationAttribute
{
    protected readonly List<ValidationResult> validationResults = [];

    public override bool IsValid(object? value)
    {
        if (value is not System.Collections.IEnumerable list) return true;

        var isValid = true;

        foreach (var item in list)
        {
            var validationContext = new ValidationContext(item);
            var isItemValid = Validator.TryValidateObject(item, validationContext, validationResults, true);
            isValid &= isItemValid;
        }
        
        return isValid;
    }
}

// note to self - this validator used the code from the class's IsValid method
public class ForfeitScoreValidator : ValidationAttribute
{
    private string ForfeitCheckboxPropertyName { get; init; }
    private string OppositeForfeitCheckboxPropertyName { get; init; }
    private string OppositeScorePropertyName { get; init; }

    public ForfeitScoreValidator(string forfeitCheckboxPropertyName, string oppositeForfeitCheckboxPropertyName, 
        string oppositeScorePropertyName)
    {
        this.ForfeitCheckboxPropertyName = forfeitCheckboxPropertyName;
        this.OppositeForfeitCheckboxPropertyName= oppositeForfeitCheckboxPropertyName;
        this.OppositeScorePropertyName = oppositeScorePropertyName;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext == null)
        {
            return ValidationResult.Success!;
        }

        var forfeitCheckboxProperty = validationContext.ObjectType.GetProperty(this.ForfeitCheckboxPropertyName);
        var oppositeForfeitCheckboxProperty = validationContext.ObjectType.GetProperty(this.OppositeForfeitCheckboxPropertyName);
        var oppositeScoreProperty = validationContext.ObjectType.GetProperty(this.OppositeScorePropertyName);

        var forfeitValue = forfeitCheckboxProperty!.GetValue(validationContext.ObjectInstance);
        var oppositeForfeitValue = oppositeForfeitCheckboxProperty!.GetValue(validationContext.ObjectInstance);
        var oppositeScoreValue = oppositeScoreProperty!.GetValue(validationContext.ObjectInstance);

        // todo - check above for null values and return useful error messages

        if (forfeitValue is bool forfeit)
        {
            if (forfeit == false)
            {
                // nothing to check in this validator
                return ValidationResult.Success!;
            }

            if (value is short score && oppositeScoreValue is short oppositeScore)
            {
                if (oppositeForfeitValue is bool oppositeForfeit)
                {
                    if (oppositeForfeit)
                    {
                        if (score != 0 || oppositeScore != 0)
                        {
                            return new ValidationResult("Score must be 0-0 for double forfeit.",
                                new[] { validationContext.MemberName }!);
                        }
                    }
                    else
                    {
                        if (score != 0 || oppositeScore != 7)
                        {
                            return new ValidationResult("Score must be 7-0 for forfeit.",
                                new[] { validationContext.MemberName }!);
                        }
                    }
                }
                else
                {
                    // todo - return useful error message
                }
            }
            else
            {
                // todo - return useful error message
            }
        }
        else
        {
            // todo - return useful error message
        }

        return ValidationResult.Success!;
    }
}
