namespace SOPServer.Repository.Enums
{
    public enum CalendarFilterType
    {
        /// <summary>
        /// Filter by current week (Monday to Sunday)
        /// </summary>
        THIS_WEEK = 0,

        /// <summary>
        /// Filter by current month
        /// </summary>
        THIS_MONTH = 1,

        /// <summary>
        /// Filter by specific month and year
        /// </summary>
        SPECIFIC_MONTH = 2,

        /// <summary>
        /// Filter by date range (from date to date)
        /// </summary>
        DATE_RANGE = 3
    }
}
