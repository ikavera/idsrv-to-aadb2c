using Microsoft.Data.Sqlite;
using System.Data;

namespace WebApi.Mappers.Common
{
    public class ConnectionFactory
    {
        public static IDbConnection CreateConnection(string data)
        {
            return (IDbConnection)new SqliteConnection(data);
        }
    }
}
