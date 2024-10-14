using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using ShopX_API.Models;
using ShopX_API.Models.DTO;
using ShopX_API.Services;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShopX_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext db;
        private readonly EmailSender emailSender;

        public AccountController(IConfiguration configuration, ApplicationDbContext db, EmailSender emailSender)
        {
            this.configuration = configuration;
            this.db = db;
            this.emailSender = emailSender;
        }






        /*

        [HttpGet("TestToken")]
        public IActionResult TestToken()
        {
            var user = new User()
            {
                Id = 1,
                Role = "admin"
            };

            string jwt = CreateJWToken(user);
            var response = new {JWToken =  jwt};

            return Ok(response);
        }
        */



        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id", "" + user.Id),
                new Claim("role", user.Role)
            };

            string strKey = configuration["JwtSettings:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                 issuer : configuration["JwtSettings:Issuer"],
                 audience : configuration["JwtSettings:Audience"],
                 claims : claims,
                 expires : DateTime.Now.AddDays(1),
                 signingCredentials : creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);    

            return jwt;
        }












        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            // Check if the email address is already used
            var emailCount = await db.Users.CountAsync(u => u.Email == userDto.Email);
            if (emailCount > 0)
            {
                ModelState.AddModelError("Email", "This email is already used");
                return BadRequest(ModelState);
            }

            // encrypt the password
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            // Create new account
            var user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Password = encryptedPassword,
                Role = "client",
                CreatedAt = DateTime.Now
            };

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();

            var jwt = CreateJWToken(user);

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,   
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);
        }














        [HttpPost("Login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u. Email == email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "invalid email or password");
                return BadRequest(ModelState);
            }

            // verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "invalid password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = DateTime.Now
            };

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);

        }






        /*
        [Authorize]
        [HttpGet("AuthorizeAuthenticatedUsers")]
        public IActionResult AuthorizeAuthenticatedUsers()
        {
            return Ok("You are authorized");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("AuthorizeAdmin")]
        public IActionResult AuthorizeAdmin()
        {
            return Ok("You are authorized");
        }


        [Authorize(Roles = "admin, seller")]
        [HttpGet("AuthorizeAdminAndSeller")]
        public IActionResult AuthorizeAdminAndSeller()
        {
            return Ok("You are authorized");
        }
        */








        /*
        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        {
            var identity = User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                Dictionary<string, string> claims = new Dictionary<string, string>();

                foreach (var claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

                return Ok(claims);
            }

            return Ok();
        }
        */











        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound();
            }

            // delete any old password reset request
            var oldPasswordReset = await db.ResetPasswords.FirstOrDefaultAsync(r => r.Email == email);
            if (oldPasswordReset != null)
            {
                // delete old password reset request
                db.Remove(oldPasswordReset);
            }

            // Create Password Reset Token
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var passwordReset = new ResetPassword()
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now
            };

            await db.ResetPasswords.AddAsync(passwordReset);
            await db.SaveChangesAsync();

            // send the Password Reset Token by email to the user
            string emailSubject = "Password Reset";
            string username = user.FirstName + " " + user.LastName;
            string emailMessage = "Dear " + username + "\n" + " We recieved your password reset request.\n" + " Please copy the following token and paste it in the Password Reset Form:\n" + token + " \n\n" + " Best Regards\n";

            emailSender.SendEmail(emailSubject, email, username, emailMessage).Wait();

            return Ok(); 
        }


















        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string token, string password)
        {
            var passwordReset = await db.ResetPasswords.FirstOrDefaultAsync(r => r.Token == token);

            if (passwordReset == null)
            {
                ModelState.AddModelError("Token", "Wrong Or Expired Token");
                return BadRequest(ModelState);
            }

            // read the user
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == passwordReset.Email);
            if (user == null)
            {
                ModelState.AddModelError("Token", "Wrong Or Expired Token");
                return BadRequest(ModelState);
            }

            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);

            // save the new encrypted password
            user.Password = encryptedPassword;

            // delete old token from database
            db.ResetPasswords.Remove(passwordReset);

            await db.SaveChangesAsync();

            return Ok();
        }










        [Authorize]
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            int id = JwtReader.GetUserId(User);


            var user = await db.Users.FindAsync(id);
            if (user == null) 
            {
                return Unauthorized();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
            };

            return Ok(userProfileDto);
        }











        [Authorize]
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = JwtReader.GetUserId(User);

            var user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return Unauthorized();
            }

            // update user profile
            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone ?? "";
            user.Address = userProfileUpdateDto.Address;


            await db.SaveChangesAsync();

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
            };

            return Ok(userProfileDto);
        }






        








        [Authorize]
        [HttpPut("UpdatePassword")]
        public async Task<IActionResult> UpdatePassword([Required, MinLength(8), MaxLength(100)]string password)
        {
            int id = JwtReader.GetUserId(User);

            var user = await db.Users.FindAsync( id);
            if (user == null)
            {
                return Unauthorized();
            }

            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), password);

            // update the user password
            user.Password = encryptedPassword;   

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}
