using FHIRcastSandbox.Rules;
using Hangfire.MemoryStorage;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Threading;
using FHIRcastSandbox.Hub.Core;

namespace FHIRcastSandbox {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc();
            services.AddHangfire(config => config
                .UseNLogLogProvider()
                .UseMemoryStorage());
            services.AddTransient<ISubscriptionValidator, SubscriptionValidator>();
            services.AddSingleton<ISubscriptions, HubSubscriptionCollection>();
            services.AddSingleton<INotifications<HttpResponseMessage>, Notifications<HttpResponseMessage>>();
            services.AddSingleton<IContexts, Contexts>();
            services.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
            services.AddTransient<ValidateSubscriptionJob>();
            services.AddTransient<Sockets>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseHangfireServer();
            app.UseStaticFiles();
            app.UseWebSockets();

            JobActivator.Current = new ServiceProviderJobActivator(app.ApplicationServices);

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        Sockets sockets = (Sockets) app.ApplicationServices.GetService(typeof(Sockets));
                        await sockets.AddSocket(webSocket);
                        //await Echo(context, webSocket);
                        //await Sockets.HandleSocketMessageAsync(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
        }

        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }

    internal class ServiceProviderJobActivator : JobActivator {
        private IServiceProvider serviceProvider;

        public ServiceProviderJobActivator(IServiceProvider serviceProvider) {
            this.serviceProvider = serviceProvider;
        }

        public override object ActivateJob(Type jobType) {
            return this.serviceProvider.GetService(jobType);
        }
    }
}
