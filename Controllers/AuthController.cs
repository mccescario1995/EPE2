using EPEAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EPEAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly EpeDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(EpeDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")] // not in use 
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.CoreVUsers.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null || string.IsNullOrEmpty(user.Userpass))
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Compute MD5 hash of password
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
            if (hashString != user.Userpass.ToLower())
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
        }

        // Generate JWT
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Userrole),
            new Claim("Employeename", user.Employeename),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            Token = tokenString,
            User = new
            {
                user.UserId,
                user.Username,
                user.Userrole,
                user.Employeename,
                user.Emailaddress
            }
        });
    }



    [HttpGet("refresh")]
    public IActionResult Me()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return Unauthorized("Session expired or not logged in");
        }

        return Ok(new
        {
            UserId = userId,
            Username = HttpContext.Session.GetString("Username"),
            Role = HttpContext.Session.GetString("UserRole"),
            Profile = JsonSerializer.Deserialize<object>(
                HttpContext.Session.GetString("UserProfile") ?? "{}"
            )
        });
    }


    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return Ok(new { message = "Logged out successfully" });
    }
}
