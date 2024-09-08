

using Firebase.Auth;
using Firebase.Storage;
using FireSharp.Interfaces;
using FireSharp.Response;
using Microsoft.Extensions.Hosting;
using SocialMediaApplication.Models;
using System.Reflection;

public class FirebaseService
{
    private readonly FirebaseAuthProvider _authProvider;
    private readonly IFirebaseClient _firebaseClient;
    private readonly FirebaseStorage _firebaseStorage;

    public FirebaseService(IConfiguration configuration)
    {
        _authProvider = new FirebaseAuthProvider(new Firebase.Auth.FirebaseConfig(configuration["Firebase:ApiKey"]));

        IFirebaseConfig config = new FireSharp.Config.FirebaseConfig
        {
            AuthSecret = configuration["Firebase:ApiKey"],
            BasePath = configuration["Firebase:DatabaseURL"]
        };
        _firebaseClient = new FireSharp.FirebaseClient(config);
        _firebaseStorage = new FirebaseStorage(configuration["Firebase:StorageBucket"]);
    }

    /*________________________ User __________________________*/

    public async Task<FirebaseAuthLink> RegisterUser(string email, string password)
    {
        try
        {
            return await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
        }
        catch (FirebaseAuthException ex)
        {
            if (ex.Message.Contains("EMAIL_EXISTS"))
            {
                throw new Exception("This email is already registered.");
            }
            throw new Exception("This email is already registered. Please Login.");
        }

    }



    public async Task<FirebaseAuthLink> LoginUser(string email, string password)
    {
        try
        {
            return await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
        }
        catch (FirebaseAuthException ex)
        {
            if (ex.Message.Contains("EMAIL_NOT_FOUND"))
            {
                throw new Exception("This email is not registered.");
            }
            else if (ex.Message.Contains("INVALID_PASSWORD"))
            {
                throw new Exception("The password is incorrect.");
            }
            throw new Exception("The password/email is incorrect or does not exist.");
        }
    }


    public async Task<string> UploadProfilePictureAsync(Stream fileStream, string fileName)
    {
        try
        {
            var task = await _firebaseStorage
                .Child("profile_pictures")
                .Child(fileName)
                .PutAsync(fileStream);
            
            return task;
        }
        catch (FirebaseStorageException ex)
        {

            throw new Exception("Failed to upload profile picture. Please try again.");
        }
    }

    public async Task<SocialMediaApplication.Models.User> GetUserProfileAsync(string userId)
    {
        FirebaseResponse response = await _firebaseClient.GetAsync($"users/{userId}/profile");
        return response.ResultAs<SocialMediaApplication.Models.User>();
    }

    public async Task<FirebaseResponse> SaveUserProfileAsync(string userId, SocialMediaApplication.Models.User userProfile)
    {

        await _firebaseClient.SetAsync($"users/{userId}/profile", userProfile);
   

        return await _firebaseClient.SetAsync($"users/{userId}/profile", userProfile);
    }


    public async Task SendPasswordResetEmailAsync(string email)
    {
        try
        {
            await _authProvider.SendPasswordResetEmailAsync(email);
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to send password reset email. Please try again.");
        }
    }

    public async Task<bool> CheckIfUserExists(string email)
    {
        try
        {

            await _authProvider.SignInWithEmailAndPasswordAsync(email, "dummyPassword");

            return true;
        }
        catch (FirebaseAuthException ex)
        {

            if (ex.Message.Contains("EMAIL_NOT_FOUND"))
            {
                return false;
            }

            if (ex.Message.Contains("INVALID_PASSWORD"))
            {
                return true;
            }

            throw new Exception("An error occurred while checking the email.");
        }
    }
    

    public async Task<Dictionary<string, SocialMediaApplication.Models.User>> GetUsersAsync()
    {
        FirebaseResponse response = await _firebaseClient.GetAsync("users");


        var usersDictionary = response.ResultAs<Dictionary<string, SocialMediaApplication.Models.User>>();

        if (usersDictionary != null)
        {
            return usersDictionary;
        }

        return null;
    }


    /*________________________ Post __________________________*/

    public async Task AddPost(string content, string authorId, string authorName, string authorAvatar)
    {

        var post = new Post
        {
            Content = content,
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorAvatar = authorAvatar,
            CreatedTime = DateTime.Now,
            UpdatedTime = DateTime.Now
        };

        if (post == null)
        {
            throw new ArgumentException("Post ID and Post object cannot be null or empty.");
        }


        try
        {
            var response = await _firebaseClient.PushAsync("posts", post);
            string generatedKey = response.Result.name;
            post.Id = generatedKey;
            await _firebaseClient.SetAsync($"posts/{generatedKey}", post);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error saving the post.", ex);
        }


    }

