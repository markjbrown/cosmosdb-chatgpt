using Newtonsoft.Json;

namespace cosmosdb_chatgpt.Models
{
    public class CosmosChatSession
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Type { get; set; }

        public string ChatSessionId { get; set; } //partition key

        public string ChatSessionName { get; set; }

        [JsonIgnore]
        public List<CosmosChatMessage> Messages { get; set; }

        public CosmosChatSession()
        {
            Id = Guid.NewGuid().ToString();
            Type = "ChatSession";
            ChatSessionId = Id;
            ChatSessionName = "New Chat";
            Messages = new List<CosmosChatMessage>();
        }

        public void AddMessage(CosmosChatMessage message)
        {

            Messages.Add(message);

        }

        public void AddPromptAndCompletion(List<CosmosChatMessage> messages)
        {

            Messages.AddRange(messages);

        }
    }
}
