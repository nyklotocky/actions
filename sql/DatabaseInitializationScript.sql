CREATE DATABASE ActionsTiming;
GO

CREATE LOGIN Actions_Login WITH PASSWORD = 'VerbsRFun';

USE ActionsTiming;
	
CREATE USER Actions_User FOR LOGIN Actions_Login;

EXEC sp_addRoleMember
	@roleName = 'db_datareader'
	, @memberName = 'Actions_User';

EXEC sp_addRoleMember
	@roleName = 'db_datawriter'
	, @memberName = 'Actions_User';

GRANT CREATE TABLE TO Actions_User;
GRANT ALTER ON SCHEMA::dbo TO Actions_User;

/*
 * This table will serve as a space- and time-saving mechanism.
 * By recording a unique surrogate key for the action name, we don't need
 * to save the n characters of the name for each time we record an activity to have occurred.
 * Further, we can avoid doing string comparisons during data retrieval.
 *
 * Inserts into this table will need to be atomic.
 */
CREATE TABLE dbo.actions
(
	-- let us have actionId as our clustered index to optimize for JOIN performance
	actionId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_actions PRIMARY KEY
	/*
	 * maintain a unique constraint for data integrity to ensure that application code can't
	 * inadvertently insert duplicate actions
	 */
	, actionName NVARCHAR(128) CONSTRAINT UQ_actions_actionName UNIQUE
)

CREATE TABLE dbo.actionTimes
(
	-- surrogate key for ensuring ease of use for any future development we may want to do with against this data
	actionTimeId INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_actionTimes PRIMARY KEY
	, actionId INT NOT NULL CONSTRAINT FK_actionTimes_actions FOREIGN KEY REFERENCES dbo.actions
	/*
	 * this datatype is somewhat strange to me:
	 * were I to be in a discussion with a PM or other teammates, I would want to know what we were trying to capture
	 * or represent here. The examples are all integer values of some unknown unit, yet we apply a division operation
	 * which will quickly require greater precision. Additionally, a numeric value doesn't readily capture/represent "time"
	 * (as seen by the confusion around units). A .NET TimeSpan would be my ideal representation in C#, which can be persisted here
	 * in a variety of ways: https://stackoverflow.com/questions/8503825/what-is-the-correct-sql-type-to-store-a-net-timespan-with-values-240000
	 */
	, actionTime FLOAT NOT NULL

	/*
	 * This is going to be our biggest performance question. For the purposes of this problem, I estimate the scale of our inserts and reads
	 * to be relatively small, but of equal importance. If we found that we had a trememdous rate of input or perhaps a ton of historical data
	 * we needed to retrieve, we would want to consider a few (not necessarily mutually exclusive) options:
	 *   - Storing the data in a different (compressed) format (such as "n" results for "action x" that we could multiply to get back the original value)
	 *   - Storing "old" (past some threshold) in a compressed fashion
	 *   - Reevaluating our index scheme - we could consider having a partitioned index. This would require a bit more complexity added to the configuration, structure,
	 *       and data stored in this table, and perhaps may even impose additional constraints (such as being unable to dynamically add new actions on the fly), but could
	 *       potentially allow for retrieving subsets of our data (ie by action) very quickly
	 *   - Placing old data in an archival structure optimized for faster retrieval (and higher compression) rather than keeping everything in a single monolithic table.
	 *        Partitioned views would need to be appropriately leveraged in this case. One possible example of an achive index that could benefit us would be COLUMNSTORE,
	 *        as we would have a higher throughput on reads due to the nature of the compression and (likely) small range of values we'd be querying
	 *   - Considering completely alternative table structures entirely. Depending on the use cases of our persistence tier, we could consider a radical, denormalized approach
	 *        wherein we idempotently create a table for dbo.[action x] (which does not contain the column actionId, saving us 32 bits/row) and only insert the time
	 *        (if for some reason in this case we still felt that actionTimeId was important, we could emulate that with a SEQUENCE). This would make batch retrieval of
	 *        all action stats harder, and likely only to be handled through our APIs, but could open up performance benefits for calculating statistics down the road
	 *        (action stats, not statistics histograms)
	 */
	, INDEX IX_actionTimes CLUSTERED (actionId)
)