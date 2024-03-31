using System.Net.Http.Headers;
using Finbuckle.Website.Infrastructure;
using Finbuckle.Website.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();

builder.Services.AddSingleton<MailService>();
builder.Services.Configure<AmazonSesOptions>(builder.Configuration.GetSection("AmazonSesOptions"));

builder.Services.AddSingleton<DocVersionService>();

builder.Services.AddHttpClient("/MultiTenant/Docs", client =>
{
    client.BaseAddress = new Uri("https://raw.githubusercontent.com/Finbuckle/Finbuckle.MultiTenant/");
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Finbuckle.Website", null));
});

builder.Services.AddSingleton<DocVersionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var docVersionService = sp.GetRequiredService<DocVersionService>();
    await docVersionService.LoadAsync();
}

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