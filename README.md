# MoreRAGFun

Took fully inspritaiton from this example but I wanted to trim it down in a standalone console app and target Azure AI.

https://github.com/MozzieBytes/semantic-kernel/tree/main/dotnet/samples/Demos/VectorStoreRAG
 
 User Secrets:

```
 {
  "AzureOpenAIConfig": {
    "Endpoint": "https://my-ai.openai.azure.com/",
    "ApiKey": "",
    "ChatDeploymentName": "gpt-4o",
    "TextEmbeddingDeploymentName": "text-embedding-ada-002"
  },
  "AzureSearchConfig": {
    "CollectionName": "default",
    "Endpoint": "https://my-ai-search.search.windows.net",
    "ApiKey": ""
  }
}
```