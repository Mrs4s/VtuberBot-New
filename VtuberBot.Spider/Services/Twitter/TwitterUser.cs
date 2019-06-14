using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace VtuberBot.Spider.Services.Twitter
{
    public class TwitterUser
    {
        [JsonProperty("id")]
        [BsonElement("_id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        [BsonElement("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        [BsonElement("screenName")]
        public string ScreenName { get; set; }

        [JsonProperty("url")]
        [BsonElement("profileLink")]
        public string ProfileLink { get; set; }

        [JsonProperty("description")]
        [BsonElement("Description")]
        public string Description { get; set; }

        [JsonProperty("followers_count")]
        [BsonElement("followersCount")]
        public int FollowersCount { get; set; }

        [JsonProperty("friends_count")]
        [BsonElement("friendsCount")]
        public int FriendsCount { get; set; }

        [JsonProperty("statuses_count")]
        [BsonElement("tweetsCount")]
        public int TweetsCount { get; set; }




    }
}
