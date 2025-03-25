using TruthOrDare_API;
using TruthOrDare_Contract.IServices;
using Newtonsoft.Json;
using TruthOrDare_Contract.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDependencyInjection();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("https://webgame-lk6s.onrender.com",
            "http://localhost:3000",
            "https://webgame-oqyj-g.fly.dev",
            "https://leminhhien.me")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


var app = builder.Build();

//app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TruthOrDare API v1");
//    c.RoutePrefix = string.Empty; 
//});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseCors("AllowAll");
app.UseSwagger(); // Kích hoạt Swagger trên mọi môi trường
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TruthOrDare API V1");
    c.RoutePrefix = "swagger"; // Truy cập Swagger UI tại /swagger
});
app.UseWebSockets();
app.Map("/ws/{roomId}/{playerId}", async (HttpContext context, IWebSocketHandler handler) =>
{
    var roomId = context.Request.RouteValues["roomId"]?.ToString();
    var playerId = context.Request.RouteValues["playerId"]?.ToString();
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync();
        await handler.HandleWebSocket(context, ws, roomId, playerId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