    public async Task<FirebaseResponse> SavePostAsync(string id, string content)
    {
        var theOne = await GetPostByPostIdAsync(id);
        var post = new Post
        {
            Content = content,
            AuthorId = theOne.AuthorId,
            AuthorName = theOne.AuthorName,
            AuthorAvatar = theOne.AuthorAvatar,
            CreatedTime = theOne.CreatedTime,
            UpdatedTime = DateTime.Now
        };
        if (post == null || string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Post object or Id cannot be null or empty.");
        }
        return await _firebaseClient.SetAsync($"posts/{id}", post);
    }

    public async Task DeletePostAsync(string postId)
    {
        if (string.IsNullOrEmpty(postId))
        {
            throw new ArgumentException("Post ID cannot be null or empty.");
        }

        try
        {

            await _firebaseClient.DeleteAsync($"posts/{postId}");
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error deleting the post.", ex);
        }
    }

    public async Task<Dictionary<string, SocialMediaApplication.Models.Post>> GetPostsAsync()
    {
        var response = await _firebaseClient.GetAsync("posts");
        var posts = response.ResultAs<Dictionary<string, Post>>();

        if (posts != null)
        {
            foreach (var postId in posts.Keys.ToList())
            {
                var post = posts[postId];

                // Fetch comments and likes concurrently
                var commentsTask = _firebaseClient.GetAsync($"posts/{postId}/comments");
                var likesTask = _firebaseClient.GetAsync($"posts/{postId}/likes");

                await Task.WhenAll(commentsTask, likesTask);

                // Process comments
                var commentsResponse = commentsTask.Result;
                var comments = commentsResponse.ResultAs<Dictionary<string, Comment>>();
                var sortedComments = comments?.OrderByDescending(pair => pair.Value.CreatedTime).ToDictionary(pair => pair.Key, pair => pair.Value);
                post.Comments = sortedComments ?? new Dictionary<string, Comment>();

                // Process post likes
                var likesResponse = likesTask.Result;
                var likes = likesResponse.ResultAs<Dictionary<string, Like>>();
                post.Likes = likes ?? new Dictionary<string, Like>();

                // Fetch likes for comments
                var commentLikeTasks = post.Comments.Keys.Select(async commentId =>
                {
                    var clikesResponse = await _firebaseClient.GetAsync($"posts/{postId}/comments/{commentId}/likes");
                    var clikes = clikesResponse.ResultAs<Dictionary<string, Like>>();

                    post.Comments[commentId].Likes = clikes ?? new Dictionary<string, Like>();
                });

                await Task.WhenAll(commentLikeTasks);
            }
        }

        return posts;
    }


    public async Task<List<SocialMediaApplication.Models.Post>> GetAllPostsAsync()
    {
        var posts = new List<Post>();
        var allPosts = await GetPostsAsync();

        if (allPosts != null)
        {
            posts = allPosts
                 .Select(post => new Post
                 {
                     Id = post.Key,
                     AuthorId = post.Value.AuthorId,
                     AuthorName = post.Value.AuthorName,
                     AuthorAvatar = post.Value.AuthorAvatar,
                     Content = post.Value.Content,
                     CreatedTime = post.Value.CreatedTime,
                     UpdatedTime = post.Value.UpdatedTime,
                     Comments = post.Value.Comments,
                     Likes = post.Value.Likes
                 })
                 .OrderByDescending(post => post.CreatedTime)
                 .ToList();
        }
        posts.Reverse();
        return posts;
    }

    public async Task<SocialMediaApplication.Models.Post> GetPostByPostIdAsync(string postId)
    {
        if (string.IsNullOrEmpty(postId))
        {
            return null;
        }

        var allPosts = await GetPostsAsync();

        if (allPosts != null && allPosts.TryGetValue(postId, out var post))
        {
            return post;
        }

        return null;

    }

    public async Task<List<Post>> FindPostsByConditionAsync(Func<KeyValuePair<string, Post>, bool> condition)
    {
        var posts = new List<Post>();
        var allPosts = await GetPostsAsync();

        if (allPosts != null)
        {
            foreach (var post in allPosts)
            {
                if (post.Value != null && condition(post)) // Added null check for post.Value
                {
                    var thepost = new Post
                    {
                        Id = post.Key,
                        AuthorId = post.Value.AuthorId,
                        AuthorName = post.Value.AuthorName,
                        AuthorAvatar = post.Value.AuthorAvatar,
                        Content = post.Value.Content,
                        CreatedTime = post.Value.CreatedTime,
                        UpdatedTime = post.Value.UpdatedTime,
                        Comments = post.Value.Comments,
                        Likes = post.Value.Likes
                    };
                    posts.Add(thepost);
                }
            }
        }
        posts.Reverse();
        return posts;
    }
    

