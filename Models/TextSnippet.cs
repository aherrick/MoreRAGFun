﻿#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace MoreRAGFun.Models;

/// <summary>
/// Data model for storing a section of text with an embedding and an optional reference link.
/// </summary>
/// <typeparam name="TKey">The type of the data model key.</typeparam>
public class TextSnippet<TKey>
{
    [VectorStoreRecordKey]
    public required TKey Key { get; set; }

    [VectorStoreRecordData]
    public string Text { get; set; }

    [VectorStoreRecordData]
    public string ReferenceDescription { get; set; }

    [VectorStoreRecordData]
    public string ReferenceLink { get; set; }

    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> TextEmbedding { get; set; }
}