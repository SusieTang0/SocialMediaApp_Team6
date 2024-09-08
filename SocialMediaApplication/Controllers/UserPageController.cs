using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SocialMediaApplication.Models;
using SocialMediaApplication.Services;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SocialMediaApplication.Controllers
{
    public class UserPageController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public UserPageController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }
        
       
        [Authorize]
        public async Task<IActionResult> Index(string Id)
        {
            ViewData.Clear();
            string userId = HttpContext.Session.GetString("userId");
            ViewBag.UserId = userId;

            if (string.IsNullOrEmpty(userId))
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
            if (Id == null || Id == userId)
            {
                ViewBag.Owner = await _firebaseService.GetUserProfileAsync(userId);
                ViewBag.IsOwner = true;
            }
            else
            {
                ViewBag.Owner = await _firebaseService.GetUserProfileAsync(Id);
                ViewBag.IsOwner = false;
            }

            ViewBag.IsOwner = true;
            ViewBag.Owner = await _firebaseService.GetUserProfileAsync(userId);
            ViewBag.Follows = await _firebaseService.GetFollowedIdsSetAsync(userId);
            ViewBag.Users = await _firebaseService.GetUsersAsync();
            ViewBag.User = await _firebaseService.GetUserProfileAsync(userId);
            ViewBag.Page = "UserPage";
            var posts = await _firebaseService.GetPostlistsAsync(userId);
        
            return View(posts);
        }


        [HttpPost]
        public async Task<IActionResult> CreatePost(string content)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                // Handle empty content case
                ModelState.AddModelError(string.Empty, "Content cannot be empty.");
                return RedirectToAction("Index"); // Redirect to the appropriate action or return a view with error messages
            }

            User thisUser = await _firebaseService.GetUserProfileAsync(userId);
            if (thisUser == null)
            {
                // Handle the case where the user profile cannot be retrieved
                return NotFound("User not found.");
            }

            try
            {
                await _firebaseService.AddPost(content, userId, thisUser.Name, thisUser.ProfilePictureUrl);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, "Internal server error. Please try again later." + ex.Message);
            }

            return RedirectToAction("Index", new { Id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePost(string postId, string content)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(postId) && string.IsNullOrEmpty(content))
            {
                // Handle the case where postId is null or empty
                return BadRequest("Post ID cannot be null or empty.");
            }


            await _firebaseService.SavePostAsync(postId, content);




            // Redirect to the index action with the correct userId
            return RedirectToAction("Index", new { Id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(string id)
        {
            string userId = HttpContext.Session.GetString("userId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(id))
            {
                // Handle the error or return a view with an error message
                return BadRequest("Post ID cannot be null or empty.");
            }
            
            await _firebaseService.DeletePostAsync(id);

            return RedirectToAction("Index", new { Id = userId });
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

        [HttpPost("following")]
        public async Task<IActionResult> Following(string followOwnerId, string followUserId)
        {
            if (string.IsNullOrEmpty(followOwnerId) || string.IsNullOrEmpty(followUserId))
            {
                // Log details or add additional debugging here
                throw new ArgumentException("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.AddFollowAsync(followOwnerId, followUserId);
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

            return RedirectToAction("Index", new { id = followOwnerId });
        }

        [HttpPost("unfollowing")]
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

            return RedirectToAction("Index", new { id = ownerId });
        }
    }
}
