namespace Nyk.Utilities.TableInfrastructure
{
	/// <summary>
	/// I am basing this incredibly rudimentary infrastructure off of an elegant and versatile capability that APT used.
	/// In this implementation, I am simply leveraging the table name capabilities to unlock easier mocking and testing
	/// </summary>
	public interface ITableProvider
	{
		/// <summary>
		/// Returns a fully qualified table name
		/// </summary>
		string Table(string name, string schema = null);
	}
}