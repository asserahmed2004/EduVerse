using AutoMapper;
using Domain.Interfaces;
using Application.Services.Interfaces.Auth;

using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs.Auth;
using Application.DTOs.Responses;
using Application.Validations;
using Domain.Entities;
using System.Net.Mail;
using System.Net;
using Application.Services.Interfaces;
using Application.DTOs.Cloud;
using System.Security.Claims;

namespace Application.Services.Implementitions.Auth
{
    public class AuthService (IRoleManagment roleManagment 
        , ITokenManagment tokenManagment , IUserManagment userManagment
        
        ,IMapper mapper,IValidator<RegisterUser> RegisterValidator ,IConfirmation emailConfirmation,
        IValidator<LoginUser> LoginValidator, IValidationService validationService , ICloudService cloudService,
        IActivityLogService activityLogService,
        Domain.Interfaces.IGeneric<Organization> organizationManagement): IAuthServices
    {
        public async Task<bool> VerifyCurrentUserPasswordAsync(ClaimsPrincipal userClaims, string password)
        {
            if (userClaims == null || string.IsNullOrWhiteSpace(password))
                return false;

            var email = userClaims.FindFirst(ClaimTypes.Email)?.Value
                ?? userClaims.FindFirst("email")?.Value
                ?? userClaims.FindFirst("emailaddress")?.Value;

            if (string.IsNullOrWhiteSpace(email))
                return false;

            var user = await userManagment.GetUserByEmail(email);
            if (user == null)
                return false;

            return await userManagment.CheckPassword(user, password);
        }

        public async Task<ServiceResponse> AddRole(string roleName)
        {
            var response =await roleManagment.AddRole(roleName);
            if (!response.Succeeded)
            {
                return new ServiceResponse
                {
                    success = false,
                    message = "Failed to add role",
                    errors = response.Errors.Select(e => e.Description)
                };
            }
            return new ServiceResponse
            {
                success = true,
                message = "Role added successfully"
            };

        }

        public async Task<ServiceResponse> AddUserToRole(string UserId, string roleName, string? performedById = null, string? performedByName = null)
        {
            if (string.IsNullOrWhiteSpace(UserId))
            {
                return new ServiceResponse(false, "User id is required");
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                return new ServiceResponse(false, "Role name is required");
            }

            var targetUser = await userManagment.GetUserById(UserId);
            if (targetUser == null)
            {
                return new ServiceResponse(false, "User not found");
            }

            var currentRole = !string.IsNullOrWhiteSpace(targetUser.Email)
                ? await roleManagment.GetUserRole(targetUser.Email)
                : string.Empty;

            if (string.Equals(currentRole, roleName, StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceResponse(false, $"User already has role '{currentRole}'");
            }

            var response = await roleManagment.AddUserToRole(targetUser, roleName);
            if (!response.Succeeded)
            {
                return new ServiceResponse
                {
                    success = false,
                    message = "Failed to add user to role",
                    errors = response.Errors.Select(e => e.Description)
                };
            }
            await activityLogService.LogAsync(
                performedById,
                performedByName ?? "Admin",
                "RoleAssigned",
                "User",
                UserId,
                $"{DisplayName(targetUser)} assigned to role {roleName}");

            return new ServiceResponse
            {
                success = true,
                message = "User added to role successfully"
            };

        }

        public async Task<IEnumerable<GetUser>> GetAllUsers(string? roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                var users = await userManagment.GetAllUsers();
                var mappedUsers = mapper.Map<IEnumerable<GetUser>>(users);
                foreach(var user in mappedUsers)
                {
                    var userRole = await roleManagment.GetUserRole(user.Email);
                    user.role = userRole;
                    await FillOrganizationAsync(user);
                }
                return mappedUsers;
            }
            else
            {
                var users = await userManagment.GetAllUsers();
                var mappedUsers = mapper.Map<IEnumerable<GetUser>>(users);
                var filteredUsers = new List<GetUser>();
                foreach (var user in mappedUsers)
                {
                    var userRole = await roleManagment.GetUserRole(user.Email);
                    if (userRole.ToLower() == roleName.ToLower())
                    {
                        user.role = userRole;
                        await FillOrganizationAsync(user);
                        filteredUsers.Add(user);
                    }
                }
                return filteredUsers ;
            }
        }

        public async Task<GetUser> GetProfile(string userId)
        {
            var user = await userManagment.GetUserById(userId);
            if (user == null)
            {
                return null;
            }
            return await BuildUserProfile(user);
        }

        public async Task<ServiceResponse> UpdateProfile(string userId, UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "User is not authenticated");

            var user = await userManagment.GetUserById(userId);
            if (user == null)
                return new ServiceResponse(false, "User not found");

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.FullName = request.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber.Trim();

            if (request.ProfilePicture != null)
            {
                var details = new FileDetails
                {
                    FileName = $"{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_ProfilePicture",
                    Folder = "ProfilePicture"
                };
                var file = new AddCloudFile
                {
                    Details = details,
                    File = request.ProfilePicture
                };
                var uploadResult = await cloudService.UploadFileAsync(file);
                if (!uploadResult.success)
                    return new ServiceResponse(false, "Failed to upload profile image");

                user.ProfilePicture = details.FileName;
            }

            var updateResult = await userManagment.UpdateUser(user);
            if (!updateResult.Succeeded)
            {
                var errors = updateResult.Errors.Select(e => e.Description).ToList();
                return new ServiceResponse(false, "Profile update failed", errors: errors);
            }

            var profile = await BuildUserProfile(user);
            await activityLogService.LogAsync(user.Id, DisplayName(user), "ProfileUpdated", "User", user.Id, $"{DisplayName(user)} updated profile information");
            return new ServiceResponse(true, "Profile updated successfully", profile);
        }

