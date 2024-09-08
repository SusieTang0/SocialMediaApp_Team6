using Firebase.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SocialMediaApplication.Models;
using SocialMediaApplication.Services;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace SocialMediaApplication.Controllers
{
    [Route("Follows")]
    public class FollowController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public FollowController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string id ,string theType)
        {
            // Retrieve userId from session
            string userId = HttpContext.Session.GetString("userId");
            HttpContext.Session.SetString("pageType", "Follow");

            // If user is not logged in, redirect to login
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(theType))
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            // Determine if viewing own profile or someone else's
           
            if (id.Equals(userId))
            {
                ViewBag.Owner = await _firebaseService.GetUserProfileAsync(userId);
                ViewBag.IsOwner = true;
            }
            else
            {

                ViewBag.Owner = await _firebaseService.GetUserProfileAsync(id);
                ViewBag.IsOwner = false;
            }

            ViewBag.TheType= theType;
            // If no specific type, load the default user profile view
            ViewBag.Follows = await _firebaseService.GetFollowedIdsSetAsync(userId);
            ViewBag.User = await _firebaseService.GetUserProfileAsync(userId);

            if (theType.Equals("followings"))
            {
                var follows = await _firebaseService.GetFollowedUsersAsync(id);
                return View(follows);
            }
            else
            {
                var follows = await _firebaseService.GetFollowerUsersAsync(id);
                return View(follows);
            }

            
        }

      


        [HttpPost("following")]
        public async Task<IActionResult> Following(string ownerId, string userId,string followedUserId)
        {
           
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(followedUserId))
            {
                // Log details or add additional debugging here
                throw new ArgumentException("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.AddFollowAsync(followedUserId, userId);
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

            string theType =ViewBag.TheType;
            if (!String.IsNullOrEmpty(theType))
            {
                return RedirectToAction("Index", new { id = ownerId, type = theType });
            }
            return RedirectToAction("Index","UserPage", new { id = ownerId  });


        }

        [HttpPost("unfollowing")]
        public async Task<IActionResult> Unfollowing(string ownerId, string userId,string followedUserId)
        {
            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(followedUserId))
            {
                // Log details or add additional debugging here
                return BadRequest("Owner ID and User ID cannot be null or empty.");
            }

            try
            {
                await _firebaseService.DeleteFollowAsync(followedUserId, userId);
            }
            catch (ArgumentException ex)
            {
                // Log the exception or handle it accordingly
                return BadRequest(ex.Message);
            }

            string theType = ViewBag.TheType;
            if (!String.IsNullOrEmpty(theType))
            {
                return RedirectToAction("Index", new { id = ownerId, type = theType });
            }
            return RedirectToAction("Index", "UserPage", new { id = ownerId });


        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}