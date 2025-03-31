//using CredentialManagement;
internal class Program
{
    /// <summary>
    /// Obtient les identifiants Windows pour la connexion au serveur sFTP
    /// </summary>
    /// <param name="CredentialName"></param>
    /// <returns></returns>
   /* private static Credential GetCredential(string CredentialName)
    {
        var cm = new Credential { Target = CredentialName, Type = CredentialType.Generic };
        if (cm.Exists() == false)
            throw new Exception(String.Format("Credential not exist for {0}. Please verify your generic credentials", CredentialName));

        cm.Load();
        return cm;
    }
   */
    private static void Main(string[] args)
    {
        try
        {
#if TEST
            // SqlServer (remote)
            //string connProvider = "sqlserver";
            //string connString = String.Format(@"Server={0};Database={1};User Id={2};Password={3};TrustServerCertificate=true;", "192.168.22.10,1433", "inventaire", "sa", "ZeDw7UUmSrXUZDcwNYNX");
            // SqlServer (local)
            //string connProvider = "sqlserver";
            //string connString = String.Format(@"Server={0};Database={1};Integrated Security=true;TrustServerCertificate=true;", "PC-TAU\\SQLEXPRESS", "prod");
            // MySQL (local)
            //string connProvider = "mysql";
            //string connString = String.Format(@"Server={0};Database={1};Uid={2};Pwd={3};", "localhost", "test", "root", "admin");
            // MySQL (local)
            string connProvider = "postgresql";
            string connString = String.Format(@"Server={0};Port=5433;Database={1};User Id={2};Password={3}", "localhost", "test", "postgres", "admin");
#else
            string connProvider = args[0];
            string connString = args[1];
#endif
            string output = String.Empty;
            switch (connProvider)
            {
                case "sqlserver":
                    output = SqlServer.ReadTables(connString);
                    break;
                case "mysql":
                    output = MySQL.ReadTables(connString);
                    break;
                case "postgresql":
                    output = Postgresql.ReadTables(connString);
                    break;
            }

            Console.Write(output);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}