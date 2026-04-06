using Microsoft.EntityFrameworkCore;
using Q2.Data;

var builder = WebApplication.CreateBuilder(args);

//Use the connection string below to connect to the database.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<PRN222_26SprB1_1>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Customer}/{action=List}");

app.Run();
