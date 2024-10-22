#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using MoreRAGFun;
using MoreRAGFun.Helpers;
using MoreRAGFun.Models;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var azureOpenAIConfig = config.GetSection(nameof(AzureOpenAIConfig)).Get<AzureOpenAIConfig>();
var azureSearchConfig = config.GetSection(nameof(AzureSearchConfig)).Get<AzureSearchConfig>();

var kernel = KernelHelper.GetKernel(azureOpenAIConfig, azureSearchConfig);

/// SERVICES
///
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

var textEmbeddingGenerationService =
    kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();

var vectorStoreRecordService = kernel.GetRequiredService<
    IVectorStoreRecordCollection<string, TextSnippet<string>>
>();

var vectorStoreSearchService = kernel.GetRequiredService<
    VectorStoreTextSearch<TextSnippet<string>>
>();

/// LOAD DATA
///

await DataHelper.LoadData(
    chatCompletionService,
    textEmbeddingGenerationService,
    vectorStoreRecordService
);

/// CHAT WITH DATA!

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Assistant > Press enter with no prompt to exit.");

// Add a search plugin to the kernel which we will use in the template below
// to do a vector search for related information to the user query.
kernel.Plugins.Add(vectorStoreSearchService.CreateWithGetTextSearchResults("SearchPlugin"));

// Start the chat loop.
while (true)
{
    // Prompt the user for a question.
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Assistant > What would you like to know from the loaded PDFs?");

    // Read the user question.
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write("User > ");
    var question = Console.ReadLine();

    // Exit the application if the user didn't type anything.
    if (string.IsNullOrWhiteSpace(question))
    {
        break;
    }

    // Invoke the LLM with a template that uses the search plugin to
    // 1. get related information to the user query from the vector store
    // 2. add the information to the LLM prompt.
    var response = kernel.InvokePromptStreamingAsync(
        promptTemplate: """
        Please use this information to answer the question:
        {{#with (SearchPlugin-GetTextSearchResults question)}}
          {{#each this}}
            Name: {{Name}}
            Value: {{Value}}
            Link: {{Link}}
            -----------------
          {{/each}}
        {{/with}}

        Include citations to the relevant information where it is referenced in the response.

        Question: {{question}}
        """,
        arguments: new KernelArguments() { { "question", question }, },
        templateFormat: "handlebars",
        promptTemplateFactory: new HandlebarsPromptTemplateFactory()
    );

    // Stream the LLM response to the console with error handling.
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nAssistant > ");

    try
    {
        await foreach (var message in response)
        {
            Console.Write(message);
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Call to LLM failed with error: {ex}");
    }
}