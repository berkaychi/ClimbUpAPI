namespace ClimbUpAPI.Models
{
    public class UserHiddenSystemEntity
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
    }
}