using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SocialMediaApplication.Models;

namespace SocialMediaApplication.ViewComponents
{
    public class CommentViewComponent : ViewComponent
    {
        private readonly FirebaseService _firebaseService;

        public CommentViewComponent(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string postId)
        {
            var comments = await _firebaseService.GetComments(postId);
            return View(comments);
        }
    }
}