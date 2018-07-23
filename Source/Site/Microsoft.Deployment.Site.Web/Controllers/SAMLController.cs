using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Deployment.Site.Web.Controllers
{
    public class SAMLController : Controller
    {
        [Route("saml/redirect")]
        public IActionResult Redirect()
        {
            string samlResponse = Request.Form["SAMLResponse"];
            if(!string.IsNullOrWhiteSpace(samlResponse))
            {
                CookieOptions options = new CookieOptions {
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    Secure = true
                    //HttpOnly = true //TODO: App logic prevents this. Possible XSS attack target.
                };
                Response.Cookies.Append("samlAuthCode", samlResponse, options);
            }

            return Redirect(Request.Scheme + "://" + Request.Host + Request.PathBase + "/redirect.html");
        }
    }
}
