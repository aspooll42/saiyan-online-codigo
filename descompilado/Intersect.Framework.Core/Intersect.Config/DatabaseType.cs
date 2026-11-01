using Intersect.Framework.Annotations;

namespace Intersect.Config;

public enum DatabaseType
{
	[Ignore]
	Unknown = 0,
	SQLite = 1,
	Sqlite = 1,
	sqlite = 1,
	MySQL = 2,
	MySql = 2,
	Mysql = 2,
	mysql = 2,
	MariaDB = 2,
	mariadb = 2
}
