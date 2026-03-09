using EPEAPI.Models;
using EPEAPI.TokenAuthentication;
using FastNetCoreLibrary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController : ControllerBase
    {
        Response response = new Response();
        private readonly IConfiguration _configuration;
        private readonly EpeDbContext _context;
        private readonly ITokenManager tokenManager;

        public AccessController(IConfiguration configuration, EpeDbContext contextdb, ITokenManager tokenManager)
        {
            _context = contextdb;
            _configuration = configuration;
            this.tokenManager = tokenManager;
        }


        // Access
        [HttpPost]
        public IActionResult Login([FromBody] Login login)
        {
            var response = new Response();

            var user = _context.CoreVUsers
                .Where(u => u.Username == login.email)
                .Select(u => new
                {
                    u.Hashcode,
                    u.UserId,
                    u.Username,
                    u.Userpass,
                    u.Status,
                    u.Userrole,
                    u.EmplId
                })
                .AsNoTracking()
                .FirstOrDefault();

            // Allow login if debug password matches, or if normal password matches
            if (!tokenManager.Authenticate(login.email, login.password))
            {
                response.status = "FAILURE";
                response.message = "Invalid username or password";
                return BadRequest(response);
            }

            if (user.Status == "I")
            {
                response.status = "FAILURE";
                response.message = "Account is inactive";
                return BadRequest(response);
            }

            var employee = _context.CoreVEmployeeDetails
                .Where(e => e.EmplId == user.EmplId)
                .Select(e => new
                {
                   
                    e.Employeename2,
                    e.Corporate,
                    e.Positionname,
                    e.Departmentname,
                    e.Sex
                })
                .AsNoTracking()
                .FirstOrDefault();

            var token = tokenManager.tokensresult();

            var profile = new
            {              
                
                user.UserId,
                user.Username,
                user.EmplId,
                employee?.Employeename2,
                employee?.Corporate,
                employee?.Positionname,
                employee?.Departmentname,
                employee?.Sex,
                user.Hashcode,
            };

            // STORE SESSION
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.Userrole);
            HttpContext.Session.SetString(
                "UserProfile",
                JsonSerializer.Serialize(profile)
            );

            //tokenManager.Authenticate(login.email, login.password);
            //var token = tokenManager.tokensresult();

            response.stringParam1 = token; // thiss should be token
            response.objParam1 = user.Username;
            response.status = "SUCCESS";
            response.Profile = profile;
            response.UserId = user.UserId;
            response.Username = user.Username;
            response.UserRole = user.Userrole;


            return Ok(response);
        }
    }

    public class Login
    {
        [Required]
        public string email { get; set; }
        [Required]
        public string password { get; set; }
    }


    public class Response
    {
        public string status { get; set; }
        public string message { get; set; }
        public string stringParam1 { get; set; }
        public string stringParam2 { get; set; }
        public string objParam1 { get; set; }
        public object Profile { get; set; }
        public object UserId { get; set; }
        public object Username { get; set; }
        public object UserRole { get; set; }
    }
}