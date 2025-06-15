using System.ComponentModel.DataAnnotations;
using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class CreateFocusSessionDto
    {
        public int? SessionTypeId { get; set; }
        public int? CustomDurationSeconds { get; set; }

        public List<int>? TagIds { get; set; }

        public int? ToDoItemId { get; set; }
    }
}
