using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebApiWithAad.Domain;
using WebApiWithAad.Services.AzureGraph;
using WebApiWithAad.Services.User;

namespace WebApiWithAad.Controllers
{
    [Route("api/[controller]/[action]")]
    public class DesktopValidationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAzureGraphApiService _azureGraphApiService;

        public DesktopValidationController(IUserService userService,
            IAzureGraphApiService azureGraphApiService)
        {
            _userService = userService;
            _azureGraphApiService = azureGraphApiService;
        }

        [HttpPost]
        [AllowAnonymous]
        [ActionName("ValidateApiKey")]
        public async Task<IActionResult> ValidateApiKey([FromBody] KeyValidationModel model)
        {
            var proceed = false;
            if (!string.IsNullOrEmpty(model?.CustomGrantType))
            {
                proceed = model.CustomGrantType == "custom_grant_token";
            }

            if (!proceed)
            {
                return BadRequest("Can't validate api key");
            }
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.ApiKey))
            {
                return BadRequest("Can't validate api key");
            }

            var aadb2c = _userService.FindAadB2cByUserNameAndKey(model.Email, model.ApiKey);
            var userAadb2c = await _azureGraphApiService.LoadGraphUser(aadb2c, new List<string> { "AccountEnabled" });
            if (userAadb2c == null || !userAadb2c.AccountEnabled.Value)
            {
                if (userAadb2c != null)
                {
                    var headers = Request.Headers;
                    var headersStr = new StringBuilder();
                    foreach (var header in headers)
                    {
                        headersStr.Append($"'{header.Key}':{string.Join(",", header.Value)}");
                    }
                }
                else
                {
                    //logging
                }
                return BadRequest("User doesn't exist or is disabled");
            }

            return Ok(new
            {
                IsKeyValid = !string.IsNullOrEmpty(aadb2c),
                ObjectId = aadb2c
            });
        }
    }
}
