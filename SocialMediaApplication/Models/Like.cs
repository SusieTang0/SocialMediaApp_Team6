namespace SocialMediaApplication.Models
{
    public class Like
    {
        public string Id { get; set; }
        public string? PostId { get; set; }
        public string? CommentId { get; set; }
        public required string UserId { get; set; }
        public required string UserName { get; set; }
        public DateTime LikedAt { get; set; }

    }
}