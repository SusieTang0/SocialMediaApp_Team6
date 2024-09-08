using System.ComponentModel.DataAnnotations;

namespace SocialMediaApplication.Models
{
    public class PostData
    {

        public string Id { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public string AuthorId { get; set; }
        [Required]
        public string AuthorName { get; set; }
        [Required]
        public string AuthorAvatar { get; set; }

        public Dictionary<string, Comment> Comments { get; set; }
        public Dictionary<string, Like> Likes { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
