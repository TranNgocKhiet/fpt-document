var builder = WebApplication.CreateBuilder(args);

//Use the connection string below to connect to the database.
var connectionStr = builder.Configuration.GetConnectionString("MyCnn");

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
