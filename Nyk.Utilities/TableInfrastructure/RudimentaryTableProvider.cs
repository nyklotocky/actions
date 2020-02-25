namespace Nyk.Utilities.TableInfrastructure
{
	public class RudimentaryTableProvider : ITableProvider
	{
		private readonly string _databaseName;

		public RudimentaryTableProvider(string databaseName)
		{
			this._databaseName = databaseName;
		}

		public string Table(string name, string schema)
		{
			return $"{this._databaseName}.{schema ?? "dbo"}.{name}";
		}
	}
}