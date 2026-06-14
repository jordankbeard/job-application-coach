using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

return await Deployment.RunAsync(async () =>
{
    var subscriptionId = (await GetClientConfig.InvokeAsync()).SubscriptionId;

    // ── Stack configuration ───────────────────────────────────────────────────
    // OCP: adding an environment means adding a new case here — the provisioning
    // code below never changes. Using a record instead of a string dictionary
    // gives compile-time exhaustiveness and IDE navigation.
    var cfg = Deployment.Instance.StackName switch
    {
        "dev" => new StackConfig(
            ResourceGroupName:  "playground",
            Location:           "ukwest",
            FuncName:           "job-app-coach-dev",
            DocIntelName:       "jobappcoach-docintel",
            DocIntelEndpoint:   "https://jobappcoach-docintel.cognitiveservices.azure.com/",
            SearchName:         "job-app-coach",
            SearchEndpoint:     "https://job-app-coach.search.windows.net",
            OpenAiName:         "dometrain-jordan-test",
            OpenAiEndpoint:     "https://dometrain-jordan-test.openai.azure.com/",
            OpenAiResourceGroup: null,
            ChatDeployment:     "gpt-4o-mini",
            EmbeddingDeployment:"text-embedding-3-small"
        ),
        "prod" => new StackConfig(
            ResourceGroupName:  "playground",
            Location:           "ukwest",
            FuncName:           "job-app-coach",
            DocIntelName:       "jobappcoach-docintel",
            DocIntelEndpoint:   "https://jobappcoach-docintel.cognitiveservices.azure.com/",
            SearchName:         "job-app-coach",
            SearchEndpoint:     "https://job-app-coach.search.windows.net",
            OpenAiName:         "dometrain-jordan-test",
            OpenAiEndpoint:     "https://dometrain-jordan-test.openai.azure.com/",
            OpenAiResourceGroup: null,
            ChatDeployment:     "gpt-4o-mini",
            EmbeddingDeployment:"text-embedding-3-small"
        ),
        var name => throw new InvalidOperationException(
            $"Unknown Pulumi stack '{name}'. Add a case for it in Program.cs.")
    };

    var openAiRg = cfg.OpenAiResourceGroup ?? cfg.ResourceGroupName;

    // ── ARM resource IDs for existing resources ───────────────────────────────
    var docIntelResourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{cfg.ResourceGroupName}/providers/Microsoft.CognitiveServices/accounts/{cfg.DocIntelName}";
    var searchResourceId   = $"/subscriptions/{subscriptionId}/resourceGroups/{cfg.ResourceGroupName}/providers/Microsoft.Search/searchServices/{cfg.SearchName}";
    var openAiResourceId   = $"/subscriptions/{subscriptionId}/resourceGroups/{openAiRg}/providers/Microsoft.CognitiveServices/accounts/{cfg.OpenAiName}";

    string RoleDefId(string roleGuid) =>
        $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleGuid}";

    // ── Storage account ───────────────────────────────────────────────────────
    var storage = new StorageAccount("storage", new StorageAccountArgs
    {
        ResourceGroupName     = cfg.ResourceGroupName,
        Location              = cfg.Location,
        Sku                   = new SkuArgs { Name = SkuName.Standard_LRS },
        Kind                  = Kind.StorageV2,
        AllowBlobPublicAccess = false,
        MinimumTlsVersion     = MinimumTlsVersion.TLS1_2,
    });

    var primaryKey = ListStorageAccountKeys
        .Invoke(new ListStorageAccountKeysInvokeArgs
        {
            AccountName       = storage.Name,
            ResourceGroupName = cfg.ResourceGroupName,
        })
        .Apply(r => r.Keys[0].Value);

    var storageConnectionString = Output.Format(
        $"DefaultEndpointsProtocol=https;AccountName={storage.Name};AccountKey={primaryKey};EndpointSuffix=core.windows.net");

    // ── App Service Plan (Consumption / Y1) ──────────────────────────────────
    var plan = new AppServicePlan("plan", new AppServicePlanArgs
    {
        ResourceGroupName = cfg.ResourceGroupName,
        Location          = cfg.Location,
        Kind              = "FunctionApp",
        Sku               = new SkuDescriptionArgs { Name = "Y1", Tier = "Dynamic" },
        Reserved          = true,
    });

    // ── Function App ──────────────────────────────────────────────────────────
    var functionApp = new WebApp("func", new WebAppArgs
    {
        Name              = cfg.FuncName,
        ResourceGroupName = cfg.ResourceGroupName,
        Location          = cfg.Location,
        ServerFarmId      = plan.Id,
        Kind              = "FunctionApp,linux",
        Identity = new ManagedServiceIdentityArgs
        {
            Type = ManagedServiceIdentityType.SystemAssigned,
        },
        SiteConfig = new SiteConfigArgs
        {
            LinuxFxVersion = "DOTNET-ISOLATED|8.0",
            AppSettings    = new[]
            {
                new NameValuePairArgs { Name = "AzureWebJobsStorage",                  Value = storageConnectionString },
                new NameValuePairArgs { Name = "FUNCTIONS_EXTENSION_VERSION",           Value = "~4" },
                new NameValuePairArgs { Name = "FUNCTIONS_WORKER_RUNTIME",             Value = "dotnet-isolated" },
                new NameValuePairArgs { Name = "AzureDocumentIntelligence__Endpoint",  Value = cfg.DocIntelEndpoint },
                new NameValuePairArgs { Name = "AzureAISearch__Endpoint",              Value = cfg.SearchEndpoint },
                new NameValuePairArgs { Name = "AzureAISearch__CvIndexName",           Value = "cv-chunks" },
                new NameValuePairArgs { Name = "AzureAISearch__JdIndexName",           Value = "jd-chunks" },
                new NameValuePairArgs { Name = "AzureOpenAI__Endpoint",                Value = cfg.OpenAiEndpoint },
                new NameValuePairArgs { Name = "AzureOpenAI__ChatDeployment",          Value = cfg.ChatDeployment },
                new NameValuePairArgs { Name = "AzureOpenAI__EmbeddingDeployment",     Value = cfg.EmbeddingDeployment },
            },
        },
    });

    var principalId = functionApp.Identity.Apply(id => id!.PrincipalId);

    // ── RBAC: managed identity → Azure AI services ────────────────────────────
    _ = new RoleAssignment("docIntelRole", new RoleAssignmentArgs
    {
        Scope            = docIntelResourceId,
        RoleDefinitionId = RoleDefId("a97b65f3-24c7-4388-baec-2e87135dc908"), // Cognitive Services User
        PrincipalId      = principalId,
        PrincipalType    = PrincipalType.ServicePrincipal,
    });

    _ = new RoleAssignment("openAiRole", new RoleAssignmentArgs
    {
        Scope            = openAiResourceId,
        RoleDefinitionId = RoleDefId("5e0bd9bd-7b93-4f28-af87-19fc36ad61bd"), // Cognitive Services OpenAI User
        PrincipalId      = principalId,
        PrincipalType    = PrincipalType.ServicePrincipal,
    });

    _ = new RoleAssignment("searchDataRole", new RoleAssignmentArgs
    {
        Scope            = searchResourceId,
        RoleDefinitionId = RoleDefId("8ebe5a00-799e-43f5-93ac-243d3dce84a7"), // Search Index Data Contributor
        PrincipalId      = principalId,
        PrincipalType    = PrincipalType.ServicePrincipal,
    });

    return new Dictionary<string, object?>
    {
        ["functionAppName"]     = functionApp.Name,
        ["functionAppUrl"]      = functionApp.DefaultHostName.Apply(h => $"https://{h}"),
        ["functionAppIdentity"] = principalId,
    };
});

// ── Stack configuration record ────────────────────────────────────────────────
record StackConfig(
    string  ResourceGroupName,
    string  Location,
    string  FuncName,
    string  DocIntelName,
    string  DocIntelEndpoint,
    string  SearchName,
    string  SearchEndpoint,
    string  OpenAiName,
    string  OpenAiEndpoint,
    string? OpenAiResourceGroup,
    string  ChatDeployment,
    string  EmbeddingDeployment
);