    public async Task<List<Post>> FindFollowedPostsAsync(string userId)
    {
        var followedIds = await GetFollowedIdsSetAsync(userId);
        return await FindPostsByConditionAsync(post => followedIds.Contains(post.Value.AuthorId));
    }

    public async Task<List<Post>> FindPostListAsync(string userId)
    {
        return await FindPostsByConditionAsync(post => post.Value != null && post.Value.AuthorId != null && post.Value.AuthorId.Equals(userId));
    }

    public async Task<PostList> GetPostlistsAsync(string id)
    {
        var thePosts = new PostList
        {
            MyPosts = await FindPostListAsync(id),
            MyFollowedPosts = await FindFollowedPostsAsync(id),//await _postService.FindFollowedPostsAsync(id, 5)
        };

        return thePosts;
    }


    /*________________________ Follow __________________________*/
    public async Task<List<SocialMediaApplication.Models.Follow>> GetFollowedUsersAsync(string userId)
    {
        FirebaseResponse response = await _firebaseClient.GetAsync($"users/{userId}/followings");
        var followsDictionary = response.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

        if (followsDictionary != null)
        {
            return followsDictionary.Values.ToList();
        }

        return new List<SocialMediaApplication.Models.Follow>();
    }

    public async Task<List<SocialMediaApplication.Models.Follow>> GetFollowerUsersAsync(string userId)
    {
        FirebaseResponse response = await _firebaseClient.GetAsync($"users/{userId}/followers");
        var followsDictionary = response.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

        if (followsDictionary != null)
        {
            return followsDictionary.Values.ToList();
        }

        return new List<SocialMediaApplication.Models.Follow>();
    }

    public async Task<HashSet<string>> GetFollowedIdsSetAsync(string userId)
    {
        var result = new HashSet<string>();

        FirebaseResponse response = await _firebaseClient.GetAsync($"users/{userId}/followings");

        var followsDictionary = response.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

        if (followsDictionary != null)
        {
            foreach(var followed in followsDictionary.Values)
            {
                Console.WriteLine($"Adding UserId: {followed.UserId}");
                result.Add(followed.UserId);
            }
          
        }

        return result;
    }

    public async Task<HashSet<string>> GetFollowerIdsSetAsync(string userId)
    {
        var result = new HashSet<string>();

        FirebaseResponse response = await _firebaseClient.GetAsync($"users/{userId}/followers");

        var followsDictionary = response.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

        if (followsDictionary != null)
        {
            foreach (var follower in followsDictionary.Values)
            {
                result.Add(follower.UserId);
            }

        }

        return result;
    }

    public async Task AddFollowAsync(string followingId, string followerId)
    {
        if (string.IsNullOrEmpty(followingId) || string.IsNullOrEmpty(followerId))
        {
            throw new ArgumentException("Following Id and Follower Id cannot be null or empty.");
        }
        var followingUser = await GetUserProfileAsync(followingId);
        var followerUser = await GetUserProfileAsync(followerId);
        if (followingUser == null || followerUser == null)
        {
            throw new ArgumentException("Following Id and Follower Id cannot be null or empty.");
        }
        var following = new Follow
        {
            UserId = followingId,
            UserName = followingUser.Name,
            UserAvatar = followingUser.ProfilePictureUrl,
            UserBio = followingUser.Bio,
            CreatedTime = DateTime.Now,
        };
        var follower = new
        {
            UserId = followerId,
            UserName = followerUser.Name,
            UserAvatar = followerUser.ProfilePictureUrl,
            UserBio = followerUser.Bio,
            CreatedTime = DateTime.Now,
        };

        try
        {
            var response1 = await _firebaseClient.PushAsync($"users/{followerId}/followings", following);
            string generatedKey1 = response1.Result.name;
            following.Id = generatedKey1;
            await _firebaseClient.SetAsync($"users/{followerId}/followings/{generatedKey1}", following);
           

            var response2 = await _firebaseClient.PushAsync($"users/{followingId}/followers", follower);
            string generatedKey2 = response2.Result.name;
            following.Id = generatedKey1;
            await _firebaseClient.SetAsync($"users/{followingId}/followers/{generatedKey2}", follower);

        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error saving the follow.", ex);
        }
    }