        public async Task<ServiceResponse> ChangePassword(string userId, ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return new ServiceResponse(false, "User is not authenticated");

            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return new ServiceResponse(false, "Current password is required");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return new ServiceResponse(false, "New password is required");

            if (request.NewPassword != request.ConfirmPassword)
                return new ServiceResponse(false, "Confirm password must match new password");

            var user = await userManagment.GetUserById(userId);
            if (user == null)
                return new ServiceResponse(false, "User not found");

            var result = await userManagment.ChangePassword(user, request.CurrentPassword, request.NewPassword);
            if (result.Succeeded)
            {
                await activityLogService.LogAsync(user.Id, DisplayName(user), "PasswordChanged", "User", user.Id, $"{DisplayName(user)} changed password");
                return new ServiceResponse(true, "Password changed successfully");
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            var currentPasswordError = result.Errors.Any(e => e.Code == "PasswordMismatch");
            return currentPasswordError
                ? new ServiceResponse(false, "Current password is incorrect", errors: errors)
                : new ServiceResponse(false, "Password change failed", errors: errors);
        }

        private async Task<GetUser> BuildUserProfile(AppUser user)
        {
            var userRole = await roleManagment.GetUserRole(user.Email);
            var profile = new GetUser
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                phoneNumber = user.PhoneNumber,
                ProfilePicture = user.ProfilePicture,
                BirthDate = user.Birthdate,
                role = userRole,
                OrganizationId = user.OrganizationId
            };
            await FillOrganizationAsync(profile);
            return profile;
        }

        private async Task FillOrganizationAsync(GetUser user)
        {
            if (user.OrganizationId.HasValue)
            {
                var organization = await organizationManagement.GetByIdAsync(user.OrganizationId.Value);
                user.OrganizationName = organization?.Name ?? "EduVerseOrganization";
                return;
            }

            user.OrganizationName = "EduVerseOrganization";
        }

