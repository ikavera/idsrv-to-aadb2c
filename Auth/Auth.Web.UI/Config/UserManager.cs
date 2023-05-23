using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Auth.Domain.Users;
using Auth.Mappers;
using Auth.Service.Cache;
using log4net;
using Microsoft.AspNetCore.Identity;
using Auth.Service.Encryption.Impl;

namespace Auth.Web.UI.Config
{
    public class UserManager : IUserManager
    {
        private readonly ICacheService _cacheService;
        protected static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UserManager(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public Task<UserDetails> FindClientByIdAsync(string username)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUsername(username);
            if (user != null)
            {
                user.UserPermissions = GetUserPermissions(user.Id);
            }
            return Task.FromResult(user);
        }

        public Task<UserDetails> FindByEmailAsync(string username)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUsername(username);
            if (user != null)
            {
                user.UserPermissions = GetUserPermissions(user.Id);
            }
            return Task.FromResult(user);
        }

        public Task<UserDetails> FindByUsernameAndApiKey(string username, string apiKey)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUsernameAndApiKey(username, apiKey);
            if (user != null && user.ApiKey.ToLower() == apiKey.ToLower())
            {
                user.UserPermissions = GetUserPermissions(user.Id);
            }
            else
            {
                throw new ArgumentException("ApiKey is wrong");
            }
            return Task.FromResult(user);
        }

        public Task<UserDetails> FindByIdAsync(int userId)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUserId(userId);
            if (user != null)
            {
                user.UserPermissions = GetUserPermissions(user.Id);
            }

            return Task.FromResult(user);
        }

        public Task<string> GeneratePasswordResetTokenAsync(int userId)
        {
            var code = Guid.NewGuid().ToString();
            try
            {
                MapperRegistry.Instance.UserMapper.InsertPasswordReset(new UserPasswordReset
                {
                    UserId = userId,
                    ResetToken = code,
                    IsUsed = false,
                    ExpirationDateUtc = DateTime.UtcNow.AddDays(1)
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.FromResult(code);
        }

        public async Task<IdentityResult> ResetPasswordAsync(int userId, string code, string password)
        {
            var isValidToken = await VerifyUserTokenAsync(userId, code);
            if (isValidToken)
            {
                var user = await FindByIdAsync(userId);
                user.UserPassword = PasswordStorage.CreateHash(password);
                MapperRegistry.Instance.UserMapper.Update(user);
                MapperRegistry.Instance.UserMapper.MarkCodeAsUsed(code, userId);
                return IdentityResult.Success;
            }

            return IdentityResult.Failed(new IdentityError
            {
                Description = "Used not valid code"
            });
        }

        public async Task<IdentityResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                if (ValidateCredentials(userId, currentPassword))
                {
                    var user = MapperRegistry.Instance.UserMapper.FindByUserId(userId);
                    user.UserPassword = PasswordStorage.CreateHash(newPassword);
                    MapperRegistry.Instance.UserMapper.Update(user);
                    return IdentityResult.Success;
                }

                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect user password"
                });

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect user password"
                });
            }
        }
        public async Task<IdentityResult> ChangePasswordByAdminAsync(int userId, string newPassword)
        {
            try
            {
                var user = MapperRegistry.Instance.UserMapper.FindByUserId(userId);
                user.UserPassword = PasswordStorage.CreateHash(newPassword);
                MapperRegistry.Instance.UserMapper.Update(user);
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return IdentityResult.Failed(new IdentityError
                {
                    Description = "Incorrect user password"
                });
            }
        }

        public Task<bool> VerifyUserTokenAsync(int userId, string code)
        {
            var userResetData = MapperRegistry.Instance.UserMapper.GetPasswordResetData(userId).ToList();
            if (userResetData.Any())
            {
                var matchingRecord = userResetData.FirstOrDefault(x => x.ResetToken == code);
                if (matchingRecord != null)
                {
                    if (!matchingRecord.IsUsed)
                    {
                        if (matchingRecord.ExpirationDateUtc >= DateTime.UtcNow)
                        {
                            return Task.FromResult(true);
                        }

                        Logger.Warn($"user {userId} tries to use expired token {code}");
                    }
                    else
                    {
                        Logger.Warn($"user {userId} tries to use used token {code}");
                    }
                }
                else
                {
                    Logger.Warn($"user {userId} use incorrect token {code}");
                }
            }

            return Task.FromResult(false);
        }

        public bool ValidateCredentials(string username, string password)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUsername(username);
            bool result = false;
            if (user != null)
            {
                bool isValid = PasswordStorage.VerifyPassword(password, user.UserPassword);
                if (!isValid)
                {
                    user = null;
                }
                result = isValid;
            }

            return result;
        }

        public bool ValidateCredentials(int userId, string password)
        {
            var user = MapperRegistry.Instance.UserMapper.FindByUserId(userId);
            bool result = false;
            if (user != null)
            {
                bool isValid = PasswordStorage.VerifyPassword(password, user.UserPassword);
                if (!isValid)
                {
                    user = null;
                }
                result = isValid;
            }

            return result;
        }

        private List<PortalPermission> GetUserPermissions(int userId)
        {
            var key = "userPermissions_" + userId;
            var cached = _cacheService.GetItem(key) as List<PortalPermission>;
            if (cached == null)
            {
                var permissions = MapperRegistry.Instance.UserMapper.GetUserPermissions(userId).ToList();
                _cacheService.AddItem(key, permissions, new TimeSpan(0, 0, 0, 10));
                cached = permissions;
            }

            return cached;
        }
    }
}
