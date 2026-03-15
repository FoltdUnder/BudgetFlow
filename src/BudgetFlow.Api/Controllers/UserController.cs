using Microsoft.AspNetCore.Mvc;
using BudgetFlow.Application.Authentication;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    // private readonly IUserService _userService;

    // public UsersController(IUserService userService)
    // {
    //     _userService = userService;
    // }

    // [HttpGet]
    // public IActionResult GetAll()
    // {
    //     var users = _userService.GetAll();
    //     return Ok(users);
    // }
}