using Microsoft.Data.SqlClient;
using RVfamcamp.Models;

namespace RVfamcamp.Services
{
    public class DatabaseStatements
    {
        private readonly string _connectionString;

        public DatabaseStatements(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MyDbConnection");
        }


        public List<UserAccount> GetAllUsers()
        {
            var users = new List<UserAccount>();

            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT UserAccountID, username, emailAddress FROM UserAccount", conn);

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new UserAccount
                {
                    userAccountId = reader.GetInt32(0),
                    username = reader.GetString(1),
                    email = reader.IsDBNull(2) ? "No Email" : reader.GetString(2)
                });
            }

            return users;
        }


    }
}
