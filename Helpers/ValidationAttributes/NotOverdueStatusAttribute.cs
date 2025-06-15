using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Helpers.ValidationAttributes
{
    public class NotOverdueStatusAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is ToDoStatus status)
            {
                if (status == ToDoStatus.Overdue)
                {
                    return new ValidationResult("Cannot manually set status to Overdue. This is an automated status.");
                }
            }
            return ValidationResult.Success;
        }
    }
}