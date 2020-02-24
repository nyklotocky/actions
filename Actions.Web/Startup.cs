using System;
using Actions.Web.BusinessLogic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nyk.Utilities;

namespace Actions.Web
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
			services.AddControllers();

			// this value is stored in appSettings.json
			// we have a "Development" mapping applied with our local environment connection string
			// for a true production deployment, we would have a deployment layer of some sort generating a
			// appSettings.Deployment.json (for instance) to avoid committing sensitive passwords to the repo
			UtilitiesModule.Register(services, Configuration.GetConnectionString("ActionsTiming"));
			services.AddTransient<ActionsTimingRetriever>();
			services.AddTransient<ActionsTimingRecorder>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
