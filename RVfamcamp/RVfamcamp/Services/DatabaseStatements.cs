using Microsoft.Data.SqlClient;
using RVfamcamp.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel; //for password hash

namespace RVfamcamp.Services
{
    public class DatabaseStatements
    {
        private readonly string _connectionString;

        public DatabaseStatements(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MyDbConnection");
        }

        /// <summary>
        /// Gets a list of all users
        /// </summary>
        /// <returns></returns>
        public List<UserAccount> GetAllUsers()
        {
            var users = new List<UserAccount>();

            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT userAccountID, emailAddress FROM UserAccount", conn);

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new UserAccount
                {
                    UserAccountId = reader.GetInt32(0),
                    Email = reader.GetString(1),
                });
            }

            return users;
        }

        /// <summary>
        /// Gets userAccountID from emailAddress
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns></returns>
        public int GetUserAccountID(string email)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT userAccountID FROM UserAccount WHERE emailAddress = @Email", conn);

            cmd.Parameters.AddWithValue("@Email", email);

            conn.Open();
            var userID = cmd.ExecuteScalar();

            return Convert.ToInt32(userID);
        }

        /// <summary>
        /// Registers a user into the database
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="role"></param>
        /// 

        // ****** Removing @User *******
        public void RegisterUser(string email, string password, string firstName, string lastName, string role)
        {
            var hasher = new PasswordHasher<UserAccount>();

            var dummyUser = new UserAccount { Email = email };

            string secureHash = hasher.HashPassword(dummyUser, password);

            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("INSERT INTO UserAccount (emailAddress, password, firstName, lastName, role) VALUES (@Email, @Hash, @FirstName, @LastName, @Role)", conn);

            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Hash", secureHash);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@Role", role);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <param name="username"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        public void UpdateUser(int userId, string email, string firstName, string lastName)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE UserAccount SET emailAddress = @Email, firstName = @FirstName, lastName = @LastName WHERE userAccountID = @UserAccountID", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@FirstName", firstName);
            cmd.Parameters.AddWithValue("@LastName", lastName);
            cmd.Parameters.AddWithValue("@UserAccountID", userId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a user from the database
        /// </summary>
        /// <param name="userId"></param>
        /// 
        // ****** Removing @User *******
        public void DeleteUser(int userId)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM UserAccount WHERE emailAddress = @UserAccountID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        // *************************************************
        // *************** Login Adding here ***************
        // *************************************************

        /// <summary>
        /// Attempts to log in a user by verifying email and password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>UserAccount if valid, otherwise null</returns>
        public UserAccount? LoginUserAccount(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand(
                @"SELECT userAccountID, emailAddress, password, firstName, lastName, role
                  FROM UserAccount
                  WHERE emailAddress = @Email",
                conn);

            cmd.Parameters.AddWithValue("@Email", email);

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                // 👇 Store hashed password locally (NOT in model)
                string storedHash = reader.GetString(2);

                var user = new UserAccount
                {
                    UserAccountId = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    FirstName = reader.GetString(3),
                    LastName = reader.GetString(4),
                    Role = reader.GetString(5)
                };

                // 🔐 Verify hashed password
                var hasher = new PasswordHasher<UserAccount>();
                var result = hasher.VerifyHashedPassword(user, storedHash, password);

                if (result == PasswordVerificationResult.Success)
                {
                    return user;
                }
            }

            return null;
        }


    }
}