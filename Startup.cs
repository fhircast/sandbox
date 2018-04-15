using FHIRcastSandbox.Rules;
using Hangfire.MemoryStorage;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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
            services.AddSingleton<ISubscriptions, Subscriptions>();
            services.AddTransient<IBackgroundJobClient, BackgroundJobClient>();
            services.AddTransient<ValidateSubscriptionJob>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseHangfireServer();

            JobActivator.Current = new ServiceProviderJobActivator(app.ApplicationServices);
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
