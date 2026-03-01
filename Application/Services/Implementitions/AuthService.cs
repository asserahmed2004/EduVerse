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

namespace Application.Services.Implementitions.Auth
{
    public class AuthService (IRoleManagment roleManagment 
        , ITokenManagment tokenManagment , IUserManagment userManagment
        
        ,IMapper mapper,IValidator<RegisterUser> RegisterValidator ,IConfirmation emailConfirmation,
        IValidator<LoginUser> LoginValidator, IValidationService validationService): IAuthServices
    {
        public async Task<ServiceResponse> AddRole(string roleName)
        {
            var response =await roleManagment.AddRole(roleName);
            if (!response)
            {
                return new ServiceResponse
                {
                    success = false,
                    message = "Failed to add role"
                };
            }
            return new ServiceResponse
            {
                success = true,
                message = "Role added successfully"
            };

        }

        public async Task<ServiceResponse> AddUserToRole(string UserId, string roleName)
        {
            var response = await roleManagment.AddUserToRole(new AppUser { Id = UserId }, roleName);
            if (!response)
            {
                return new ServiceResponse
                {
                    success = false,
                    message = "Failed to add user to role"
                };
            }
            return new ServiceResponse
            {
                success = true,
                message = "User added to role successfully"
            };

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
                    message = "Not Valid",
                };
            }
            var mappedUser = mapper.Map<AppUser>(user);
            var confirmationResult = await emailConfirmation.GetConfirmationByEmail(user.Email);
            if (confirmationResult == null)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "confirmation is not correct"
                };
            }
            if (confirmationResult.ConfirmationCode != user.ConfirmationCode)
            {
                return new LoginResponse
                {
                    succeed = false,
                    message = "confirmation code is not correct"
                };
            }
            var mappedconfirmation = mapper.Map<EmailConfirmation>(confirmationResult);



            var isRegistered = await userManagment.RegisterUser(mappedUser);
            if(isRegistered)
                await emailConfirmation.RemoveConfirmation(user.Email);
            if (!isRegistered)
            {
                return new LoginResponse(false, "Registration failed");
            }
            
            var _user = await userManagment.GetUserByEmail(user.Email);
           
            var roleAssign = await roleManagment.AddUserToRole(_user, user.role);
            if (!roleAssign)
            {
                var removeUser = await userManagment.RemoveUser(user.Email);
                return new LoginResponse(false, message: "Role assignment failed");
            }
            var login= await LoginUser(new LoginUser { Email = user.Email, Password = user.Password });
            return login;



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
            var existingConfirmation = await emailConfirmation.GetConfirmationByEmail(email);
            string confirmationCode;
            if (existingConfirmation != null)
            {
                confirmationCode = existingConfirmation.ConfirmationCode;
            }
            else
            {
                confirmationCode = new Random().Next(100000, 999999).ToString();
            }
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
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("EduVerse1311@gmail.com", "xikj ywxu qcpu dlnb"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("EduVerse1311@gmail.com"),
                Subject = "Confirmation Code",
                Body = $"<h1>{confirmation.ConfirmationCode}</h1>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(confirmation.Email);

            smtpClient.Send(mailMessage);



            return mapper.Map<ConfirmEmail>(confirmation);


        }
    }
}
