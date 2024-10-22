#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using MoreRAGFun.Models;

namespace MoreRAGFun;

public class KernelHelper
{
    public static Kernel GetKernel(
        Helpers.AzureOpenAIConfig azureOpenAIConfig,
        Helpers.AzureSearchConfig azureSearchConfig
    )
    {
        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: azureOpenAIConfig.ChatDeploymentName,
            endpoint: azureOpenAIConfig.Endpoint,
            apiKey: azureOpenAIConfig.ApiKey
        );

        kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
            deploymentName: azureOpenAIConfig.TextEmbeddingDeploymentName,
            endpoint: azureOpenAIConfig.Endpoint,
            apiKey: azureOpenAIConfig.ApiKey
        );

        kernelBuilder.AddAzureAISearchVectorStoreRecordCollection<TextSnippet<string>>(
            azureSearchConfig.CollectionName,
            new Uri(azureSearchConfig.Endpoint),
            new AzureKeyCredential(azureSearchConfig.ApiKey)
        );

        kernelBuilder.AddVectorStoreTextSearch<TextSnippet<string>>(
            new TextSearchStringMapper((result) => (result as TextSnippet<string>).Text),
            new TextSearchResultMapper(
                (result) =>
                {
                    // Create a mapping from the Vector Store data type to the data type returned by the Text Search.
                    // This text search will ultimately be used in a plugin and this TextSearchResult will be returned to the prompt template
                    // when the plugin is invoked from the prompt template.
                    var castResult = result as TextSnippet<string>;
                    return new TextSearchResult(value: castResult.Text)
                    {
                        Name = castResult.ReferenceDescription,
                        Link = castResult.ReferenceLink
                    };
                }
            )
        );

        var kernel = kernelBuilder.Build();

        return kernel;
    }
}