        private static string DisplayName(AppUser? user)
        {
            return user?.FullName ?? user?.UserName ?? user?.Email ?? "Unknown user";
        }

        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            var validationResponse = await validationService.ValidateAsync<LoginUser>(user, LoginValidator);
            if (!validationResponse.success)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Validation failed",
                    
                };
            }
            var mappedUser = mapper.Map<AppUser>(user);
            
            var isLoggedIn = await userManagment.LoginUser(mappedUser);
            if (!isLoggedIn)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Login failed"
                };
            }
            var _user = await userManagment.GetUserByEmail(user.Email);
            var claims = await userManagment.GetUserClaims(user.Email);
            var token = tokenManagment.GenerateToken(claims);
            var refreshToken = tokenManagment.GetRefreshTokenAsync();
            var test = await tokenManagment.ValidateRefreshToken(refreshToken);
            int addRefreshToken=0;
            if (test)
                addRefreshToken = await tokenManagment.UpdateRefreshToken(_user.Id, refreshToken);
            else
                addRefreshToken = await tokenManagment.AddRefreshToken(_user.Id, refreshToken);
            if (addRefreshToken <= 0)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Failed to add refresh token"
                };
            }
            return new LoginResponse
            {
                succeed = true,
                message = "Login successful",
                token = token,
                refreshToken = refreshToken
            };



        }
        


        public async Task<LoginResponse> RegisterUser(RegisterUser user)
        {
            var response = await validationService.ValidateAsync<RegisterUser>(user, RegisterValidator);
            if (!response.success)
            {

                return new LoginResponse
                {
                    succeed= false,
                    message = response.message,
                };
            }

            if (!DateOnly.TryParse(user.Birth, out var birthDate))
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Birth date is invalid"
                };
            }

            var normalizedRole = user.role.Trim().ToLowerInvariant() switch
            {
                "student" => "student",
                "instructor" => "instructor",
                _ => null
            };
            if (normalizedRole == null)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Public registration is available only for Student and Instructor accounts"
                };
            }

            user.Email = user.Email.Trim();
            user.UserName = user.UserName.Trim();
            user.FullName = user.FullName.Trim();
            var mappedUser = mapper.Map<AppUser>(user);
            mappedUser.Birthdate = birthDate;

            var confirmationResult = await emailConfirmation.GetConfirmationByEmail(user.Email);
            if (confirmationResult == null)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Request a confirmation code before registering"
                };
            }
            if (!string.Equals(confirmationResult.ConfirmationCode, user.ConfirmationCode, StringComparison.Ordinal))
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "Confirmation code is not correct"
                };
            }

            mappedUser.EmailConfirmed = true;
            if (user.ProfilePicture != null)
            {
                var details = new FileDetails
                {
                    FileName = $"{user.Email}_ProfilrPicture",
                    Folder = "ProfilePicture"
                };
                var file = new AddCloudFile
                {
                    Details = details,
                    File = user.ProfilePicture
                };
                var uploadResult = await cloudService.UploadFileAsync(file);
                if (!uploadResult.success)
                {
                    return new LoginResponse
                    {
                        succeed = false,
                        message = "Failed to upload profile image"
                    };
                }

                mappedUser.ProfilePicture = details.FileName;
            }
            var isRegistered = await userManagment.RegisterUser(mappedUser);
            if(isRegistered.Succeeded)
                await emailConfirmation.RemoveConfirmation(user.Email);
            if (!isRegistered.Succeeded)
            {
                var errors = isRegistered.Errors.Select(e => e.Description).ToList();
                return new LoginResponse(false, "Registration failed", errors: errors);
            }
            
            var _user = await userManagment.GetUserByEmail(user.Email);
           
            var roleAssign = await roleManagment.AddUserToRole(_user, normalizedRole);
            if (!roleAssign.Succeeded)
            {
                var removeUser = await userManagment.RemoveUser(user.Email);
                return new LoginResponse(false, message: "Role assignment failed", errors: roleAssign.Errors.Select(e => e.Description));
            }

            return new LoginResponse(
                succeed: true,
                message: "Registration successful. You can now sign in.");



        }

        public async Task<ServiceResponse> RemoveRole(string roleName)
        {
            var response = await roleManagment.DeleteRole(roleName);
            if (!response)
            {
                return new ServiceResponse
                {
                    success = false,
                    message = "Failed to remove role"
                };
            }
            return new ServiceResponse
            {
                success = true,
                message = "Role removed successfully"
            };
        }

        public async Task<LoginResponse> ReviveToken(string refreshtoken)
        {
            var validationResponse = await tokenManagment.ValidateRefreshToken(refreshtoken);
            if(!validationResponse)
                return new LoginResponse
                {
                    succeed = false,
                    message = "Invalid refresh token"
                };
            var userId = await tokenManagment.GetUserIdFromToken(refreshtoken);
            var user = await userManagment.GetUserById(userId);
            if (user == null)
                return new LoginResponse
                {
                    succeed = false,
                    message = "User not found"
                };
            var claims = await userManagment.GetUserClaims(user.Email);
            if (claims == null)
                return new LoginResponse
                {
                    succeed = false,
                    message = "Failed to get user claims"
                };
            var token = tokenManagment.GenerateToken(claims);
            var newRefreshToken = tokenManagment.GetRefreshTokenAsync();
            var updateRefreshToken = await tokenManagment.UpdateRefreshToken(userId, newRefreshToken);
            if (updateRefreshToken <= 0)
                return new LoginResponse
                {
                    succeed = false,
                    message = "Failed to update refresh token"
                };
            return new LoginResponse { message = "Token revived successfully", succeed = true, token = token, refreshToken = newRefreshToken };


        }

        public async Task<ConfirmEmail> SendConfirmationEmail(string email)
        {
            email = email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
            {
                return new ConfirmEmail
                {
                    Email = email,
                    ConfirmationCode = null
                };
            }

            var existingConfirmation = await emailConfirmation.GetConfirmationByEmail(email);
            string confirmationCode;
            if (existingConfirmation != null)
            {
                var isRemoved = await emailConfirmation.RemoveConfirmation(email);
                if (!isRemoved)
                {
                    return new ConfirmEmail
                    {
                        Email = email,
                        ConfirmationCode = null,

                    };
                }

            }
            
            
            confirmationCode = new Random().Next(100000, 999999).ToString();
            
            var confirmation = new EmailConfirmation
            {
                Email = email,
                ConfirmationCode = confirmationCode,
                
            };
            var result = await emailConfirmation.AddConfirmationCode(confirmation);
            if(!result)
            {
                return new ConfirmEmail
                {
                    Email = email,
                    ConfirmationCode = null,
                    
                };
            }
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("EduVerse1311@gmail.com", "xikj ywxu qcpu dlnb"),
                EnableSsl = true,
                Timeout = 20_000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress("EduVerse1311@gmail.com"),
                Subject = "Confirmation Code",
                Body = $"<h1>{confirmation.ConfirmationCode}</h1>",
                IsBodyHtml = true,
                
            };
            mailMessage.To.Add(confirmation.Email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage).WaitAsync(TimeSpan.FromSeconds(20));
                return mapper.Map<ConfirmEmail>(confirmation);
            }
            catch
            {
                await emailConfirmation.RemoveConfirmation(email);
                return new ConfirmEmail
                {
                    Email = email,
                    ConfirmationCode = null
                };
            }


        }
    }
}
