using System.Collections.Generic;

namespace ClarionTransformer.Services
{
    public class TransformerProfile
    {
        public string ProfileName         { get; set; } = "Legacy -> ABC";
        public string AiProtocolFile      { get; set; } = "";
        public string AiExtraInstructions { get; set; } = "";
        public bool   CreateBackup        { get; set; } = true;
        public bool   AddComments         { get; set; } = false;
        public bool   Reindent            { get; set; } = true;
        public int    IndentSpaces        { get; set; } = 4;
    }

    public class TransformerSettings
    {
        public string AnthropicApiKey { get; set; } = "";
        public string AiModel         { get; set; } = "claude-sonnet-4-6";
        public string ActiveProfile   { get; set; } = "Legacy -> ABC";
        public List<TransformerProfile> Profiles { get; set; } = new List<TransformerProfile>
        {
            new TransformerProfile { ProfileName = "Legacy -> ABC" },
            new TransformerProfile { ProfileName = "Refactorizar", CreateBackup = false }
        };
    }
}
