using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SocialMediaApplication.Models;
using Firebase.Auth;

namespace SocialMediaApplication.Controllers
{
    public class CommentController : Controller
    {
        private readonly FirebaseService _firebaseService;

        public CommentController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(string ownerId, string postId, string commentContent)
        {
            string authorId = HttpContext.Session.GetString("userId");
            var author = await _firebaseService.GetUserProfileAsync(authorId);
            string authorName = author.Name;
            string authorAvatar = author.ProfilePictureUrl;
            await _firebaseService.AddComment(postId, authorId, authorName, authorAvatar, commentContent);
           
            return await turnToPage(ownerId, authorId);
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(string ownerId, string postId, string commentId, string commentContent)
        {
            string authorId = HttpContext.Session.GetString("userId");
            var author = await _firebaseService.GetUserProfileAsync(authorId);
            await _firebaseService.EditComment(postId, commentId, commentContent);
            return await turnToPage(ownerId, authorId);
        }

        public async Task<IActionResult> DeleteComment(string ownerId, string postId, string commentId)
        {
            string authorId = HttpContext.Session.GetString("userId");
            var author = await _firebaseService.GetUserProfileAsync(authorId);
            await _firebaseService.DeleteComment(postId, commentId);
            return await turnToPage(ownerId, authorId);
        }


        public async Task<IActionResult> turnToPage(string ownerId, string authorId)
        {
            var pageType = HttpContext.Session.GetString("pageType");
            if (pageType == "OtherPages")
            {
                return RedirectToAction("Index", "OtherPages", new { Id = ownerId });
            }
            else if (pageType == "Square")
            {
                return RedirectToAction("Index", "Square", new { Id = authorId });
            }
            else
            {
                return RedirectToAction("Index", "UserPage", new { Id = authorId });
            }
        }

    } 
}