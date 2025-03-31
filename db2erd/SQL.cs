using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

internal static class SQL
{
    /// <summary>
    /// Actualise une gamme en base de données
    /// </summary>
    public static string ReadTables(string SqlServer, string SqlDatabase, string Username, string Password)
    {
        string connString = String.IsNullOrEmpty(Username) ? String.Format(@"Server={0};Database={1};Integrated Security=True;TrustServerCertificate=true;", SqlServer, SqlDatabase, Username, Password) : String.Format(@"Server={0};Database={1};User Id={2};Password={3};TrustServerCertificate=true;", SqlServer, SqlDatabase, Username, Password);

        StringBuilder content = new StringBuilder();

        using (SqlConnection conn = new SqlConnection(connString))
        {
            conn.Open();

            var tablesNames = new List<(string, string)>();//schema,name
            var primaryKeys = new List<(string, string, string)>();//schema,name,column
            var foreignKeys = new List<(string, string, string, string, string, string)>();//schema,name,column, to schema,name,column

            // obtient le nom des tables
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "select TABLE_SCHEMA, TABLE_NAME from information_schema.tables where TABLE_TYPE = 'BASE TABLE'";//TABLE_TYPE = BASE TABLE, VIEW

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tablesNames.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString()));
                }
            }

            // liste les clés primaires
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
select col.TABLE_SCHEMA, col.TABLE_NAME, col.COLUMN_NAME, cst.CONSTRAINT_NAME from information_schema.TABLE_CONSTRAINTS cst
inner join information_schema.KEY_COLUMN_USAGE col on (col.TABLE_SCHEMA = cst.TABLE_SCHEMA and col.TABLE_NAME = cst.TABLE_NAME and col.TABLE_CATALOG = cst.TABLE_CATALOG)
where CONSTRAINT_TYPE = 'PRIMARY KEY'";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    primaryKeys.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString(), reader["COLUMN_NAME"].ToString()));
                }
            }

            // liste les clés étrangeres
            /*using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
select col.TABLE_SCHEMA, col.TABLE_NAME, col.COLUMN_NAME, cst.CONSTRAINT_NAME from information_schema.TABLE_CONSTRAINTS cst
inner join information_schema.KEY_COLUMN_USAGE col on (col.TABLE_SCHEMA = cst.TABLE_SCHEMA and col.TABLE_NAME = cst.TABLE_NAME and col.TABLE_CATALOG = cst.TABLE_CATALOG)
where CONSTRAINT_TYPE = 'FOREIGN KEY'";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    foreignKeys.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString(), reader["COLUMN_NAME"].ToString()));
                }
            }*/

            // obtient la définition des tables
            foreach (var tableName in tablesNames)
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select * from information_schema.COLUMNS where TABLE_SCHEMA+'.'+TABLE_NAME = @TableName order by ORDINAL_POSITION";

                    cmd.Parameters.AddWithValue("@TableName", tableName.Item1 + '.' + tableName.Item2);
                    cmd.Parameters["@TableName"].Direction = ParameterDirection.Input;

                    content.AppendLine(String.Format("[{0}]", tableName.Item2));

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if(primaryKeys.Contains((tableName.Item1, tableName.Item2, reader["COLUMN_NAME"].ToString())))
                            content.Append("*");

                        content.AppendLine(reader["COLUMN_NAME"].ToString());
                    }

                    content.AppendLine();
                }
            }

            conn.Close();
        }

        var result = content.ToString();

        return result;
    }
}
