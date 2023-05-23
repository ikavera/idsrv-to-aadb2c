using Dapper;
using System.Data;
using System.Diagnostics;

namespace WebApi.Mappers.Common
{
    public abstract class AbstractMapper
    {

        private string _connectionData = null;

        protected AbstractMapper(string connectionData)
        {
            _connectionData = connectionData;
        }

        protected IDbConnection getConnection()
        {
            return ConnectionFactory.CreateConnection(_connectionData);
        }

        #region Find One

        protected T FindOne<T>(string sql)
        {
            var result = default(T);

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, commandType: CommandType.Text).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }

            return result;
        }

        protected T FindOne<T>(string sql, DynamicParameters parameters)
        {
            var result = default(T);

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, parameters, commandType: CommandType.Text).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected T FindLast<T>(string sql, DynamicParameters parameters)
        {
            var result = default(T);

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, parameters, commandType: CommandType.Text).LastOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected T FindOne<T>(string sql, DynamicParameters parameters, CommandType commandType)
        {
            var result = default(T);
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, parameters, commandType: commandType).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected T FindOne<T>(string sql, DynamicParameters parameters, Func<dynamic, T> loadFunc)
        {
            //var result = default(T);
            dynamic result = default(T);
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<dynamic>(sql, parameters, commandType: CommandType.Text).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return loadFunc(result);
        }

        #endregion

        #region Find Many

        protected List<T> FindMany<T>(string sql)
        {
            var result = new List<T>();

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, commandType: CommandType.Text).ToList();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }

            return result;
        }

        protected List<T> FindMany<T>(string sql, DynamicParameters parameters)
        {
            var result = new List<T>();

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, parameters, commandType: CommandType.Text).ToList();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }

            return result;
        }

        protected List<T> FindMany<T>(string sql, DynamicParameters parameters, Func<dynamic, T> loadFunc)
        {
            dynamic result = new List<T>();
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    var rows = new List<dynamic>();
                    if (loadFunc != null)
                    {
                        rows = connection.Query<dynamic>(sql, parameters, commandType: CommandType.Text).ToList();
                        foreach (var row in rows)
                        {
                            result.Add(loadFunc(row));
                        }
                    }
                    else
                    {
                        result = connection.Query<T>(sql, parameters, commandType: CommandType.Text).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected List<T> FindMany<T>(string sql, DynamicParameters parameters, Func<dynamic, T> loadFunc,
            CommandType commandType)
        {
            dynamic result = new List<T>();
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    var rows = new List<dynamic>();
                    if (loadFunc != null)
                    {
                        rows = connection.Query<dynamic>(sql, parameters, commandType: commandType).ToList();
                        foreach (var row in rows)
                        {
                            result.Add(loadFunc(row));
                        }
                    }
                    else
                    {
                        result = connection.Query<T>(sql, parameters, commandType: commandType).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }


        protected List<T> FindMany<T>(string sql, DynamicParameters parameters, CommandType commandType)
        {
            var result = new List<T>();

            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T>(sql, parameters, commandType: commandType).ToList();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }

            return result;
        }

        protected List<int> FindMany(string sql, DynamicParameters parameters, CommandType commandType)
        {
            var result = new List<int>();
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<int>(sql, parameters, commandType: commandType).ToList();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected List<T> FindMany<T>(string sql, DynamicParameters parameters, Func<dynamic, T> loadFunc,
            CommandType commandType, int commandTimeout)
        {
            dynamic result = new List<T>();
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    var rows = new List<dynamic>();
                    rows = connection.Query<dynamic>(sql, parameters, commandType: commandType,
                        commandTimeout: commandTimeout).ToList();

                    foreach (var row in rows)
                    {
                        result.Add(loadFunc(row));
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        #endregion

        #region First or Default

        protected T1 FirstOrDefault<T1>(String sql, DynamicParameters parameters)
        {
            var result = default(T1);
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T1>(sql, parameters).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        protected T1 FirstOrDefault<T1>(String sql)
        {
            var result = default(T1);
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.Query<T1>(sql).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }

            return result;
        }

        #endregion

        #region Execute 

        protected void Execute(String sql)
        {
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    connection.Execute(sql);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, null);
                throw ex;
            }
        }

        protected void Execute(String sql, DynamicParameters parameters)
        {
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    connection.Execute(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }
        }

        protected void Execute(String sql, DynamicParameters parameters, System.Data.CommandType type)
        {
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    connection.Execute(sql, parameters, commandType: type);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }
        }

        protected void Execute(String sql, DynamicParameters parameters, System.Data.CommandType type,
            int commandTimeout)
        {
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    connection.Execute(sql, parameters, commandType: type, commandTimeout: commandTimeout);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }
        }

        protected T ExecuteScalar<T>(String sql, DynamicParameters parameters)
        {
            var result = default(T);
            try
            {
                using (IDbConnection connection = ConnectionFactory.CreateConnection(_connectionData))
                {
                    result = connection.ExecuteScalar<T>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                LogException(ex.Message, sql, parameters);
                throw ex;
            }

            return result;
        }

        #endregion

        private void LogException(String message, String sql, DynamicParameters parameters)
        {
            Debug.WriteLine("SQL Exception: " + message);
            Debug.WriteLine(sql);
            if (parameters != null)
            {
                foreach (var name in parameters.ParameterNames)
                {
                    Debug.WriteLine("Parameter: " + name + ", Value: " + parameters.Get<object>(name)?.ToString());
                }
            }
        }
    }
}
