﻿using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Plugins;

// Create kernel
var builder = Kernel.CreateBuilder();
string deploymentName = Environment.GetEnvironmentVariable("deploymentname", EnvironmentVariableTarget.User)!;
string endpoint = Environment.GetEnvironmentVariable("endpoint", EnvironmentVariableTarget.User)!;
string apiKey = Environment.GetEnvironmentVariable("apiKey", EnvironmentVariableTarget.User)!;
builder.Services.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
// register native plugin with the kernel
builder.Plugins.AddFromType<TimePlugin>();
//builder.Plugins.AddFromType<ModelsPlugin>();
//builder.Plugins.AddFromType<CalcPlugin>();
//builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddConsole().SetMinimumLevel(LogLevel.Information));
var kernel = builder.Build();

// Create chat history
ChatHistory history = [];

// Get chat completion service
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Start the conversation
while (true)
{
    // Get user input
    Console.Write("User > ");
    history.AddUserMessage(Console.ReadLine()!);

    // enable auto function calling
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // Get the response from the AI
    var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Stream the results
    string fullMessage = "";
    var first = true;
    await foreach (var content in result)
    {
        if (content.Role.HasValue && first)
        {
            Console.Write("Assistant > ");
            first = false;
        }
        Console.Write(content.Content);
        fullMessage += content.Content;
    }
    Console.WriteLine();

    // Add the message from the agent to the chat history
    history.AddAssistantMessage(fullMessage);
}