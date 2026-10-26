using XanthosResidentialAI.Components;
using XanthosResidentialAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// One-flow services (27 Oct skeleton).
builder.Services.AddScoped<IXanthosScoreClient, XanthosScoreClient>();
builder.Services.AddScoped<IMetaModelClient, MetaModelClient>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddHttpClient<IPhotoPipelineClient, PhotoPipelineClient>((services, http) =>
{
    // Local Python photo pipeline (python/photo-pipeline). Localhost URL is
    // not a secret; it is plain configuration, no credentials involved.
    var baseUrl = services.GetRequiredService<IConfiguration>()["PhotoPipeline:BaseUrl"]
                  ?? "http://localhost:8000";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromMinutes(2); // first CLIP call can be slow
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
