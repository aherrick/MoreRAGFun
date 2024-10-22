#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0020 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Net;
using System.Threading;
using Azure;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using MoreRAGFun.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using static System.Net.Mime.MediaTypeNames;

namespace MoreRAGFun.Helpers;

public static class DataHelper
{
    public static async Task LoadData(
        IChatCompletionService chatCompletionService,
        ITextEmbeddingGenerationService textEmbeddingGenerationService,
        IVectorStoreRecordCollection<string, TextSnippet<string>> vectorStoreRecordService
    )
    {
        // just delete and readd for
        await vectorStoreRecordService.DeleteCollectionAsync();

        await vectorStoreRecordService.CreateCollectionIfNotExistsAsync();

        // get the data
        var pdfPath = "semantic-kernel.pdf";
        var rawContentList = GetRawContent(pdfPath);

        var batches = rawContentList.Chunk(10);

        // Process each batch of content items for images
        foreach (var batch in batches)
        {
            var textContentTasks = batch.Select(async content =>
            {
                return await GetTextContentFromImages(content, chatCompletionService);
            });

            var textContentLocal = await Task.WhenAll(textContentTasks);

            // Map each paragraph to a TextSnippet and generate an embedding for it.
            var recordTasks = textContentLocal.Select(async content => new TextSnippet<string>
            {
                Key = Guid.NewGuid().ToString(),
                Text = content.Text,
                ReferenceDescription = $"{new FileInfo(pdfPath).Name}#page={content.PageNumber}",
                ReferenceLink =
                    $"{new Uri(new FileInfo(pdfPath).FullName).AbsoluteUri}#page={content.PageNumber}",
                TextEmbedding = await GetEmbeddings(content.Text, textEmbeddingGenerationService)
            });

            // Upsert the records into the vector store.
            var records = await Task.WhenAll(recordTasks);

            var upsertedKeys = vectorStoreRecordService.UpsertBatchAsync(records);
            await foreach (var key in upsertedKeys.ConfigureAwait(false))
            {
                Console.WriteLine($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(10_000);
        }
    }

    private static List<RawContent> GetRawContent(string pdfPath)
    {
        var rawContentList = new List<RawContent>();
        using (PdfDocument document = PdfDocument.Open(pdfPath))
        {
            foreach (Page page in document.GetPages())
            {
                foreach (var image in page.GetImages())
                {
                    if (image.TryGetPng(out var png))
                    {
                        rawContentList.Add(
                            new RawContent { Image = png, PageNumber = page.Number }
                        );
                    }
                }

                var blocks = DefaultPageSegmenter.Instance.GetBlocks(page.GetWords());
                foreach (var block in blocks)
                {
                    rawContentList.Add(
                        new RawContent { Text = block.Text, PageNumber = page.Number }
                    );
                }
            }
        }

        return rawContentList;
    }

    private static async Task<RawContent> GetTextContentFromImages(
        RawContent content,
        IChatCompletionService chatCompletionService
    )
    {
        if (content.Text != null)
        {
            return content;
        }

        var tries = 0;

        while (true)
        {
            try
            {
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(
                    [
                        new TextContent("What’s in this image?"),
                        new ImageContent(content.Image, "image/png"),
                    ]
                );
                var result = await chatCompletionService.GetChatMessageContentsAsync(chatHistory);
                var textFromImage = string.Join("\n", result.Select(x => x.Content));

                return new RawContent { Text = textFromImage, PageNumber = content.PageNumber };
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate text from image. Error: {ex}");
                    Console.WriteLine("Retrying text to image conversion...");
                    await Task.Delay(10_000);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    private static async Task<ReadOnlyMemory<float>> GetEmbeddings(
        string text,
        ITextEmbeddingGenerationService textEmbeddingGenerationService
    )
    {
        var tries = 0;
        while (true)
        {
            try
            {
                return await textEmbeddingGenerationService.GenerateEmbeddingAsync(text);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    Console.WriteLine($"Failed to generate embedding. Error: {ex}");
                    Console.WriteLine("Retrying embedding generation...");
                    await Task.Delay(10_000);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}