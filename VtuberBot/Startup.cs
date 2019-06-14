using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using VtuberBot.Bot;
using VtuberBot.Bot.Processors;
using VtuberBot.Core;
using VtuberBot.Models;
using VtuberBot.Plugin;

namespace VtuberBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            var database = new MongoClient(Config.DefaultConfig.DatabaseUrl).GetDatabase("vtuber-bot-data");
            var observer = new VtuberBotObserver(database);
            foreach (var client in Config.DefaultConfig.Clients)
            {
                LogHelper.Info("正在初始化Bot " + client.ClientId);
                var bot = new Bot.VtuberBot { BotName = client.ClientId };
                foreach (var config in client.Services)
                {
                    var service = ServiceDispatcher.SearchBotService(config.ServiceType);
                    if (service == null)
                    {
                        LogHelper.Error("未找到服务类型 " + config.ServiceType);
                        continue;
                    }

                    service.ListenUrl = config.ListenUrl;
                    service.AccessToken = config.AccessToken;
                    service.ListenPort = config.ListenPort;
                    service.Level = config.Level;
                    service.WebsocketUrl = config.WsUrl;
                    service.Load();
                    bot.Services.Add(service);
                }
                bot.Init();
                observer.AddBot(bot);
                LogHelper.Info($"初始化 Bot {bot.BotName} 完成..");
            }
            observer.Processors.Add(new HelpProcessor()); //HELP
            observer.Processors.Add(new LiveProcessor());
            observer.Processors.Add(new VtuberProcessor());
            observer.Processors.Add(new SubscribeProcessor());
            observer.Processors.Add(new PluginProcessor());
            services.AddSingleton(serviceProvider => database);
            services.AddSingleton(serviceProvider => observer);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "http://api.bot.vtb.wiki",

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = BotJwt.IssuerSigningKey,

                        ValidateAudience = false,
                        ValidateLifetime = true,
                    };
                    options.SaveToken = true;
                });
            services.AddCors(option =>
                {
                    option.AddPolicy("Any", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
                });
            PluginManager.Manager.LoadPlugins(observer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseCors("Any");
            app.UseMvc();
        }
    }
}
