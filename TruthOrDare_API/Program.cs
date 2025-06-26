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
using TruthOrDare_Core.Hubs;

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
            "https://leminhhien.id.vn",
            "http://localhost:8080",
            "https://localhost:8080")
               .AllowAnyMethod()
               .AllowAnyHeader()
                .AllowCredentials();
        //builder.WithOrigins("http://127.0.0.1:8080", "http://localhost:8080")
        //       .AllowAnyMethod()
        //       .AllowAnyHeader()
        //       .AllowCredentials();
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

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 128 * 1024 * 1024; // 128MB
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

app.MapHub<RoomHub>("/roomHub");

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseExceptionMiddleware();
app.MapControllers();

app.Run();
