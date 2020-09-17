using EventStore.ClientAPI;
using Importer.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Importer
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddSingleton(provider =>
            {
                var connection = EventStoreConnection.Create(
                    connectionString: Configuration.GetValue<string>("EventStore:ConnectionString"),
                    builder: ConnectionSettings.Create().KeepReconnecting(),
                    connectionName: Configuration.GetValue<string>("EventStore:ConnectionName"));
                connection.ConnectAsync().Wait(Configuration.GetValue<int>("EventStore:ConnectionTimeout"));
                return connection;
            });
            services.AddTransient<IRebalanceRepository, RebalanceRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Import API V1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
