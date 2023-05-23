using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Config;
using WebApi.Domain;
using WebApi.Filters;
using WebApiWithAad.Mappers.User;
using WebApiWithAad.Services.AzureGraph;

namespace WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class RestrictedController : ControllerBase
    {
        private readonly IUserMapper _userMapper;
        private readonly IAzureGraphApiService _azureGraphApiService;

        public RestrictedController(IUserMapper userMapper,
            IAzureGraphApiService azureGraphApiService)
        {
            _userMapper = userMapper;
            _azureGraphApiService = azureGraphApiService;
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
        public async Task<ActionResult> ImpersonateUser(int userId, string clientId)
        {
            var isUserEnabled = true;
            var userPermissions = _userMapper.GetUserPermissions(userId);
            var targetUserHasWebAdmin = userPermissions.FirstOrDefault(x => x.PermissionName == "Web:Admin") != null;
            if (isUserEnabled && !targetUserHasWebAdmin)
            {
                string guid = User.Claims.Where(x => x.Type == "sub").Select(x => x.Value).FirstOrDefault();
                // enable possibility to impersonate anyone
                await _azureGraphApiService.EnableImpersonation(guid, IsWebAdmin());
            }
            return Ok(new
            {
                IsEnabled = isUserEnabled && !targetUserHasWebAdmin,
                UserId = userId
            });
        }

        [HttpGet]
        [ActionName("GetCurrentUserId")]
        public IActionResult GetCurrentUserId()
        {
            var userId = User.GetUserId<int>();
            return Ok(userId);
        }

        [HttpGet]
        [ActionName("GetCurrentUserRoles")]
        public IActionResult GetCurrentUserRoles()
        {
            return Ok(User.Claims.Where(x => x.Type == "role").Select(x => x.Value));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MigrateUsers()
        {
            await _azureGraphApiService.MigrateUsers();
            return Ok();
        }

        private bool IsWebAdmin()
        {
            return IsInRole("Web:Admin");
        }

        private bool IsInRole(string role)
        {
            var roleClaims = User.Claims.Where(x => x.Type == "role").ToList();
            return roleClaims.FirstOrDefault(x => x.Value == role) != null;
        }
    }
}
