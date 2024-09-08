using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SocialMediaApplication.Models;

namespace SocialMediaApplication.ViewComponents
{            
    public class LikeViewComponent : ViewComponent
    {
        private readonly FirebaseService _firebaseService;

         public LikeViewComponent(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }
    }
}