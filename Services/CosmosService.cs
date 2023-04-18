using System.Collections.ObjectModel;
using cosmosdb_chatgpt.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;


namespace cosmosdb_chatgpt.Services
{
    public class CosmosService
    {
        
        private readonly CosmosClient cosmosClient;
        private Container chatContainer;
        private readonly string databaseId;
        private readonly string containerId;

        public CosmosService(IConfiguration configuration)
        {
            
            string uri = configuration["CosmosDb.Endpoint"];
            string key = configuration["CosmosDb.Key"];

            databaseId = configuration["CosmosDb.Database"];
            containerId = configuration["CosmosDb.Container"];

            cosmosClient = new CosmosClient(uri, key);

            chatContainer = cosmosClient.GetContainer(databaseId, containerId);

        }

        
        // First call is made to this when chat page is loaded for left-hand nav.
        // Only retrieve the chat sessions, not chat messages
        public async Task<List<CosmosChatSession>> GetChatSessionsAsync()
        {

            List<CosmosChatSession> chatSessions = new();

            try 
            { 
                //Get the chat sessions without the chat messages.
                QueryDefinition query = new QueryDefinition("SELECT DISTINCT c.id, c.Type, c.ChatSessionId, c.ChatSessionName FROM c WHERE c.Type = @Type")
                    .WithParameter("@Type", "ChatSession");

                FeedIterator<CosmosChatSession> results = chatContainer.GetItemQueryIterator<CosmosChatSession>(query);

                while (results.HasMoreResults)
                {
                    FeedResponse<CosmosChatSession> response = await results.ReadNextAsync();

                    chatSessions.AddRange(response);
                
                }
            }
            catch(CosmosException ce)
            {
                //if 404, first run, create a new default chat session.
                if (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    CosmosChatSession chatSession = new CosmosChatSession();
                    await InsertChatSessionAsync(chatSession);
                    chatSessions.Add(chatSession);
                }

            }

            return chatSessions;

        }

        public async Task<CosmosChatSession> InsertChatSessionAsync(CosmosChatSession chatSession)
        {

            return await chatContainer.CreateItemAsync<CosmosChatSession>(chatSession, new PartitionKey(chatSession.ChatSessionId));

        }

        public async Task<CosmosChatSession> UpdateChatSessionAsync(CosmosChatSession chatSession)
        {

            return await chatContainer.ReplaceItemAsync(item: chatSession, id: chatSession.Id, partitionKey: new PartitionKey(chatSession.ChatSessionId));

        }

        public async Task DeleteChatSessionAsync(string chatSessionId)
        {
            
            //Retrieve the chat session and all chat messages
            QueryDefinition query = new QueryDefinition("SELECT c.id, c.ChatSessionId FROM c WHERE c.ChatSessionId = @chatSessionId")
                    .WithParameter("@chatSessionId", chatSessionId);


            FeedIterator<CosmosChatMessage> results = chatContainer.GetItemQueryIterator<CosmosChatMessage>(query);


            List<Task> deleteTasks = new List<Task>();

            while (results.HasMoreResults)
            {
                FeedResponse<CosmosChatMessage> response = await results.ReadNextAsync();
                
                foreach (var item in response)
                {

                    deleteTasks.Add(chatContainer.DeleteItemStreamAsync(item.Id, new PartitionKey(item.ChatSessionId)));

                }

            }

            await Task.WhenAll(deleteTasks);
 
        }

        public async Task<CosmosChatMessage> InsertChatMessageAsync(CosmosChatMessage chatMessage)
        {

            return await chatContainer.CreateItemAsync<CosmosChatMessage>(chatMessage, new PartitionKey(chatMessage.ChatSessionId));
            
        }

        public async Task<List<CosmosChatMessage>> InsertChatMessagesBatchAsync(List<CosmosChatMessage> cosmosChatMessages)
        {

           TransactionalBatchResponse messagesBatch = await chatContainer.CreateTransactionalBatch(new PartitionKey(cosmosChatMessages[0].ChatSessionId))
                .CreateItem(cosmosChatMessages[0])
                .CreateItem(cosmosChatMessages[1])
                .ExecuteAsync();

            
            return cosmosChatMessages;
            
        }

        public async Task<List<CosmosChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
        {

            //Get the chat messages for a chat session
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.ChatSessionId = @ChatSessionId AND c.Type = @Type")
                .WithParameter("@ChatSessionId", chatSessionId)
                .WithParameter("@Type", "ChatMessage");

            FeedIterator<CosmosChatMessage> results = chatContainer.GetItemQueryIterator<CosmosChatMessage>(query);

            List<CosmosChatMessage> chatMessages= new List<CosmosChatMessage>();
            
            while (results.HasMoreResults)
            {
                FeedResponse<CosmosChatMessage> response = await results.ReadNextAsync();

                chatMessages.AddRange(response);

            }

            return chatMessages;

        }

    }

}
