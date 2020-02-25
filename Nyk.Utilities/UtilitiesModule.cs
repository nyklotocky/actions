using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Nyk.Utilities.TableInfrastructure;

namespace Nyk.Utilities
{
	public class UtilitiesModule
	{
		/// <summary>
		/// Registers utilities useful for various tasks
		///
		/// Note: assume this project is tested. It contains some misc capabilities that I've used in other places, and would
		/// be a thoroughly tested part of an infrastructural library
		/// Also note: that in a truly production environment I would have been leveraging Autofac rather than .NET Core's out of the box
		/// IoC, and would have had this class extend Autofac.Module, and be registered via the Load override
		/// </summary>
		public static void Register(IServiceCollection services, string sqlConnectionString)
		{
			services.AddTransient<ISqlQuery>(_ => new SqlQuery(sqlConnectionString));

			var databaseName = new SqlConnectionStringBuilder(sqlConnectionString).InitialCatalog;
			services.AddTransient<ITableProvider>(_ => new RudimentaryTableProvider(databaseName));
		}
	}
}