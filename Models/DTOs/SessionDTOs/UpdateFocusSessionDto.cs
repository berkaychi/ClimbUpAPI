using ClimbUpAPI.Models.Enums;

namespace ClimbUpAPI.Models.DTOs.SessionDTOs
{
    public class UpdateFocusSessionDto
    {
        public List<int>? TagIds { get; set; }

        public int? ToDoItemId { get; set; }

        public int? FocusLevel { get; set; }
        public string? ReflectionNotes { get; set; }

    }
}
