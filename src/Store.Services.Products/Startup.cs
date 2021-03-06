﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Dnc.Common.Mongo;
using Dnc.Common.Mvc;
using Dnc.Common.RabbitMq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Store.Messages.Products;
using Store.Services.Products.Domain;

namespace Store.Services.Products
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IContainer Container { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddDefaultJsonOptions();
            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.AddRabbitMq();
            builder.RegisterAssemblyTypes(typeof(Startup).Assembly)
                   .AsImplementedInterfaces();

            builder.AddMongoDB();
            builder.AddMongoDBRepository<Product>("Products");

            Container = builder.Build();

            return new AutofacServiceProvider(Container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime, IMongoDbInitializer mongoDbInitializer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseMvc();
            app.UseRabbitMq()
               .SubscribeCommand<CreateProduct>();

            mongoDbInitializer.InitializeAsync();

            applicationLifetime.ApplicationStopped.Register(() => Container.Dispose());
        }
    }
}
