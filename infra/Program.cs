using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

return await Deployment.RunAsync(async () =>
{
    var subscriptionId = (await GetClientConfig.InvokeAsync()).SubscriptionId;

    // Base config shared across all stacks. Each stack overrides only what differs.
    // Adding a new environment = one new 'with' expression below.
    var baseConfig = new StackConfig(
        ResourceGroupName:   "playground",
        Location:            "ukwest",
        FuncName:            "job-app-coach",
        DocIntelName:        "jobappcoach-docintel",
        DocIntelEndpoint:    "https://jobappcoach-docintel.cognitiveservices.azure.com/",
        SearchName:          "job-app-coach",
        SearchEndpoint:      "https://job-app-coach.search.windows.net",
        OpenAiName:          "dometrain-jordan-test",
        OpenAiEndpoint:      "https://dometrain-jordan-test.openai.azure.com/",
        OpenAiResourceGroup: null,
        ChatDeployment:      "gpt-4o-mini",
        EmbeddingDeployment: "text-embedding-3-small"
    );

    var cfg = Deployment.Instance.StackName switch
    {
        "prod" => baseConfig,
        "dev"  => baseConfig with { FuncName = "job-app-coach-dev" },
        var n  => throw new InvalidOperationException($"Unknown stack '{n}'. Add a case in Program.cs.")
    };

    var openAiRg = cfg.OpenAiResourceGroup ?? cfg.ResourceGroupName;

    string ArmId(string rg, string provider, string name) =>
        $"/subscriptions/{subscriptionId}/resourceGroups/{rg}/providers/{provider}/{name}";

    string RoleDefId(string guid) =>
        $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{guid}";

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

    var plan = new AppServicePlan("plan", new AppServicePlanArgs
    {
        ResourceGroupName = cfg.ResourceGroupName,
        Location          = cfg.Location,
        Kind              = "FunctionApp",
        Sku               = new SkuDescriptionArgs { Name = "Y1", Tier = "Dynamic" },
        Reserved          = true,  // required for Linux
    });

    var functionApp = new WebApp("func", new WebAppArgs
    {
        Name              = cfg.FuncName,
        ResourceGroupName = cfg.ResourceGroupName,
        Location          = cfg.Location,
        ServerFarmId      = plan.Id,
        Kind              = "FunctionApp,linux",
        Identity          = new ManagedServiceIdentityArgs { Type = ManagedServiceIdentityType.SystemAssigned },
        SiteConfig        = new SiteConfigArgs
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

    // Grant the function app's managed identity the minimum roles needed to call
    // each AI service. Mirrors what the developer's personal account has locally.
    void Assign(string name, string scope, string roleGuid) =>
        new RoleAssignment(name, new RoleAssignmentArgs
        {
            Scope            = scope,
            RoleDefinitionId = RoleDefId(roleGuid),
            PrincipalId      = principalId,
            PrincipalType    = PrincipalType.ServicePrincipal,
        });

    Assign("docIntelRole",  ArmId(cfg.ResourceGroupName, "Microsoft.CognitiveServices/accounts", cfg.DocIntelName), "a97b65f3-24c7-4388-baec-2e87135dc908");
    Assign("openAiRole",    ArmId(openAiRg,              "Microsoft.CognitiveServices/accounts", cfg.OpenAiName),   "5e0bd9bd-7b93-4f28-af87-19fc36ad61bd");
    Assign("searchDataRole",ArmId(cfg.ResourceGroupName, "Microsoft.Search/searchServices",      cfg.SearchName),   "8ebe5a00-799e-43f5-93ac-243d3dce84a7");

    return new Dictionary<string, object?>
    {
        ["functionAppName"]     = functionApp.Name,
        ["functionAppUrl"]      = functionApp.DefaultHostName.Apply(h => $"https://{h}"),
        ["functionAppIdentity"] = principalId,
    };
});

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
