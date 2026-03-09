using FastNetCoreLibrary;
using System.Text;
using EPEAPI.Models;
using EPEAPI.TokenAuthentication;

namespace EPEAPI.TokenAuthentication
{
    public class TokenManager : ITokenManager
    {

        private EpeDbContext _dbContext;

        private string tokenresult = string.Empty;
        public TokenManager(EpeDbContext dbContext)
        {
            _dbContext = dbContext;

        }

        public bool Authenticate(string username, string password)
        {
            string IncPassword = string.Empty;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                var userEmployee = _dbContext.CoreVUsers.Where(m => m.Username == username && m.Status == "A");

                var debugPassword = _dbContext.CoreSystems.Where(m => m.Debugpassword == password);

                if (debugPassword.Count() != 0)
                {
                    var userdebugpass = _dbContext.CoreVUsers.Where(u => u.Username == username).FirstOrDefault();
                    IncPassword = userdebugpass.Userpass;
                }
                else
                {
                    IncPassword = PasswordEncryptor.HashPassword(password).ToString();
                }

                var verifyUser = _dbContext.CoreVUsers
                         .FirstOrDefault(c => c.Username == username &&
                                         c.Userpass == IncPassword);

                if (verifyUser != null)
                {
                    var userid = verifyUser.UserId;
                    NewToken(userid.ToString());
                    return true;
                }
            }

            return false;

        }

        //TO RETURN THE FROM THE CONTROLLER 
        //NEED TO UPDATE 
        public string tokensresult()
        {
            var tokentodisplay = tokenresult;
            return tokentodisplay;
        }

        public TokenList NewToken(string userId)
        {
            var tokenExpireinHrs = Int32.Parse(tokenExpiry);
            //var tokenExpireinHrs = 24;

            var token = new TokenList
            {
                validtoken = Guid.NewGuid().ToString(),
                expirydate = DateTime.Now.AddHours(tokenExpireinHrs)
            };

            byte[] byteToken = Encoding.UTF8.GetBytes(userId + ":" + token.validtoken + String.Concat("tsaf") + ":" + token.expirydate);
            string ConvertedToken = Convert.ToBase64String(byteToken).ToString();

            var toAddtoken = _dbContext.Tokens.Add(new Token
            {
                UserId = userId.ToString(),
                AuthToken = ConvertedToken,
                ExpiresOn = token.expirydate,
                IssuedOn = DateTime.Now,

            });

            tokenresult = ConvertedToken;
            tokensresult();

            _dbContext.SaveChanges();

            return token;
        }

        public bool VerifyToken(string token)
        {
            try
            {
                var FromBase64 = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var credentials = FromBase64.Split(':');
                if (credentials[1].Substring(credentials[1].Length - 4).Equals("tsaf")) //if token came from fast
                {

                    var tokenList = _dbContext.Tokens.FirstOrDefault(t => t.AuthToken == token);
                    if (tokenList != null)
                    {
                        if (tokenList.ExpiresOn > DateTime.Now)
                        {
                            return true;
                        }
                        else
                        {
                            tokenList.IsExpire = true;
                            _dbContext.SaveChanges();
                            return false;
                        }

                    }
                }

                return false;

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
