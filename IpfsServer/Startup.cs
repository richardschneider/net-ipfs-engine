using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ipfs.CoreApi;
using Ipfs.Engine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Ipfs.Server
{
    public class Startup
    {
        const string passphrase = "this is not a secure pass phrase";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddJsonOptions(jo =>
            {
                jo.SerializerSettings.ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new DefaultNamingStrategy()
                };
            });

            var ipfs = new IpfsEngine(passphrase.ToCharArray());
            ipfs.StartAsync().Wait();
            services.AddSingleton<ICoreApi>(ipfs);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
