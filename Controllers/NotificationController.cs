using EPEAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EPEAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly EpeDbContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(EpeDbContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Helper method to get the current user ID from authentication
        private long GetCurrentUserId()
        {
            // First try to get from session (used by some controllers)
            var sessionUserId = HttpContext.Session.GetInt32("UserId");
            if (sessionUserId.HasValue)
            {
                return sessionUserId.Value;
            }

            // Try to get from token header (used by TokenAuthenticationFilter)
            if (HttpContext.Request.Headers.ContainsKey("token"))
            {
                var token = HttpContext.Request.Headers["token"].ToString();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                    if (jsonToken != null)
                    {
                        var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                        if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                        {
                            return userId;
                        }
                    }
                }
                catch
                {
                    // If JWT parsing fails, try the custom token format used by TokenManager
                    try
                    {
                        var fromBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                        var credentials = fromBase64.Split(':');
                        if (credentials.Length >= 3 && credentials[1].EndsWith("tsaf"))
                        {
                            if (long.TryParse(credentials[0], out var userId))
                            {
                                return userId;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and continue to return 0
                    }
                }
            }

            // Try to get from Authorization header (Bearer token)
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
                        if (jsonToken != null)
                        {
                            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
                            {
                                return userId;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore and continue to return 0
                    }
                }
            }

            // Return 0 as fallback (should be improved in a real application)
            return 0;
        }

        // GET: api/notification
        [HttpPost]
        public async Task<ActionResult<IEnumerable<EpeNotification>>> GetNotifications(UseridRequest userid)
        {
            Console.WriteLine(userid);

            var userId = userid.UserId;

            _logger.LogInformation(" GetNotifications HIT: {UserId}", userid.UserId);

            Console.WriteLine(userId);

            return await _context.EpeNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        // GET: api/notification/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount(UseridRequest userid)
        {
            var userId = userid.UserId;
            var count = await _context.EpeNotifications
                .CountAsync(n => n.UserId == userId && n.IsRead == 0);
            return count;
        }

        // POST: api/notification/{id}/read
        [HttpPost("{id}/read")]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var notification = await _context.EpeNotifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = 1 ;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/notification/read-all
        [HttpPost("read-all")]
        public async Task<ActionResult> MarkAllAsRead(UseridRequest userid)
        {
            var userId = userid.UserId;
            var notifications = await _context.EpeNotifications
                .Where(n => n.UserId == userId && n.IsRead == 0)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = 1;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}