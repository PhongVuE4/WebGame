using TruthOrDare_API;
using TruthOrDare_Contract.IServices;
using Newtonsoft.Json;
using TruthOrDare_Contract.Models;
using Quartz;
using TruthOrDare_Core.Services;
using TruthOrDare_Common.Middleware;
using Microsoft.AspNetCore.Mvc;
using TruthOrDare_Common.Exceptions;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "TruthOrDare API", Version = "v1" });
    // Thêm mô tả chi tiết cho tham số filters
    c.ParameterFilter<FiltersParameterFilter>();
});
builder.Services.AddDependencyInjection();
// Tùy chỉnh xử lý lỗi validation
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Chuyển ModelState thành dictionary của lỗi
        var errors = context.ModelState
            .Where(e => e.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        // Ném ValidationException
        throw new ValidationException(errors);
    };
});
// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.WithOrigins("https://webgame-lk6s.onrender.com",
            "http://localhost:3000",
            "https://leminhhien.me",
            "http://localhost:3001",
            "https://webgame-oqyj-g.fly.dev",
            "https://leminhhien.id.vn")
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
// Cấu hình Quartz
builder.Services.AddQuartz(q =>
{
    // Đăng ký job
    var jobKey = new JobKey("CleanupJob");
    q.AddJob<CleanupJob>(opts => opts.WithIdentity(jobKey));

    // Cấu hình trigger
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("CleanupTrigger")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule
        //.WithCronSchedule("0 * * ? * *")); // Chạy mỗi phút 
            .WithIntervalInHours(24)
            .RepeatForever()));
    //Mỗi phút: "0 * * ? * *"
    //Mỗi 5 phút: "0 0/5 * ? * *"
    //Mỗi giờ: "0 0 * ? * *"
});
builder.Services.AddQuartzHostedService();
var app = builder.Build();

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
app.UseExceptionMiddleware();
app.MapControllers();

app.Run();
