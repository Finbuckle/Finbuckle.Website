using Finbuckle.Website.Infrastructure;
using Finbuckle.Website.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<BackgroundSyncService>();
builder.Services.AddRazorComponents();

// builder.Services.AddSingleton<GraphQLHttpClient>(sp =>
//     new GraphQLHttpClient("https://api.github.com/graphql", new SystemTextJsonSerializer()));
// builder.Services.AddTransient<GitHubSponsorService>();
// builder.Services.Configure<GitHubSponsorServiceOptions>(builder.Configuration.GetSection("GitHubSponsorServiceOptions"));

builder.Services.AddSingleton<MailService>();
builder.Services.Configure<MailService.AmazonSesOptions>(builder.Configuration.GetSection("AmazonSesOptions"));
builder.Services.AddSingleton<DocVersionService>();
builder.Services.AddSingleton<BlogService>();

var app = builder.Build();

await app.Services.GetRequiredService<DocVersionService>().LoadAsync();
await app.Services.GetRequiredService<BlogService>().LoadAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithRedirects("/Error");

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>();

app.Run();