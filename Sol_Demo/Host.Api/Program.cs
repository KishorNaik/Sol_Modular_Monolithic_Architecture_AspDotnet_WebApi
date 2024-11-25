var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();
DotNetEnv.Env.TraversePath().Load();

// Add services to the container.
builder.AddServices();
builder.AddModules();

var app = builder.Build();

// Map Middlewares
app.MapMiddlewares(builder);

app.Run();