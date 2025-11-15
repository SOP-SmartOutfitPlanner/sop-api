using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Enums
{
    public enum AISettingType
    {
        MODEL_EMBEDDING,
        MODEL_ANALYZING,
        API_ITEM_ANALYZING,
        API_SUGGESTION,
        API_EMBEDDING,
        VALIDATE_ITEM_PROMPT,
        DESCRIPTION_ITEM_PROMPT,
        CATEGORY_ITEM_ANALYSIS_PROMPT,
        OUTFIT_GENERATION_PROMPT,
        OUTFIT_CHOOSE_PROMPT
    }
}
