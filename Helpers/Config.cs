using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreRAGFun.Helpers;

public class AzureOpenAIConfig
{
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }

    public string ChatDeploymentName { get; set; }
    public string TextEmbeddingDeploymentName { get; set; }
}

public class AzureSearchConfig
{
    public string CollectionName { get; set; }

    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
}