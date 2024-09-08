using Firebase.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialMediaApplication.Models;
using SocialMediaApplication.Services;

namespace SocialMediaApplication.Controllers
{
    [Route("Square")]
    public class SquareController : Controller
    {
        private readonly ILogger<SquareController> _logger;
        private readonly FirebaseService _firebaseService;

        public SquareController(FirebaseService firebaseService)
        {

            _firebaseService = firebaseService;
           
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
           
            string userId = HttpContext.Session.GetString("userId");
            HttpContext.Session.SetString("pageType", "Square");
            ViewBag.UserId = userId;
            if (string.IsNullOrEmpty(userId))
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
           
            ViewBag.Owner = await _firebaseService.GetUserProfileAsync(userId);
                
           
              
            ViewBag.IsOwner = true;
            
            ViewBag.Page = "Square";
            var posts = await _firebaseService.GetAllPostsAsync();
            ViewBag.Users = await _firebaseService.GetUsersAsync();
            ViewBag.Follows = await _firebaseService.GetFollowedIdsSetAsync(userId);
            ViewBag.User = await _firebaseService.GetUserProfileAsync(userId);
            return View(posts);
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


        [HttpPost("square-following")]
        public async Task<IActionResult> Following(string ownerId, string userId)
        {
           
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId))
            {
               
                throw new ArgumentException("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.AddFollowAsync(ownerId, userId);
            }
            catch (ArgumentException ex)
            {
                
                throw new ApplicationException("Follow error: " + ex.Message);
            }
            catch (Exception ex)
            {
               
                throw new ApplicationException("An unexpected error occurred: " + ex.Message);
            }

            return RedirectToAction("Index");
        }


        [HttpPost("square-unfollowing")]
        public async Task<IActionResult> Unfollowing(string ownerId, string userId)
        {
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId))
            {
                // Log details or add additional debugging here
                return BadRequest("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.DeleteFollowAsync(ownerId, userId);
            }
            catch (ArgumentException ex)
            {
                // Log the exception or handle it accordingly
                return BadRequest(ex.Message);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(string postId)
        {
            string userId = HttpContext.Session.GetString("userId");
            var user = await _firebaseService.GetUserProfileAsync(userId);
            string userName = user.Name;
            await _firebaseService.LikePost(postId, userId, userName);
            string page = ViewBag.page;

            return RedirectToAction("Index", "UserPage");
        }

        [HttpPost]
        public async Task<IActionResult> UnlikePost(string postId)
        {
            string userId = HttpContext.Session.GetString("userId");
            await _firebaseService.UnlikePost(postId, userId);
            string page = ViewBag.page;

            return RedirectToAction("Index", "UserPage");
        }

        [HttpPost]
        public async Task<IActionResult> LikeComment(string postId, string commentId)
        {
            string userId = HttpContext.Session.GetString("userId");
            var user = await _firebaseService.GetUserProfileAsync(userId);
            string userName = user.Name;
            await _firebaseService.LikeComment(postId, commentId, userId, userName);
            string page = ViewBag.page;

            return RedirectToAction("Index", "UserPage");
        }

        [HttpPost]
        public async Task<IActionResult> UnlikeComment(string postId, string commentId)
        {
            string userId = HttpContext.Session.GetString("userId");
            await _firebaseService.UnlikeComment(postId, commentId, userId);
            string page = ViewBag.page;

            return RedirectToAction("Index", "UserPage");
        }
    }
}
