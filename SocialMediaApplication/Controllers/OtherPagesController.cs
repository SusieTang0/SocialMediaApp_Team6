using Firebase.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SocialMediaApplication.Models;
using SocialMediaApplication.Services;

namespace SocialMediaApplication.Controllers
{
    [Route("OtherPage")]
    public class OtherPagesController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public OtherPagesController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Index(string id)
        {
            string userId = HttpContext.Session.GetString("userId");
            HttpContext.Session.SetString("pageType", "OtherPages");
            ViewBag.UserId = userId;
            if (string.IsNullOrEmpty(userId))
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
    
            ViewBag.IsOwner = false;
            ViewBag.Follows = await _firebaseService.GetFollowedIdsSetAsync(userId);
            ViewBag.Users = await _firebaseService.GetUsersAsync();
            ViewBag.Owner = await _firebaseService.GetUserProfileAsync(id);
            ViewBag.User = await _firebaseService.GetUserProfileAsync(userId);
            
            var posts = await _firebaseService.GetPostlistsAsync(id);
         
            return View(posts);
        }


        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("following")]
        public async Task<IActionResult> Following(string ownerId, string userId)
        {
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId))
            {
                // Log details or add additional debugging here
                throw new ArgumentException("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.AddFollowAsync(ownerId, userId);
            }
            catch (ArgumentException ex)
            {
                // Log the exception or handle it accordingly
                throw new ApplicationException("Follow error: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                throw new ApplicationException("An unexpected error occurred: " + ex.Message);
            }

            return RedirectToAction("Index", new { id = ownerId });
        }

        [HttpPost("unfollowing")]
        public async Task<IActionResult> Unfollowing(string ownerId, string userId)
        {
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId))
            {
                // Log details or add additional debugging here
                throw new ArgumentException("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.DeleteFollowAsync(ownerId, userId);
            }
            catch (ArgumentException ex)
            {
                // Log the exception or handle it accordingly
                throw new ApplicationException("Follow error: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                throw new ApplicationException("An unexpected error occurred: " + ex.Message);
            }

            return RedirectToAction("Index", new { id = ownerId });
        }

    }

}
