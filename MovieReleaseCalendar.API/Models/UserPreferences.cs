using System;
using Newtonsoft.Json;

namespace MovieReleaseCalendar.API.Models
{
    public class UserPreferences
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "global";

        [JsonProperty("theme")]
        public string Theme { get; set; } = "dark";

        [JsonProperty("defaultView")]
        public string DefaultView { get; set; } = "month";

        [JsonProperty("tmdbApiKey")]
        public string TmdbApiKey { get; set; } = string.Empty;

        [JsonProperty("cronSchedule")]
        public string CronSchedule { get; set; } = "0 0 * * 0";

        [JsonProperty("showRatings")]
        public bool ShowRatings { get; set; } = true;

        [JsonProperty("enableSwagger")]
        public bool EnableSwagger { get; set; } = false;

        [JsonProperty("updatedAt")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
