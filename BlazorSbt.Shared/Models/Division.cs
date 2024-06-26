﻿using System.ComponentModel.DataAnnotations;

// Razor does not play well with nullable reference types,
// but this line will still allow for null derefernce warnings
#nullable disable annotations

// Divisions table handles the link from an ID to a more useful, longer description,
// as well as the last updated time.
// Example usage:
// ID = FC01
// League = Fall Coed
// Division = 1

namespace BlazorSbt.Shared.Models;

public class Division
{
    // Note: this domain object is also used directly as a view model,
    // so display-centric attributes are included here.

    [RegularExpression(@"^[a-zA-Z0-9]+[ a-zA-Z0-9-_]*$")]
    public string Organization { get; set; } = string.Empty;

    [Required]
    //[Comment(Short string version used in URLs - must be unique within an Organization.")]
    [RegularExpression(@"^[a-zA-Z]+[a-zA-Z0-9-_]*$", ErrorMessage = "Allowed: digits, letters, dash, and underline.")]
    [StringLength(50, MinimumLength = 2)]
    public string Abbreviation { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[a-zA-Z0-9]+[ a-zA-Z0-9-_]*$", ErrorMessage = "Allowed: digits, letters, dash, underline, and spaces.")]
    public string League { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Name")]
    [RegularExpression(@"^[a-zA-Z0-9]+[ a-zA-Z0-9-_]*$", ErrorMessage = "Allowed: digits, letters, dash, underline, and spaces.")]
    public string NameOrNumber { get; set; } = string.Empty;

    [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy h:mm tt}", ApplyFormatInEditMode = false)]
    public DateTime Updated { get; set; }

    //[Comment("Locked is used to prevent scores from being reported.")]
    public bool Locked { get; set; }

    public List<Standings> Standings { get; set; } = new List<Standings>();

    public List<Schedule> Schedule { get; set; } = new List<Schedule>();
}

public class DisabledOnAzureAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        // Originally, this was going to validate the uniqueness of the division ID within an Organization,
        // but since this is called before the Organization can be set in Create's OnPost(),
        // this doesn't work. But I am keeping this code here and commented out for future reference purposes.
            /*
            var dbContext = (Sbt.Data.DemoContext)validationContext.GetService(typeof(Sbt.Data.DemoContext));
            var id = (string)value;
            var organization = ((Divisions)validationContext.ObjectInstance).Organization;

            if (dbContext!.Divisions.Any(e => e.Organization == organization && e.ID != id))
            {
                return new ValidationResult(ErrorMessage);
            }
            */
            return ValidationResult.Success;
    }
}