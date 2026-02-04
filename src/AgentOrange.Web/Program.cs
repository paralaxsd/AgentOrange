using AgentOrange.Core;
using AgentOrange.Core.ChatSession;
using AgentOrange.Core.Embedding;
using AgentOrange.Web.Components;
using AgentOrange.Web.Services;
using AgentOrange.Web.Services.Ingestion;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.Configure<AgentChatConfig>(
    builder.Configuration.GetSection("AgentChatConfig"));
services.AddScoped<IAgentChatSessionFactory, AgentOrangeSessionFactory>();

// TODO: provide a custom embedding client via AgentOrange.Core
//var embeddingGenerator = ghModelsClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
//var credential = new ApiKeyCredential(builder.Configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. See the README for details."));
//var openAIOptions = new OpenAIClientOptions()
//{
//    Endpoint = new Uri("https://models.inference.ai.azure.com")
//};
//var ghModelsClient = new OpenAIClient(credential, openAIOptions);
//var embeddingGenerator = ghModelsClient.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";


services.AddRazorComponents().AddInteractiveServerComponents();
services.AddSqliteVectorStore(_ => vectorStoreConnectionString);
services.AddSqliteCollection<string, IngestedChunk>(IngestedChunk.CollectionName, vectorStoreConnectionString);
services.AddSingleton<DataIngestor>();
services.AddSingleton<SemanticSearch>();
services.AddSingleton<ResourceDisposer>();
services.AddKeyedSingleton("ingestion_directory", new DirectoryInfo(Path.Combine(builder.Environment.WebRootPath, "Data")));
services.AddSingleton<IEmbeddingGenerator>(sp =>
{
    var config = sp.GetRequiredService<IOptions<AgentChatConfig>>().Value;
    return new AgentOrangeEmbeddingGenerator();
});
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
    (IEmbeddingGenerator<string, Embedding<float>>)sp.GetRequiredService<IEmbeddingGenerator>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
