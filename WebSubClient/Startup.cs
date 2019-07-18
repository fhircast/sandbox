using FHIRcastSandbox.Hubs;
using FHIRcastSandbox.WebSubClient.Hubs;
using FHIRcastSandbox.WebSubClient.Rules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FHIRcastSandbox.WebSubClient {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc();
            services.AddSignalR();
            services.AddSingleton<ClientSubscriptions>();
            services.AddTransient<IHubSubscriptions, HubSubscriptions>();
            services.AddTransient(typeof(WebSubClientHub));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            //app.UseMvc(routes => {
            //    routes.MapRoute(
            //        name: "default",
            //        template: "{controller=Home}/{action=Index}/{id?}");
            //});

            app.UseSignalR(routes => {
                routes.MapHub<FHIRcastSandbox.Hubs.WebSubClientHub>("/websubclienthub");
            });

            app.UseMvc();
        }
    }
}
