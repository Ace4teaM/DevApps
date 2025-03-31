using MySql.Data.MySqlClient;
using System.Data;
using System.Text;

internal static class MySQL
{
    public static string ReadTables(string connString)
    {
        StringBuilder content = new StringBuilder();
        using (MySqlConnection conn = new MySqlConnection(connString))
        {
            conn.Open();

            var tablesNames = new List<(string, string)>();//schema,name
            var primaryKeys = new List<(string, string, string)>();//schema,name,column
            var foreignKeys = new List<(string, string, string, string, string, string)>();//schema,name,column, to schema,name,column
            
            // obtient le nom des tables
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = string.Format("select * from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '{0}'", conn.Database);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tablesNames.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString()));
                }
            }

            // liste les clés primaires
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
select col.TABLE_SCHEMA, col.TABLE_NAME, col.COLUMN_NAME, cst.CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS cst
inner join INFORMATION_SCHEMA.KEY_COLUMN_USAGE col on (col.TABLE_SCHEMA = cst.TABLE_SCHEMA and col.TABLE_NAME = cst.TABLE_NAME and col.CONSTRAINT_NAME = cst.CONSTRAINT_NAME)
where cst.CONSTRAINT_SCHEMA = 'test' and cst.CONSTRAINT_TYPE = 'PRIMARY KEY'";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    primaryKeys.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString(), reader["COLUMN_NAME"].ToString()));
                }
            }

            // liste les clés étrangeres
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
select col.TABLE_SCHEMA, col.TABLE_NAME, col.COLUMN_NAME, cst.CONSTRAINT_NAME, col.REFERENCED_TABLE_SCHEMA as T_TABLE_SCHEMA, ref.REFERENCED_TABLE_NAME as T_TABLE_NAME, col.REFERENCED_COLUMN_NAME as T_COLUMN_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS cst
inner join INFORMATION_SCHEMA.KEY_COLUMN_USAGE col on (col.TABLE_SCHEMA = cst.TABLE_SCHEMA and col.TABLE_NAME = cst.TABLE_NAME and col.CONSTRAINT_NAME = cst.CONSTRAINT_NAME)
inner join INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS ref on (ref.CONSTRAINT_NAME = col.CONSTRAINT_NAME)
where cst.CONSTRAINT_SCHEMA = 'test' and cst.CONSTRAINT_TYPE = 'FOREIGN KEY'";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    foreignKeys.Add((reader["TABLE_SCHEMA"].ToString(), reader["TABLE_NAME"].ToString(), reader["COLUMN_NAME"].ToString(),
                                     reader["T_TABLE_SCHEMA"].ToString(), reader["T_TABLE_NAME"].ToString(), reader["T_COLUMN_NAME"].ToString()));
                }
            }

            // obtient la définition des tables
            foreach (var tableName in tablesNames)
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA=@TableSchema and TABLE_NAME = @TableName order by ORDINAL_POSITION";

                    cmd.Parameters.AddWithValue("@TableName", tableName.Item2);
                    cmd.Parameters["@TableName"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@TableSchema", conn.Database);
                    cmd.Parameters["@TableSchema"].Direction = ParameterDirection.Input;

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

            // obtient les relations des tables
            if (foreignKeys.Count > 0)
            {
                content.AppendLine();
                foreach (var foreignKey in foreignKeys)
                {
                    content.AppendLine(String.Format("{0}-->{1}", foreignKey.Item2, foreignKey.Item5));
                }
            }

            conn.Close();
        }

        var result = content.ToString();

        return result;
    }
}
