using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TruthOrDare_Contract;
using TruthOrDare_Contract.IRepository;
using TruthOrDare_Contract.IServices;
using TruthOrDare_Core.Services;
using TruthOrDare_Infrastructure;
using TruthOrDare_Infrastructure.Repository;

namespace TruthOrDare_API
{
    public static class DIConfig
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
        {
            //Add Repository
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            //Add service
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IWebSocketHandler, WebSocketHandler>();
            services.AddScoped<IPasswordHashingService, PasswordHashingService>();
            // Register MongoDbContext
            services.AddSingleton<MongoDbContext>(sp => new MongoDbContext(sp.GetRequiredService<IConfiguration>()));
            //Register AutoNextPlayerService
            services.AddHostedService<AutoNextPlayerService>();
            return services;
        }
    }
}
