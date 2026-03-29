using ChatApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Add controllers for file upload API

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 1 * 1024 * 1024; // 1MB per message (larger chunks for speed)
    options.ClientTimeoutInterval = TimeSpan.FromHours(2); // 2 hours for very large files
    options.HandshakeTimeout = TimeSpan.FromMinutes(5);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Keep connection alive
    options.EnableDetailedErrors = true;
    options.MaximumParallelInvocationsPerClient = 10; // Allow parallel chunk sending
});

// Configure file upload limits for large files (2GB)
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB max request
});

// Configure Kestrel for large files (2GB)
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 2L * 1024 * 1024 * 1024; // 2GB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
    options.Limits.KeepAliveTimeout = TimeSpan.FromHours(2);
    options.Limits.MinRequestBodyDataRate = null; // Disable minimum data rate for slow uploads
    options.Limits.MinResponseDataRate = null; // Disable minimum data rate for slow downloads
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers(); // Map controller routes
app.MapHub<ChatHub>("/chathub");

app.Run();