    public async Task DeleteFollowAsync(string followingId, string followerId)
    {
        if (string.IsNullOrEmpty(followingId) || string.IsNullOrEmpty(followerId))
        {
            throw new ArgumentException("Post ID cannot be null or empty.");
        }

        try
        {

            FirebaseResponse response1 = await _firebaseClient.GetAsync($"users/{followerId}/followings");
            var followedDictionary = response1.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

            foreach (var follow in followedDictionary)
            {
                var followKey = follow.Key;
                if (follow.Value.UserId == followingId)
                {
                    if (followKey != null)
                    {
                        await _firebaseClient.DeleteAsync($"users/{followerId}/followings/{followKey}");

                        break;
                    }

                }
            }

            FirebaseResponse response2 = await _firebaseClient.GetAsync($"users/{followingId}/followers");
            var followerDictionary = response2.ResultAs<Dictionary<string, SocialMediaApplication.Models.Follow>>();

            foreach (var follow in followerDictionary)
            {
                var followKey2 = follow.Key;
                if (follow.Value.UserId == followerId)
                {
                    if (followKey2 != null)
                    {
                        await _firebaseClient.DeleteAsync($"users/{followingId}/followers/{followKey2}");

                        break;
                    }

                }
            }

        }
        catch (Exception ex)
        {
            throw new ApplicationException("Error saving the follow.", ex);
        }

    }



    /*________________________ Like __________________________*/
    public async Task LikePost(string postId, string userId, string userName)
    {
        var like = new { UserId = userId, UserName = userName, LikedAt = DateTime.Now };
        var response = await _firebaseClient.PushAsync($"posts/{postId}/likes/", like);

    }

    public async Task UnlikePost(string postId, string userId)
    {
        var likeNode = await _firebaseClient.GetAsync($"posts/{postId}/likes");
        var likes = likeNode.ResultAs<Dictionary<string, Like>>();

        var likeId = likes?.FirstOrDefault(x => x.Value.UserId == userId).Key;
        if (likeId != null)
        {
            await _firebaseClient.DeleteAsync($"posts/{postId}/likes/{likeId}");
        }

    }
    public async Task<List<Like>> ShowPostLikes(string postId)
    {
        var response = await _firebaseClient.GetAsync($"posts/{postId}/likes");
        var likes = response.ResultAs<Dictionary<string, Like>>();

        return likes?.Values.ToList() ?? new List<Like>();
    }

    public async Task<int> ShowPostLikesCount(string postId)
    {
        var response = await _firebaseClient.GetAsync($"posts/{postId}/likes");
        var likes = response.ResultAs<Dictionary<string, Like>>();

        return likes?.Count ?? 0;

    }


    /*________________________ Comment __________________________*/
    public async Task AddComment(string postId, string authorId, string authorName, string authorAvatar, string content)
    {
        var comment = new Comment
        {
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorAvatar = authorAvatar,
            PostId = postId,
            Content = content,
            CreatedTime = DateTime.Now,
            UpdatedTime = DateTime.Now
        };
        var response = await _firebaseClient.PushAsync($"posts/{postId}/comments", comment);
    }

    public async Task EditComment(string postId, string content, string commentId)
    {
        var comment = new
        {
            Content = content,
            UpdatedTime = DateTime.Now
        };
        var response = await _firebaseClient.PushAsync($"posts/{postId}/comments/{commentId}", comment);
    }

    public async Task DeleteComment(string postId, string userId)
    {
        var commentNode = await _firebaseClient.GetAsync($"posts/{postId}/comments");
        var comments = commentNode.ResultAs<Dictionary<string, Comment>>();

        var commentId = comments?.FirstOrDefault(x => x.Value.AuthorId == userId).Key;
        if (commentId != null)
        {
            await _firebaseClient.DeleteAsync($"posts/{postId}/comments/{commentId}");
        }
    }
    public async Task<List<dynamic>> GetComments(string postId)
    {
        var response = await _firebaseClient.GetAsync($"posts/{postId}/comments");
        return response.ResultAs<List<dynamic>>();
    }

    public async Task LikeComment(string postId, string commentId, string userId, string userName)
    {
        var like = new { UserId = userId, UserName = userName, LikedAt = DateTime.Now };
        var response = await _firebaseClient.PushAsync($"posts/{postId}/comments/{commentId}/likes", like);

    }

    public async Task UnlikeComment(string postId, string commentId, string userId)
    {
        var likeNode = await _firebaseClient.GetAsync($"posts/{postId}/comments/{commentId}/likes");
        var likes = likeNode.ResultAs<Dictionary<string, Like>>();

        var likeId = likes?.FirstOrDefault(x => x.Value.UserId == userId).Key;
        if (likeId != null)
        {
            await _firebaseClient.DeleteAsync($"posts/{postId}/comments/{commentId}/likes/{likeId}");
        }

    }
    public async Task<List<Like>> ShowCommentLikes(string postId, string commentId)
    {
        var response = await _firebaseClient.GetAsync($"posts/{postId}/comments/{commentId}/likes");
        var likes = response.ResultAs<Dictionary<string, Like>>();

        return likes?.Values.ToList() ?? new List<Like>();
    }


}