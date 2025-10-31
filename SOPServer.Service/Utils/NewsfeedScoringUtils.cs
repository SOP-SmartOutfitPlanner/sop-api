using System;
using System.Collections.Generic;
using System.Linq;

namespace SOPServer.Service.Utils
{
    /// <summary>
    /// Utilities for newsfeed ranking calculations.
    /// Implements time-decay, engagement scoring, affinity calculations, and diversity penalties.
    /// </summary>
    public static class NewsfeedScoringUtils
    {
        /// <summary>
        /// Calculates time-decay factor using exponential decay.
        /// R = exp(-? * age_hours)
        /// </summary>
        /// <param name="createdAt">Post creation time</param>
        /// <param name="lambda">Decay rate parameter (?)</param>
        /// <returns>Recency score between 0 and 1</returns>
        public static double CalculateRecencyScore(DateTime createdAt, double lambda)
        {
            var ageHours = (DateTime.UtcNow - createdAt).TotalHours;
            return Math.Exp(-lambda * ageHours);
        }

        /// <summary>
        /// Calculates engagement score based on interactions.
        /// E = ?*likes + ?*comments + ?*reshares
        /// </summary>
        /// <param name="likes">Number of likes</param>
        /// <param name="comments">Number of comments</param>
        /// <param name="reshares">Number of reshares</param>
        /// <param name="alpha">Weight for likes (?)</param>
        /// <param name="beta">Weight for comments (?)</param>
        /// <param name="gamma">Weight for reshares (?)</param>
        /// <returns>Engagement score</returns>
        public static double CalculateEngagementScore(
            int likes,
            int comments,
            int reshares,
            double alpha,
            double beta,
            double gamma)
        {
            return (alpha * likes) + (beta * comments) + (gamma * reshares);
        }

        /// <summary>
        /// Normalizes affinity score to [0, 1] range.
        /// A = normalize(w1*pastLikes + w2*pastComments + w3*directReplies + w4*profileVisits)
        /// </summary>
        /// <param name="pastLikes">Past likes from user to author</param>
        /// <param name="pastComments">Past comments from user to author</param>
        /// <param name="directReplies">Direct replies from user to author</param>
        /// <param name="profileVisits">Profile visits from user to author</param>
        /// <param name="w1">Weight for past likes</param>
        /// <param name="w2">Weight for past comments</param>
        /// <param name="w3">Weight for direct replies</param>
        /// <param name="w4">Weight for profile visits</param>
        /// <param name="maxAffinity">Maximum expected affinity value for normalization</param>
        /// <returns>Normalized affinity score [0, 1]</returns>
        public static double CalculateAffinityScore(
            int pastLikes,
            int pastComments,
            int directReplies,
            int profileVisits,
            double w1,
            double w2,
            double w3,
            double w4,
            double maxAffinity = 100.0)
        {
            var rawAffinity = (w1 * pastLikes) + (w2 * pastComments) + (w3 * directReplies) + (w4 * profileVisits);
            return Math.Min(rawAffinity / maxAffinity, 1.0);
        }

        /// <summary>
        /// Calculates author quality score (EMA of engagement rate).
        /// Q = EMA(author_engagement_rate, 30d) ? [0,1]
        /// For simplicity, uses average engagement rate.
        /// </summary>
        /// <param name="authorEngagementRate">Author's average engagement rate</param>
        /// <returns>Quality score [0, 1]</returns>
        public static double CalculateQualityScore(double authorEngagementRate)
        {
            return Math.Clamp(authorEngagementRate, 0.0, 1.0);
        }

        /// <summary>
        /// Calculates diversity penalty to prevent over-representation.
        /// D = -? * over_representation_factor
        /// </summary>
        /// <param name="authorPostCount">Number of posts from this author in current feed</param>
        /// <param name="diversityThreshold">Maximum posts per author before penalty</param>
        /// <param name="delta">Penalty weight (?)</param>
        /// <returns>Diversity penalty (negative value)</returns>
        public static double CalculateDiversityPenalty(
            int authorPostCount,
            int diversityThreshold,
            double delta)
        {
            if (authorPostCount <= diversityThreshold)
                return 0.0;

            var overRepresentation = (double)(authorPostCount - diversityThreshold) / diversityThreshold;
            return -delta * overRepresentation;
        }

        /// <summary>
        /// Calculates negative feedback penalty.
        /// N = -? * feedback_severity
        /// </summary>
        /// <param name="hideCount">Number of times user hid posts from this author</param>
        /// <param name="reportCount">Number of times user reported this author</param>
        /// <param name="zeta">Penalty weight (?)</param>
        /// <returns>Negative feedback penalty (negative value)</returns>
        public static double CalculateNegativeFeedbackPenalty(
            int hideCount,
            int reportCount,
            double zeta)
        {
            var feedbackSeverity = hideCount + (reportCount * 2); // Reports weigh more
            return -zeta * feedbackSeverity;
        }

