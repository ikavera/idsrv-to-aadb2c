using Microsoft.Data.Sqlite;
using System.Data;

namespace Auth.Mappers.Common
{
    public class ConnectionFactory
    {
        public static IDbConnection CreateConnection(string data)
        {
            return (IDbConnection)new SqliteConnection(data); 
        }
    }
}
