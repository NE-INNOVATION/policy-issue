using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using policy_issue.Services;

namespace policy_issue
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
            services.AddCors(options =>
           {
               options.AddDefaultPolicy(
                               builder =>
                               {
                                   builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                               });
           });

           services.AddSingleton<DataModel>();

            services.AddSingleton<KafkaConsumer>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var life =  app.ApplicationServices.GetService<IHostApplicationLifetime>();
            var dataModel =  app.ApplicationServices.GetService<DataModel>();
            var consumer =  app.ApplicationServices.GetService<KafkaConsumer>();
            life.ApplicationStarted.Register(GetOnStarted(consumer, dataModel));
            life.ApplicationStopping.Register(GetOnStopped(consumer));

            app.UseRouting();

            app.UseCors();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static Action GetOnStarted(KafkaConsumer consumer, DataModel model)
        {
            return () => {
                    consumer.SetupConsume(model);
                };
        }

        private static Action GetOnStopped(KafkaConsumer consumer)
        {
            return () => {consumer.CloseConsume();};
        }
    }
}
