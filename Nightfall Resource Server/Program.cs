WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

WebApplication app = builder.Build();

app.Urls.Add("http://0.0.0.0:5000");

app.MapControllers();

app.Run();