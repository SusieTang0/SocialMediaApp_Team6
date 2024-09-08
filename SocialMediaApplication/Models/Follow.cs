namespace SocialMediaApplication.Models
{
    public class Follow
    {
        public string Id { get; set; }
        public string UserId {  get; set; }
        public string UserName { get; set; }
        public string UserAvatar { get; set; }
        public string UserBio { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}