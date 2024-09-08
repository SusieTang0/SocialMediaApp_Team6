using SocialMediaApplication.Models;
using SocialMediaApplication.Services;

namespace SocialMediaApplication.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
