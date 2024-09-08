namespace SocialMediaApplication.Models
{
    public class CommentViewModel
    {
        public string PostId { get; set; }
        public Dictionary<string, Comment> Comments { get; set; }
    }
}
