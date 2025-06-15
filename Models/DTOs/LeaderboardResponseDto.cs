using System.Collections.Generic;

namespace ClimbUpAPI.Models.DTOs
{
    public class LeaderboardResponseDto
    {
        public required List<LeaderboardEntryDto> Entries { get; set; }
        public int TotalEntries { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}