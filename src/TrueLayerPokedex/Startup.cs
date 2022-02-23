using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TrueLayerPokedex.Application.Queries.GetBasicPokemonInfo;
using TrueLayerPokedex.Domain;
using TrueLayerPokedex.Domain.Options;
using TrueLayerPokedex.Infrastructure;
// ReSharper disable AssignNullToNotNullAttribute

namespace TrueLayerPokedex
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();

            services.AddMediatR(Assembly.GetAssembly(typeof(GetBasicPokemonInfoQuery)));
            services.AddInfrastructure(Configuration);
            services.AddScoped<IUtcNowProvider, UtcNowProvider>();

            services.Configure<CachingOptions>(Configuration.GetSection("Caching"));
            services.AddDistributedMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}