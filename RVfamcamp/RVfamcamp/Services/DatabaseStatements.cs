using Microsoft.Data.SqlClient;
using RVfamcamp.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.Diagnostics;

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
        
        // CREATE: Reservations
        public void AddReservation(Reservation reservation)
        {
            
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                """
                    INSERT INTO Reservation (StartDate, EndDate, ConfirmationNumber)
                    VALUES (@startDate, @endDate, @confirmationNumber)
                """);
            cmd.Parameters.AddWithValue("@startDate", reservation.startDate);
            cmd.Parameters.AddWithValue("@endDate", reservation.endDate);
            cmd.Parameters.AddWithValue("@confirmationNumber", reservation.confirmationNumber);
            conn.Open();
            cmd.ExecuteNonQuery();
        }
        
        // READ: Reservations
        public List<Reservation> GetAllReservations()
        {
            var reservations = new List<Reservation>();
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT ReservationID, StartDate, EndDate, ConfirmationNumber FROM Reservation", conn); 
            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservations.Add(
                    
                    new Reservation
                    {
                        reservationId = reader.GetInt32(0),
                    }
                    
                );
            }
            return reservations;
        }
        
        // READ: Reservations
        public Reservation GetReservationById(int reservationId)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT ReservationID, StartDate, EndDate, ConfirmationNumber FROM Reservation WHERE ReservationID = @reservationId", conn);
            cmd.Parameters.AddWithValue("@reservationId", reservationId);
            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return null; 
            }
            else
            {
                return new Reservation
                {
                    reservationId = reader.GetInt32(0),
                    startDate = reader.GetDateTime(1),
                    endDate = reader.GetDateTime(2),
                    confirmationNumber = reader.GetInt32(3)
                };
            }
        }
        
        // DELETE: Reservation 
        public void RemoveReservationById(Reservation reservation)
        {
            using var conn = new SqlConnection(_connectionString);
            
            // Delete entry in vehicle reservation if it exists. 
            var cmdDelVehicleRegistration =
                new SqlCommand("DELETE FROM VehicleReservation vr WHERE vr.reservationID == @reservationId");
            cmdDelVehicleRegistration.Parameters.AddWithValue("@reservationId", reservation.reservationId);
            conn.Open();
            cmdDelVehicleRegistration.ExecuteNonQuery();
            conn.Close();
            
            // Delete reservation. 
            var cmd = new SqlCommand("DELETE FROM Reservation WHERE ReservationID = @reservationID", conn);
            cmd.Parameters.AddWithValue("@reservationID", reservation.reservationId);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        // Retrieves all Reservation objects whose 'startDate' column corresponds to the given date. 
        // Note that time is not considered in this query. So, if two DateTime columns are identical in Date but not in Time, that does not matter to this function. 
        private IList<Reservation> GetArrivalsForDate(DateOnly date)
        {
            IList<Reservation> reservations = new List<Reservation>();
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand(
                """
                SELECT ReservationID, StartDate, EndDate, ConfirmationNumber FROM Reservation
                WHERE CAST(startDate AS DATE) = @ArrivalDate
                """
            );
            cmd.Parameters.AddWithValue("@ArrivalDate", date);            
            
            conn.Open();
            
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservations.Add(
                    new Reservation
                    {
                        reservationId = reader.GetInt32(0),
                        startDate = reader.GetDateTime(1),
                        endDate = reader.GetDateTime(2),
                        confirmationNumber = reader.GetInt32(3)
                    }
                );
            }

            return reservations;
        }

        // Returns all Reservations whose 'endDate' column corresponds to the given date column. 
        // Note that time is not considered in this query. So, if two DateTime columns are identical in Date but not in Time, that does not matter to this function.
        private IList<Reservation> GetDeparturesForDate(DateOnly date)
        {
            IList<Reservation> reservations = new List<Reservation>();
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand(
                """
                SELECT ReservationID, StartDate, EndDate, ConfirmationNumber FROM Reservation
                WHERE CAST(endDate AS DATE) = @DepartureDate
                """
            );
            cmd.Parameters.AddWithValue("@DepartureDate", date);            
            
            conn.Open();
            
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                reservations.Add(
                    new Reservation
                    {
                        reservationId = reader.GetInt32(0),
                        startDate = reader.GetDateTime(1),
                        endDate = reader.GetDateTime(2),
                        confirmationNumber = reader.GetInt32(3)
                    }
                );
            }

            return reservations;
        }

        // A QoL function that allows you to retrieve arrivals and departures through a single function call. 
        // This function fulfills the CRUD requirements for Report 1: Selection of arrivals and departures for a given date. 
        public IList<IList<Reservation>> GetArrivalsAndDeparturesForDate(DateOnly date)
        {
            return new List<IList<Reservation>>
            {
                GetArrivalsForDate(date),
                GetDeparturesForDate(date)
            };
        }

        // This function constitutes the CRUD portion of Report 2: Selection of lots left vacant over a given time period. 
        public IList<Lot> GetVacantLotsOverRange(DateOnly start, DateOnly end)
        {
            IList<Lot> lots =  new List<Lot>();
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand(
                """
                SELECT DISTINCT l.lotID, l.isOccupied, l.lotType FROM Reservation r
                INNER JOIN LotReservation lr
                ON lr.reservationID = r.ReservationID
                INNER JOIN Lot l
                ON l.lotID = lr.LotID
                WHERE CAST(r.startDate AS DATE) > @endDate OR CAST(r.endDate AS DATE) < @startDate
                """
            );
            cmd.Parameters.AddWithValue("@startDate", start);
            cmd.Parameters.AddWithValue("@endDate", end);
            
            conn.Open();
            
            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lots.Add(new Lot
                {
                    LotId = reader.GetInt32(0),
                    IsOccupied = reader.GetBoolean(1),
                    LotType = reader.GetInt32(2),
                });
            }

            return lots;
        }

    }
    
}