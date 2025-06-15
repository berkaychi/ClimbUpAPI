using System.ComponentModel.DataAnnotations;

namespace ClimbUpAPI.Models.DTOs.Admin
{
    public class AssignRoleDto
    {
        [Required(ErrorMessage = "Role name is required.")]
        [RegularExpression("^(Admin|User)$", ErrorMessage = "Role must be either 'Admin' or 'User'.")]
        public string RoleName { get; set; } = string.Empty;
    }
}