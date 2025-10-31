using System;

namespace SOPServer.Service.SettingModels
{
    /// <summary>
    /// Configuration settings for newsfeed ranking algorithm.
    /// All parameters are tunable via appsettings.json.
    /// </summary>
    public class NewsfeedSettings
    {
        #region Time-Decay Parameters
        
        /// <summary>
        /// Lambda (?) - Time decay rate.
        /// Higher values = faster decay (older posts drop faster).
        /// Typical range: 0.01 - 0.1
        /// </summary>
        public double Lambda { get; set; } = 0.05;

        #endregion

        #region Engagement Score Weights

        /// <summary>
        /// Alpha (?) - Weight for likes in engagement score.
        /// Typical: 1.0
        /// </summary>
        public double Alpha { get; set; } = 1.0;

        /// <summary>
        /// Beta (?) - Weight for comments in engagement score.
        /// Comments usually valued more than likes.
        /// Typical: 2.0
        /// </summary>
        public double Beta { get; set; } = 2.0;

        /// <summary>
        /// Gamma (?) - Weight for reshares in engagement score.
        /// Reshares typically valued highest.
        /// Typical: 3.0
        /// </summary>
        public double Gamma { get; set; } = 3.0;

        #endregion

        #region Affinity Score Weights

        /// <summary>
        /// W1 - Weight for past likes in affinity calculation.
        /// </summary>
        public double W1 { get; set; } = 1.0;

        /// <summary>
        /// W2 - Weight for past comments in affinity calculation.
        /// </summary>
        public double W2 { get; set; } = 2.0;

        /// <summary>
        /// W3 - Weight for direct replies in affinity calculation.
        /// </summary>
        public double W3 { get; set; } = 3.0;

        /// <summary>
        /// W4 - Weight for profile visits in affinity calculation.
        /// </summary>
        public double W4 { get; set; } = 0.5;

        /// <summary>
        /// Maximum expected affinity value for normalization.
        /// </summary>
        public double MaxAffinity { get; set; } = 100.0;

        #endregion

        #region Composite Score Weights

        /// <summary>
        /// Weight for Recency score in final composite.
        /// </summary>
        public double Wr { get; set; } = 1.0;

        /// <summary>
        /// Weight for Engagement score in final composite.
        /// </summary>
        public double We { get; set; } = 1.5;

        /// <summary>
        /// Weight for Affinity score in final composite.
        /// </summary>
        public double Wa { get; set; } = 2.0;

        /// <summary>
        /// Weight for Quality score in final composite.
        /// </summary>
        public double Wc { get; set; } = 1.0;

        /// <summary>
        /// Weight for Diversity penalty in final composite.
        /// </summary>
        public double Wd { get; set; } = 1.0;

        /// <summary>
        /// Weight for Negative feedback penalty in final composite.
        /// </summary>
        public double Wn { get; set; } = 1.0;

        /// <summary>
        /// Weight for Contextual boost in final composite.
        /// </summary>
        public double Wb { get; set; } = 0.5;

        #endregion

        #region Diversity & Penalties

        /// <summary>
        /// Delta (?) - Diversity penalty weight.
        /// Higher = stronger penalty for over-representation.
        /// Typical: 0.3 - 1.0
        /// </summary>
        public double Delta { get; set; } = 0.5;

        /// <summary>
        /// Zeta (?) - Negative feedback penalty weight.
        /// Typical: 0.5 - 2.0
        /// </summary>
        public double Zeta { get; set; } = 1.0;

        /// <summary>
        /// Maximum posts per author before diversity penalty kicks in.
        /// </summary>
        public int DiversityThreshold { get; set; } = 3;

        #endregion

        #region Contextual Boosts

        /// <summary>
        /// Boost value for posts with trending hashtags.
        /// </summary>
        public double TrendingHashtagBoost { get; set; } = 2.0;

        /// <summary>
        /// Boost value for posts from users with mutual followers.
        /// </summary>
        public double MutualFollowersBoost { get; set; } = 1.0;

        #endregion

        #region Refresh Dynamics

        /// <summary>
        /// Jitter percentage to add variance to scores (default 1.5%).
        /// Applied as: Score ± (Score * JitterPercent / 100)
        /// </summary>
        public double JitterPercent { get; set; } = 1.5;

        /// <summary>
        /// Temperature parameter for softmax sampling.
        /// Lower = more deterministic, Higher = more random.
        /// Typical: 0.3 - 0.7
        /// </summary>
        public double SoftmaxTemperature { get; set; } = 0.5;

        /// <summary>
        /// Percentage of posts to inject from explore/trending pool (?-greedy).
        /// Value between 0.0 and 1.0 (e.g., 0.1 = 10%)
        /// </summary>
        public double ExploreRate { get; set; } = 0.1;

        #endregion

        #region Redis Cache Settings

        /// <summary>
        /// TTL for candidate set cache in minutes.
        /// </summary>
        public int CandidateCacheTTL { get; set; } = 10;

        /// <summary>
        /// TTL for post metrics cache in minutes.
        /// </summary>
        public int MetricsCacheTTL { get; set; } = 15;

        /// <summary>
        /// TTL for seen posts tracking in minutes.
        /// </summary>
        public int SeenPostsTTL { get; set; } = 10;

        /// <summary>
        /// TTL for short-lived ranked window cache in seconds.
        /// </summary>
        public int RankedWindowTTL { get; set; } = 30;

        #endregion

        #region Feed Generation Settings

        /// <summary>
        /// Minimum number of candidate posts required before backfilling with trending.
        /// </summary>
        public int MinCandidates { get; set; } = 20;

        /// <summary>
        /// Maximum number of posts to fetch from database for candidate generation.
        /// </summary>
        public int MaxCandidateFetch { get; set; } = 500;

        /// <summary>
        /// Number of days to look back for candidate posts.
        /// </summary>
        public int CandidateLookbackDays { get; set; } = 7;

        #endregion
    }
}
