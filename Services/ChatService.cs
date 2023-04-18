using Azure.AI.OpenAI;
using cosmosdb_chatgpt.Models;

namespace cosmosdb_chatgpt.Services
{
    public class ChatService 
    {

        //All data is cached in chatSessions List object.
        private static List<CosmosChatSession> chatSessions;

        private readonly CosmosService cosmos;
        private readonly OpenAiService openAi;
        private readonly int maxConversationLength;


        public ChatService(IConfiguration configuration)
        {
            maxConversationLength = int.Parse(configuration["OpenAiMaxTokens"]) / 2;

            cosmos = new CosmosService(configuration);
            openAi = new OpenAiService(configuration);
            
            chatSessions = new List<CosmosChatSession>();
        }

        
        // Returns list of chat session ids and names for left-hand nav to bind to (display ChatSessionName and ChatSessionId as hidden)
        public async Task<List<CosmosChatSession>> GetAllChatSessionsAsync()
        {
            chatSessions = await cosmos.GetChatSessionsAsync();
           
            return chatSessions;
        }

        //Returns the chat messages to display on the main web page when the user selects a chat from the left-hand nav
        public async Task<List<CosmosChatMessage>> GetChatSessionMessagesAsync(string chatSessionId)
        {

            List<CosmosChatMessage> chatMessages = new List<CosmosChatMessage>();

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);
            
            if (chatSessions[index].Messages.Count == 0)
            { 
                //Messages are not cached, go read from database
                chatMessages = await cosmos.GetChatSessionMessagesAsync(chatSessionId);

                //cache results
                 chatSessions[index].Messages = chatMessages;

            }
            else
            {
                //load from cache
                chatMessages = chatSessions[index].Messages;
            }
            return chatMessages;

        }

        //User creates a new Chat Session
        public async Task CreateNewChatSessionAsync()
        {
            CosmosChatSession chatSession = new CosmosChatSession();

            chatSessions.Add(chatSession);
            
            await cosmos.InsertChatSessionAsync(chatSession);
                       
        }

        //User Inputs a chat from "New Chat" to user defined
        public async Task RenameChatSessionAsync(string chatSessionId, string newChatSessionName)
        {
            
            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].ChatSessionName = newChatSessionName;

            await cosmos.UpdateChatSessionAsync(chatSessions[index]);

        }

        //User deletes a chat session
        public async Task DeleteChatSessionAsync(string chatSessionId)
        {
            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions.RemoveAt(index);

            await cosmos.DeleteChatSessionAsync(chatSessionId);


        }

        //User prompt to ask OpenAI a question
        public async Task<string> AskOpenAiAsync(string chatSessionId, string prompt)
        {
            
            string conversation = GetChatSessionConversation(chatSessionId, prompt);

            CosmosChatCompletion completion = await openAi.AskAsync(chatSessionId, conversation);

            
            // Add prompt and completion to the chat session message list object.
            List<CosmosChatMessage> cosmosChatMessages = new List<CosmosChatMessage>();
            cosmosChatMessages.Add(CreatePromptMessage(chatSessionId, completion.PromptTokens, prompt));
            cosmosChatMessages.Add(CreateCompletionMessage(chatSessionId, completion.CompletionTokens, completion.Completion));


            //Insert into Cosmos as a Transaction
            await cosmos.InsertChatMessagesBatchAsync(cosmosChatMessages);

            return completion.Completion;

        }

        private string GetChatSessionConversation(string chatSessionId, string prompt)
        {
            string conversation = "";

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            if (chatSessions[index].Messages.Count > 0)
            {
                List<CosmosChatMessage> chatMessages = chatSessions[index].Messages;

                
                foreach(CosmosChatMessage chatMessage in chatMessages)
                {

                    conversation += chatMessage.Text + "\n";
                    
                }

                conversation += prompt;

                if ((conversation.Length) > maxConversationLength)
                    conversation = conversation.Substring((conversation.Length) - maxConversationLength, maxConversationLength);

            }

            return conversation;
        }

        public async Task<string> SummarizeChatSessionNameAsync(string chatSessionId, string prompt)
        {
            
            string response = await openAi.SummarizeAsync(chatSessionId, prompt);

            await RenameChatSessionAsync(chatSessionId, response);

            return response;

        }


        // Add user prompt to the chat session message list object.
        private CosmosChatMessage CreatePromptMessage(string chatSessionId, int tokens, string text)
        {
            CosmosChatMessage chatMessage = new CosmosChatMessage(chatSessionId, "User", tokens, text);

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].AddMessage(chatMessage);

            return chatMessage;

            //await cosmos.InsertChatMessageAsync(chatMessage);

        }

        // Add OpenAI completion to the chat session message list object.
        private CosmosChatMessage CreateCompletionMessage(string chatSessionId, int tokens, string text)
        {
            CosmosChatMessage chatMessage = new CosmosChatMessage(chatSessionId, "Assistant", tokens, text);

            int index = chatSessions.FindIndex(s => s.ChatSessionId == chatSessionId);

            chatSessions[index].AddMessage(chatMessage);

            return chatMessage;

            //await cosmos.InsertChatMessageAsync(chatMessage);

        }

    }
}
