namespace SocialMediaApplication.Models
{
    public class User
    {
        public string UserId { get; set; } // This might be set after registration
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; } // Add this if you plan to use User for registration
        public string Bio { get; set; } = "This is your bio. You can update it later."; // Default value
        public string ProfilePictureUrl { get; set; } // Optional

    }
}
