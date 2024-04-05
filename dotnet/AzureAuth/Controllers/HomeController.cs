using AzureAuth.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AzureAuth.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;
        protected readonly IConfiguration _config;
        private readonly string client_id;
        private readonly string tenant_id;
        private readonly string redirect_url;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration) {
            _logger = logger;
            _config = configuration;
            tenant_id = _config.GetValue<string>("azure_auth:tenant_id");
            client_id = _config.GetValue<string>("azure_auth:client_id");
            redirect_url = _config.GetValue<string>("azure_auth:redirect_url");
        }

        private string AutorizeUrl {
            get {
                return $"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/authorize?client_id={client_id}&response_type=id_token&redirect_uri={redirect_url}&response_mode=form_post&scope=openid%20offline_access%20user.read%20mail.read&state=12345&nonce=678910";
            }
        }

        private void AddViewData() {
            ViewData["autorize_url"] = AutorizeUrl
        }



        public IActionResult Index() {
            AddViewData();
            return View();
        }

        public IActionResult Privacy() {
            AddViewData();
            return View();

        }

        [HttpPost]
        public async Task<IActionResult> ReturnLogin() {
            AddViewData();

            var idToken = Request.Form["id_token"];
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            // Datos del usuario desde el token
            var userId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var userName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
            var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

            ViewData["email"] = email;
            //var a = await GetUserInfo(idToken);
            return View();

        }

        public async Task<string> GetUserInfo(string accessToken) {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            try {
                var url = $"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            } catch (HttpRequestException ex) {
                Console.WriteLine($"Error al obtener la información del usuario: {ex.Message}");
                return null;
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Get the user name, user domain and email of the user from the authentication claims
        /// </summary>
        /// <param name="user">Auth Claims</param>
        /// <returns>Azure AD</returns>
        public static UserAzureAD GetUserOnAzureAd(ClaimsPrincipal user) {
            var preferredUsernameClaim = user.Claims.FirstOrDefault(c => c.Type.Equals("preferred_username"));
            if (preferredUsernameClaim != null) {
                return new UserAzureAD {
                    user_name = user.Claims.FirstOrDefault(p => p.Type.Equals("name")).Value,
                    user_email = preferredUsernameClaim.Value,
                    user_domain = string.Format(@"cpiccr\{0}", preferredUsernameClaim.Value.Split('@')[0])
                };
            }
            return null; // Or throw an exception if preferred_username claim is required
        }

    }
}
