## Disclaimer: This is a work in progress and is not yet ready for production use.

# How to use OAuth with LichessNET

LichessNET provides some classes to help you authenticate with Lichess using OAuth.
The first one is the AccountController class, which is responsible for handling the OAuth flow.
The second one is the OAuthToken class, which is responsible for storing the OAuth token.

## Include the code in your project

```csharp
using LichessNET.Entities.Enumerations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddOAuthAuthentication(builder.Configuration, LichessScope.EmailRead);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseOAuthAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

For a login to lichess, you will have to add

```html
<a href="/login">Login</a>
```

## Configuration

To configure OAuth, you will have to add the following to your appsettings.json file

```json
{
  "Lichess": {
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

to your appsettings.json file.