        /// <summary>
        /// Applies contextual boost based on various factors.
        /// B = contextual_boost (e.g., trending hashtags, mutual friends)
        /// </summary>
        /// <param name="hasTrendingHashtag">Post has trending hashtag</param>
        /// <param name="hasMutualFollowers">User and author have mutual followers</param>
        /// <param name="trendingBoost">Boost value for trending content</param>
        /// <param name="mutualBoost">Boost value for mutual connections</param>
        /// <returns>Contextual boost value</returns>
        public static double CalculateContextualBoost(
            bool hasTrendingHashtag,
            bool hasMutualFollowers,
            double trendingBoost,
            double mutualBoost)
        {
            double boost = 0.0;
            if (hasTrendingHashtag) boost += trendingBoost;
            if (hasMutualFollowers) boost += mutualBoost;
            return boost;
        }

        /// <summary>
        /// Adds random jitter to score for variance in feed ranking.
        /// Score' = Score + ?, where ? ~ U(-?, ?)
        /// </summary>
        /// <param name="score">Base score</param>
        /// <param name="jitterPercent">Jitter as percentage of score (default 1.5%)</param>
        /// <param name="random">Random number generator</param>
        /// <returns>Score with jitter applied</returns>
        public static double ApplyJitter(double score, double jitterPercent, Random random)
        {
            var tau = score * (jitterPercent / 100.0);
            var epsilon = (random.NextDouble() * 2 - 1) * tau; // U(-?, ?)
            return score + epsilon;
        }

        /// <summary>
        /// Calculates softmax probability for sampling.
        /// p_i = exp(Score_i / T) / ? exp(Score_j / T)
        /// </summary>
        /// <param name="scores">List of scores</param>
        /// <param name="temperature">Temperature parameter (T, default 0.5)</param>
        /// <returns>Probability distribution</returns>
        public static double[] CalculateSoftmaxProbabilities(IEnumerable<double> scores, double temperature = 0.5)
        {
            var scoreList = scores.ToList();
            var expScores = scoreList.Select(s => Math.Exp(s / temperature)).ToArray();
            var sumExp = expScores.Sum();
            return expScores.Select(e => e / sumExp).ToArray();
        }

        /// <summary>
        /// Samples items based on softmax probabilities.
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="items">Items to sample from</param>
        /// <param name="probabilities">Probability distribution</param>
        /// <param name="sampleSize">Number of items to sample</param>
        /// <param name="random">Random number generator</param>
        /// <returns>Sampled items</returns>
        public static List<T> SoftmaxSample<T>(
            IList<T> items,
            double[] probabilities,
            int sampleSize,
            Random random)
        {
            var sampled = new List<T>();
            var availableIndices = Enumerable.Range(0, items.Count).ToList();
            var availableProbs = probabilities.ToList();

            for (int i = 0; i < Math.Min(sampleSize, items.Count); i++)
            {
                var cumulativeProbs = new double[availableProbs.Count];
                cumulativeProbs[0] = availableProbs[0];
                for (int j = 1; j < availableProbs.Count; j++)
                {
                    cumulativeProbs[j] = cumulativeProbs[j - 1] + availableProbs[j];
                }

                var randomValue = random.NextDouble() * cumulativeProbs[^1];
                var selectedIndex = 0;
                for (int j = 0; j < cumulativeProbs.Length; j++)
                {
                    if (randomValue <= cumulativeProbs[j])
                    {
                        selectedIndex = j;
                        break;
                    }
                }

                sampled.Add(items[availableIndices[selectedIndex]]);
                availableIndices.RemoveAt(selectedIndex);
                availableProbs.RemoveAt(selectedIndex);
            }

            return sampled;
        }

        /// <summary>
        /// Calculates final composite score.
        /// Score = wr*R + we*E + wa*A + wc*Q + wd*D + wn*N + wb*B
        /// </summary>
        /// <param name="recency">Recency score (R)</param>
        /// <param name="engagement">Engagement score (E)</param>
        /// <param name="affinity">Affinity score (A)</param>
        /// <param name="quality">Quality score (Q)</param>
        /// <param name="diversity">Diversity penalty (D)</param>
        /// <param name="negativeFeedback">Negative feedback penalty (N)</param>
        /// <param name="contextualBoost">Contextual boost (B)</param>
        /// <param name="wr">Weight for recency</param>
        /// <param name="we">Weight for engagement</param>
        /// <param name="wa">Weight for affinity</param>
        /// <param name="wc">Weight for quality</param>
        /// <param name="wd">Weight for diversity</param>
        /// <param name="wn">Weight for negative feedback</param>
        /// <param name="wb">Weight for contextual boost</param>
        /// <returns>Final composite score</returns>
        public static double CalculateCompositeScore(
            double recency,
            double engagement,
            double affinity,
            double quality,
            double diversity,
            double negativeFeedback,
            double contextualBoost,
            double wr,
            double we,
            double wa,
            double wc,
            double wd,
            double wn,
            double wb)
        {
            return (wr * recency) +
                   (we * engagement) +
                   (wa * affinity) +
                   (wc * quality) +
                   (wd * diversity) +
                   (wn * negativeFeedback) +
                   (wb * contextualBoost);
        }
    }
}
