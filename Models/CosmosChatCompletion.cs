namespace cosmosdb_chatgpt.Models
{
    public class CosmosChatCompletion
    {

        public string ChatSessionId { get; set; }

        public int PromptTokens { get; set; }

        public int CompletionTokens { get; set; }

        public string Completion { get; set; }


        public CosmosChatCompletion(string ChatSessionId, int PromptTokens, int CompletionTokens, string Completion)
        {
            this.ChatSessionId = ChatSessionId;
            this.PromptTokens = PromptTokens;
            this.CompletionTokens = CompletionTokens;
            this.Completion = Completion;
        }
    }
}
