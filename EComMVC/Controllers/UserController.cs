using EComMVC.Data;
using EComMVC.Models;
using EComMVC.Models.BO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EComMVC.Controllers
{
    public class UserController : Controller
    {
        private readonly DBContextConnection _dbContextConnection;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserController> _logger;
        private readonly IMemoryCache _memoryCache;



        private static string JwtKey = "C1CF4B7DC4C4175B6618DE4F55CA4";
        private static string JwtAudience = "SecureApiUser";
        private static string JwtIssuer = "https://localhost:44381";
        private static Double JwtExpireDays = 30;

        public UserController(DBContextConnection dB, IConfiguration configuration, ILogger<UserController> logger, IMemoryCache memoryCache)

        {
            this._dbContextConnection = dB;
            this._configuration = configuration;
            this._logger = logger;
            this._memoryCache = memoryCache;
         }


        [HttpGet]
        public ActionResult FormAuth()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Authenticate(UserAuthViewModel userA)
        {
            try
            {
                var user = _dbContextConnection.users.FirstOrDefault(u => u.Email == userA.Email && u.Password == userA.Password);

                if (user != null)
                {

                    var token = GenerateJwtToken(user);
                    //ecrire dans le fichier de journalisation si le user arrive à s'authentifier
                    _logger.LogInformation($"User '{user.Email}' successfully authenticated. Token: {token}");

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("AccessToken", new JwtSecurityTokenHandler().WriteToken(token))
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                    RafraichirCache();
                    if (user.role == "user")
                    { return RedirectToAction("Index", "Product"); }
                    else if(user.role=="admin")
                    {
                        return RedirectToAction("Index2", "Product");
                    }
                    else
                    {
                        return View("FormAuth");
                    }

                }
                else
                {
                    //ecrire dans le fichier de journalisation si le user n'est pas trouve
                    _logger.LogWarning($"Failed authentication attempt for user '{userA.Email}'.");
                    return View("FormAuth");
                }
            }
            catch (Exception ex)
            {
                //ecrire dans le fichier de journalisation qu'il y a une exception
                _logger.LogError($"An error occurred during authentication: {ex.Message}");
                return View("FormAuth");
            }
        }




        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //ecrire dans le fichier de journalisation que le user est deconnecté
            _logger.LogInformation("User logged out.");
            return RedirectToAction("FormAuth", "User");
        }


        private JwtSecurityToken GenerateJwtToken(User user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddMinutes(Convert.ToDouble(JwtExpireDays));

                var token = new JwtSecurityToken(
                    issuer: JwtIssuer,
                    audience: JwtAudience,
                    claims: new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Email)
                    },
                    expires: expires,
                    signingCredentials: creds
                );

                return token;
            }
            catch (Exception ex)
            {
                //ecrire dans le fichier de journalisation qu'il y a un erreur qu niveau de la generation de jwt token
                _logger.LogError($"An error occurred while generating JWT token: {ex.Message}");
                throw; 
            }
        }
         
        public IActionResult RafraichirCache()
        {
            // Effacez le cache pour forcer une nouvelle récupération des données lors de la prochaine demande
            _memoryCache.Remove("keyA");
            _memoryCache.Remove("key");
            _logger.LogInformation($"rafraichir les donnees pour un ajout");
            return RedirectToAction("Index");
        }
    }
}
