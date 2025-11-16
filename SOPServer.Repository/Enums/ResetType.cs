namespace SOPServer.Repository.Enums
{
    public enum ResetType
    {
        Never = 0,   // Credit-based, never resets (e.g., wardrobe items)
        Monthly = 1  // Resets every month (e.g., outfit suggestions, split items, plan occasions)
    }
}
