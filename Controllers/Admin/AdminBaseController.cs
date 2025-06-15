using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimbUpAPI.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/[controller]")]
    public abstract class AdminBaseController : ControllerBase
    { }
}