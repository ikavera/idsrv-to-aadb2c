using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Config;
using WebApi.Domain;
using WebApi.Filters;
using WebApi.Mappers;

namespace WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class RestrictedController : ControllerBase
    {
        private readonly IUserMapper _userMapper;

        public RestrictedController(IUserMapper userMapper)
        {
            _userMapper = userMapper;
        }

        [HttpGet]
        [MultiplePoliciesAuthorize("Web:Admin;Web:User")]
        public IActionResult GetData()
        {
            return Ok("got access");
        }

        [HttpGet]
        [MultiplePoliciesAuthorize("Web:Admin;Web:User")]
        public IActionResult GetUsersCount()
        {
            return Ok(_userMapper.FindAll().Count);
        }

        [HttpGet]
        [MultiplePoliciesAuthorize("Web:Admin;Web:User")]
        public IActionResult GetUserProfile()
        {
            return Ok(_userMapper.FindByUserId(User.GetUserId<int>()));
        }

        [HttpGet]
        [MultiplePoliciesAuthorize("Web:Admin")]
        public IActionResult GetUsersList()
        {
            return Ok(_userMapper.FindAll());
        }

        [HttpGet]
        [MultiplePoliciesAuthorize("Web:Admin")]
        public IActionResult GetUser(int userId)
        {
            return Ok(_userMapper.FindByUserId(userId));
        }

        [HttpGet]
        [ActionName("ImpersonateUser")]
        [MultiplePoliciesAuthorize("Web:Admin")]
        public ActionResult ImpersonateUser(int userId, string clientId)
        {
            return Ok(new
            {
                AuthUrl = ProjectSettings.AuthServer + "Account/Impersonate/",
                UserId = userId
            });
        }
    }
}
