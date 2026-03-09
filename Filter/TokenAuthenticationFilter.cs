using EPEAPI.TokenAuthentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CVAPI.Filter
{
    public class TokenAuthenticationFilter : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {

         
                var tokenManager = (ITokenManager)context.HttpContext.RequestServices.GetService(typeof(ITokenManager));

                var result = true;
                if (!context.HttpContext.Request.Headers.ContainsKey("token") || !context.HttpContext.Request.Headers.ContainsKey("Token"))
                    result = false;

                string token = string.Empty;

                if (result)
                {
                    token = context.HttpContext.Request.Headers.First(x => x.Key == "token" || x.Key == "Token").Value;
                    if (!tokenManager.VerifyToken(token))
                        result = false;


                }

                if (!result) 
                {

                    context.ModelState.AddModelError("Unauthorized", "You are not authorized");
                    context.Result = new UnauthorizedObjectResult(context.ModelState);
                
            }

        
        }
    }
}
