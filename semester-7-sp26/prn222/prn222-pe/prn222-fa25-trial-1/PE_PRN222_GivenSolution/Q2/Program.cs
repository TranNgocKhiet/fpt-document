using Microsoft.EntityFrameworkCore;
using Q2.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionStr));
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Book}/{action=Index}/{id?}");

app.Run();
