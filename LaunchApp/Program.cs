using LaunchApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMasaBlazor(builder =>
{ 
    builder.ConfigureTheme(theme =>
    {
        theme.Themes.Light.Primary = "#807699";
        theme.Themes.Light.Accent = "#806dad";
        theme.Themes.Dark.Primary = "#5d4698";
        theme.Themes.Dark.Accent = "#6750A4";
    });
});

await builder.Build().RunAsync();