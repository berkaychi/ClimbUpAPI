namespace ClimbUpAPI.Models
{
    public class ToDoTag
    {
        public int ToDoItemId { get; set; }
        public ToDoItem ToDoItem { get; set; } = null!;

        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }

}