using Microsoft.EntityFrameworkCore;
using Q2.Data;
using Q2.Repositories;
using Q2.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionStr = builder.Configuration.GetConnectionString("MyCnn");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connectionStr));

// Register DAL and BLL
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IBookService, BookService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute(name: "default", pattern: "{controller=Book}/{action=Index}/{id?}");

app.Run();