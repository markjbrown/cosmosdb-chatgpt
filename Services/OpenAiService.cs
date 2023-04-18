using Azure;
using Azure.AI.OpenAI;
using cosmosdb_chatgpt.Models;

namespace cosmosdb_chatgpt.Services
{
    public class OpenAiService
    {

        private readonly OpenAIClient client;
        private readonly string deployment;
        private readonly int maxTokens;

        private ChatMessage systemPrompt { get; set; }
        private ChatMessage summarizePrompt { get; set; }

        public OpenAiService(IConfiguration configuration) 
        {
            

            string openAiUri = configuration["OpenAi.Endpoint"];
            string openAiKey = configuration["OpenAi.Key"];
            deployment = configuration["OpenAi.Deployment"];
            maxTokens = int.Parse(configuration["OpenAi.MaxTokens"]);

            client = new(new Uri(openAiUri), new AzureKeyCredential(openAiKey));


            string systemText = "You are an AI assistant that helps people find information. \n";
            systemText += "Provide concise answers that are polite and professional. \n";
            systemText += "If you do not know an answer, reply with 'I do not know the answer to your question.'\n";


            systemPrompt = new ChatMessage(ChatRole.System, systemText);

            summarizePrompt = new ChatMessage(ChatRole.System, "\n Summarize the following text in one or two words to use as a label on a web page");

        }

        public async Task<CosmosChatCompletion> AskAsync(string chatSessionId, string prompt)
        {

            ChatMessage userPrompt = new ChatMessage(ChatRole.User, prompt);


            Response<ChatCompletions> response = await
                client.GetChatCompletionsAsync(
                    deploymentOrModelName: deployment,
                    new ChatCompletionsOptions()
                    {
                        Messages = {
                            systemPrompt,
                            userPrompt
                        },
                        User = chatSessionId,
                        Temperature = (float)0.5,
                        MaxTokens = maxTokens,
                        NucleusSamplingFactor = (float)0.95,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0
                    });


            ChatCompletions completions = response.Value;

            CosmosChatCompletion cosmosChatCompletion = new CosmosChatCompletion(
                chatSessionId, 
                completions.Usage.PromptTokens, 
                completions.Usage.CompletionTokens, 
                completions.Choices[0].Message.Content);
            
            

            return cosmosChatCompletion;

        }

        public async Task<string> SummarizeAsync(string chatSessionId, string prompt)
        {

            
            ChatMessage userPrompt = new ChatMessage(ChatRole.User, prompt);


            Response<ChatCompletions> response = await
                client.GetChatCompletionsAsync(
                    deploymentOrModelName: deployment,
                    new ChatCompletionsOptions()
                    {
                        Messages = {
                            systemPrompt,
                            summarizePrompt,
                            userPrompt
                        },
                        User = chatSessionId,
                        Temperature = (float)0.5,
                        MaxTokens = maxTokens,
                        NucleusSamplingFactor = (float)0.95,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0
                    });


            ChatCompletions completions = response.Value;


            return completions.Choices[0].Message.Content;

        }

    }

}
