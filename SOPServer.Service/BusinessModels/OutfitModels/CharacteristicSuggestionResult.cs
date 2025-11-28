namespace SOPServer.Service.BusinessModels.OutfitModels
{
    using SOPServer.Service.BusinessModels.ItemModels;
    using System.Collections.Generic;

    public class CharacteristicSuggestionResult
    {
        public string Category { get; set; }
        public List<ItemModel> Items { get; set; } = new List<ItemModel>();
    }
}
