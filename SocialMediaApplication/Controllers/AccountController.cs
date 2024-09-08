using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SocialMediaApplication.Models;
using Firebase.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;


namespace SocialMediaApplication.Services
{

    public class AccountController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public AccountController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, IFormFile profilePicture)
        {
            try
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();

                // Proceed with registration if email does not exist
                var authLink = await _firebaseService.RegisterUser(email, password);

                string imageUrl = null;

                if (profilePicture != null)
                {
                    using var stream = profilePicture.OpenReadStream();
                    imageUrl = await _firebaseService.UploadProfilePictureAsync(stream, profilePicture.FileName);
                }

                var userProfile = new SocialMediaApplication.Models.User
                {
                    UserId = authLink.User.LocalId,
                    Email = email,
                    Name = email,
                    Bio = "This is your bio. You can update it later.",
                    ProfilePictureUrl = imageUrl ?? "https://firebasestorage.googleapis.com/v0/b/socialmediaapp-ac715.appspot.com/o/profile_pictures%2Ffemale2.jpg?alt=media&token=e8588357-8fb9-4439-a772-56e8c24bf9b5" // Fallback to default avatar if no picture uploaded
                };

                await _firebaseService.SaveUserProfileAsync(authLink.User.LocalId, userProfile);

                HttpContext.Session.SetString("userId", authLink.User.LocalId);

                return RedirectToAction("Index", "UserPage");
            }
            catch (FirebaseAuthException ex)
            {
                // Handle Firebase-specific exceptions
                ViewBag.ErrorMessage = ex.Message;
                return View("Register");
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                // Return a 200 status code with an error message
                ViewBag.ErrorMessage = "This email already has an account. Please Login.";
                return View("Register");
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var authLink = await _firebaseService.LoginUser(email, password);
                // Store userId in session or cookies
                HttpContext.Session.SetString("userId", authLink.User.LocalId);
                var claims = new List<Claim>
               {
                    new Claim(ClaimTypes.Name, email)

                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return RedirectToAction("Index", "UserPage");
            }

            catch (Exception ex)
            {
                // Log the exception if needed
                // Return a 200 status code with an error message
                ViewBag.ErrorMessage = "Email does not exist or the password is incorrect";
                return View("Login");
            }



        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Retrieve the user profile from the database, not the Firebase.Auth.User object
            var profile = await _firebaseService.GetUserProfileAsync(userId);
            if (profile == null)
            {
                // Handle the case where the profile is not found
                return RedirectToAction("Error");
            }

            return View(profile); // Pass the SocialMediaApplication.Models.User object to the view
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            // Retrieve the user profile to populate the form
            var profile = await _firebaseService.GetUserProfileAsync(userId);
            if (profile == null)
            {
                return RedirectToAction("Error");
            }

            return View(profile); // Return the view with the user profile data
        }

        [HttpPost("Account/UpdateProfile")]
        public async Task<IActionResult> UpdateProfile(string name, string bio, IFormFile profilePicture, string existingProfilePictureUrl)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            string imageUrl = existingProfilePictureUrl;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                using var stream = profilePicture.OpenReadStream();
                imageUrl = await _firebaseService.UploadProfilePictureAsync(stream, profilePicture.FileName);
            }

            var userProfile = new Models.User
            {
                UserId = userId,
                Name = name,
                Bio = bio,
                ProfilePictureUrl = imageUrl
            };

            await _firebaseService.SaveUserProfileAsync(userId, userProfile);

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Sign out the user from the authentication system
            await HttpContext.SignOutAsync();

            // Redirect to the Home/Index page
            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string resetEmail)
        {
            try
            {

                await _firebaseService.SendPasswordResetEmailAsync(resetEmail);


                TempData["ResetMessage"] = "A password reset link has been sent to your email.";

                return RedirectToAction("Login", new { email = resetEmail });
            }
            catch (Exception ex)
            {
                TempData["ResetErrorMessage"] = ex.Message;

                return RedirectToAction("ResetPassword");
            }
        }
    }
}
