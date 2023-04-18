using Newtonsoft.Json;

namespace cosmosdb_chatgpt.Models
{
    public class CosmosChatMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string Type { get; set; }

        public string ChatSessionId { get; set; } //partition key

        public DateTime TimeStamp { get; set; }

        public string Sender { get; set; }

        public int Tokens { get; set; }

        public string Text { get; set; }

        public CosmosChatMessage(string ChatSessionId, string Sender, int tokens, string Text)
        {
            Id = Guid.NewGuid().ToString();
            Type = "ChatMessage";
            this.ChatSessionId = ChatSessionId; //partition key
            this.Sender = Sender;
            TimeStamp = DateTime.UtcNow;
            Tokens = tokens;
            this.Text = Text;
        }
    }
}
