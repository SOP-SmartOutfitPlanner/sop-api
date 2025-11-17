namespace SOPServer.Repository.Enums
{
    /// <summary>
    /// Defines how subscription benefits behave across subscription renewals
    /// </summary>
    public enum BenefitType
    {
        /// <summary>
        /// Credits persist across subscription renewals (e.g., wardrobe storage capacity).
        /// Remaining credits carry over when user repurchases the same plan.
        /// Can be restored when items are deleted.
        /// </summary>
        Persistent = 0,

        /// <summary>
        /// Credits reset to full on each new subscription purchase (e.g., AI suggestions quota).
        /// Fresh credits granted when user repurchases, regardless of previous usage.
        /// Cannot be restored when consumed.
        /// </summary>
        Renewable = 1
    }
}
