using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SocialMediaApplication.Models;

namespace SocialMediaApplication.Controllers
{

    public class LikeController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public LikeController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpPost]
        public async Task<IActionResult> LikePost(string ownerId,string postId)
        {
            string userId = HttpContext.Session.GetString("userId");
            var user = await _firebaseService.GetUserProfileAsync(userId);
            string userName = user.Name;
            await _firebaseService.LikePost(postId, userId, userName);
            var pageType = HttpContext.Session.GetString("pageType");
     
            if (!String.IsNullOrEmpty(ownerId) && !String.IsNullOrEmpty(userId))
            {
                if (pageType == "OtherPages")
                {
                    return RedirectToAction("Index", "OtherPages", new { id = ownerId });
                }
                else if (pageType == "Square")
                {
                    return RedirectToAction("Index", "Square", new { id = userId });
                }
            }

            return RedirectToAction("Index", "UserPage", new { id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> UnlikePost(string ownerId, string postId)
        {
            string userId = HttpContext.Session.GetString("userId");
            await _firebaseService.UnlikePost(postId, userId);
            var pageType = HttpContext.Session.GetString("pageType");

            if (!String.IsNullOrEmpty(ownerId) && !String.IsNullOrEmpty(userId))
            {
                if (pageType == "OtherPages")
                {
                    return RedirectToAction("Index", "OtherPages", new { Id = ownerId });
                }
                else if (pageType == "Square")
                {
                    return RedirectToAction("Index", "Square", new { Id = userId });
                }
            }

            return RedirectToAction("Index", "UserPage", new { Id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> LikeComment(string ownerId, string postId, string commentId)
        {
            string userId = HttpContext.Session.GetString("userId");
            var user = await _firebaseService.GetUserProfileAsync(userId);
            string userName = user.Name;
            await _firebaseService.LikeComment(postId, commentId, userId, userName);
            var pageType = HttpContext.Session.GetString("pageType");
           
            if (!String.IsNullOrEmpty(ownerId) && !String.IsNullOrEmpty(userId))
            {
                if (pageType == "OtherPages")
                {
                    return RedirectToAction("Index", "OtherPages", new { Id = ownerId });
                }
                else if (pageType == "Square")
                {
                    return RedirectToAction("Index", "Square", new { Id = userId });
                }
                return RedirectToAction("Index", "UserPage", new { Id = userId });
            }

            return RedirectToAction("Index", "UserPage", new { Id = userId });
        }

        [HttpPost]
        public async Task<IActionResult> UnlikeComment(string ownerId,string postId, string commentId)
        {
            string userId = HttpContext.Session.GetString("userId");
            await _firebaseService.UnlikeComment(postId, commentId, userId);
            var pageType = HttpContext.Session.GetString("pageType");
          
            if (!String.IsNullOrEmpty(ownerId) && !String.IsNullOrEmpty(userId))
            {
                if (pageType == "OtherPages")
                {
                    return RedirectToAction("Index", "OtherPages", new { Id = ownerId });
                }
                else if (pageType == "Square")
                {
                    return RedirectToAction("Index", "Square", new { Id = userId });
                }
            }

            return RedirectToAction("Index", "UserPage", new { Id = userId });
        }
    }

}