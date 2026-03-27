using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Configuration;
using RVfamcamp.Models;
using RVPark.Models;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;

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

            var cmd = new SqlCommand("SELECT userAccountID, firstName, lastName, emailAddress, role FROM UserAccount", conn);

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new UserAccount
                {
                    UserAccountId = reader.GetInt32(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    Email = reader.GetString(3),
                    Role = reader.GetString(4)
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
            
            if (userID == null || userID == DBNull.Value)
            {
                return -1;
            }
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
        /// Gets basic user profile information by userAccountID
        /// </summary>
        public UserAccount? GetUserById(int userAccountId)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                @"SELECT userAccountID, emailAddress, firstName, lastName, role 
          FROM UserAccount 
          WHERE userAccountID = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userAccountId);
            conn.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new UserAccount
                {
                    UserAccountId = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    Role = reader.GetString(4)
                };
            }

            return null;
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

        public void UpdateUserRole(int userAccountId, string newRole)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE UserAccount SET role = @Role WHERE userAccountID = @UserAccountID", conn);
            cmd.Parameters.AddWithValue("@Role", newRole);
            cmd.Parameters.AddWithValue("@UserAccountID", userAccountId);

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
            var cmd = new SqlCommand("DELETE FROM UserAccount WHERE userAccountID = @UserAccountID", conn);
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
        public int AddReservation(Reservation reservation, int userAccountID)
        {
            
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                """
                    INSERT INTO Reservation (StartDate, EndDate, ConfirmationNumber, userAccountID)
                    VALUES (@startDate, @endDate, @confirmationNumber, @userAccountID);
                    SELECT SCOPE_IDENTITY();
                """, conn);
            cmd.Parameters.AddWithValue("@startDate", reservation.startDate);
            cmd.Parameters.AddWithValue("@endDate", reservation.endDate);
            cmd.Parameters.AddWithValue("@confirmationNumber", reservation.confirmationNumber);
            cmd.Parameters.AddWithValue("@userAccountID", userAccountID);
            conn.Open();
            var result = cmd.ExecuteScalar();

            return Convert.ToInt32(result);


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
                reader.Read();
                return new Reservation
                {
                    reservationId = reader.GetInt32(0),
                    startDate = reader.GetDateTime(1),
                    endDate = reader.GetDateTime(2),
                    confirmationNumber = reader.GetInt32(3)
                };
            }
        }

        // READ: Reservation
        public List<Reservation> GetUsersReservations(int userAccountID)
        {
            var reservations = new List<Reservation>();

            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT reservationID, startDate, endDate, " +
                "confirmationNumber FROM Reservation WHERE userAccountID = @UserAccountID", conn);
            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);

            conn.Open();
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                reservations.Add(new Reservation
                {
                    reservationId = reader.GetInt32(0),
                    startDate = reader.GetDateTime(1),
                    endDate = reader.GetDateTime(2),
                    confirmationNumber = reader.GetInt32(3)
                });
            }

            return reservations;
        }

        // DELETE: Reservation 
        public void RemoveReservationById(int reservationId)
        {
            using var conn = new SqlConnection(_connectionString);
            
            // Delete entry in vehicle reservation if it exists. 
            var cmdDelVehicleRegistration =
                new SqlCommand("DELETE FROM VehicleReservation WHERE reservationID = @reservationId", conn);
            cmdDelVehicleRegistration.Parameters.AddWithValue("@reservationId", reservationId);
            conn.Open();
            cmdDelVehicleRegistration.ExecuteNonQuery();
            conn.Close();
            
            // Delete reservation. 
            var cmd = new SqlCommand("DELETE FROM Reservation WHERE ReservationID = @reservationID", conn);
            cmd.Parameters.AddWithValue("@reservationID", reservationId);
            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        // Gets the lots tied to a reservation
        public List<Lot> GetLotsByReservationId(int reservationId)
        {
            List<Lot> lots = new List<Lot>();
            using var conn = new SqlConnection(_connectionString);

            // We join LotReservation (the link) to Lot (the data)
            var cmd = new SqlCommand(@"SELECT l.lotID, l.lotType, l.isOccupied 
                   FROM Lot l
                   JOIN LotReservation lr ON l.lotID = lr.lotID
                   WHERE lr.reservationID = @ResrvationID", conn);

            cmd.Parameters.AddWithValue("@ResrvationID", reservationId);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lots.Add(new Lot
                {
                    LotId = reader.GetInt32(0),
                    LotType = reader.GetInt32(1),
                    IsOccupied = reader.GetBoolean(2)
                });
            }
            return lots;
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
                """, conn
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
                """, conn
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
                """, conn
            );
            cmd.Parameters.AddWithValue("@startDate", start);
            cmd.Parameters.AddWithValue("@endDate", end);
            
            conn.Open();
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lots.Add(new Lot
                {
                    LotId = reader.GetInt32(0),
                    IsOccupied = reader.GetBoolean(1),
                    LotType = reader.GetInt32(2)
                });
            }

            return lots;
        }


        //This method categorizes reservation information into three categories. Upcoming, In Progress, and Completed.
        //This is used when making the reports in the reports page.
        public StatusReport GetStatusReport(DateTime startRange, DateTime endRange)
        {
            var report = new StatusReport();
            DateTime today = DateTime.Today;

            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"SELECT r.reservationID, r.startDate, r.endDate, r.confirmationNumber,
                       u.firstName, u.lastName, u.emailAddress, l.lotNumber
                FROM Reservation r
                JOIN UserAccount u ON r.userAccountID = u.userAccountID
                JOIN LotReservation lr ON r.reservationID = lr.reservationID
                JOIN Lot l ON lr.lotID = l.lotID
                WHERE r.startDate >= @Start AND r.endDate <= @End", conn);

            cmd.Parameters.AddWithValue("@Start", startRange);
            cmd.Parameters.AddWithValue("@End", endRange);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var res = new ReservationDetail
                {
                    Id = reader.GetInt32(0),
                    Start = reader.GetDateTime(1),
                    End = reader.GetDateTime(2),
                    Conf = reader.GetInt32(3),
                    CustomerName = $"{reader.GetString(4)} {reader.GetString(5)}",
                    Email = reader.GetString(6),
                    LotNum = reader.GetInt32(7)
                };

                if (res.End < today)
                    report.Completed.Add(res);
                else if (res.Start <= today && res.End >= today)
                    report.InProgress.Add(res);
                else
                    report.Upcoming.Add(res);
            }
            return report;
        }




        // *****************
        // Client Table
        // *****************
        public void AddClientInfo(int userAccountID, string militaryAffiliation, string street, string city, string state, string zip)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("INSERT INTO Client (userAccountID, militaryAffiliation, billingStreet, billingCity, billingState, billingZip) VALUES (@UserAccountID, @MilitaryAffiliation, @Street, @City, @State, @Zip)", conn);

            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);
            cmd.Parameters.AddWithValue("@MilitaryAffiliation", militaryAffiliation);
            cmd.Parameters.AddWithValue("@Street", street);
            cmd.Parameters.AddWithValue("@City", city);
            cmd.Parameters.AddWithValue("@State", state);
            cmd.Parameters.AddWithValue("@Zip", zip);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public ClientInfo? GetClientInfo(int userAccountId)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                @"SELECT militaryAffiliation, billingStreet, billingCity, billingState, billingZip 
          FROM Client 
          WHERE userAccountID = @UserId", conn);

            cmd.Parameters.AddWithValue("@UserId", userAccountId);
            conn.Open();

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new ClientInfo
                {
                    MilitaryAffiliation = reader.GetString(0),
                    BillingStreet = reader.GetString(1),
                    BillingCity = reader.GetString(2),
                    BillingState = reader.GetString(3),
                    BillingZip = reader.GetString(4)
                };
            }
            return null;
        }

        public void EditClientInfo(int userAccountID, string militaryAffiliation, string street, string city, string state, string zip)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE Client SET militaryAffiliation = @MilitaryAffiliation, billingStreet = @Street, billingCity = @City, billingState = @State, billingZip = @Zip WHERE userAccountID = @UserAccountID", conn);

            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);
            cmd.Parameters.AddWithValue("@MilitaryAffiliation", militaryAffiliation);
            cmd.Parameters.AddWithValue("@Street", street);
            cmd.Parameters.AddWithValue("@City", city);
            cmd.Parameters.AddWithValue("@State", state);
            cmd.Parameters.AddWithValue("@Zip", zip);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteClientInfo(int userAccountID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM Client WHERE userAccountID = @UserAccountID", conn);
            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        // *****************
        // Vehicle Table
        // *****************

        /// Gets all vehicles for a specific user
        public List<VehicleViewModel> GetVehiclesByUser(int userAccountId)
        {
            var vehicles = new List<VehicleViewModel>();

            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(
                @"SELECT vehicleID, licenseNumber, year, make, model 
          FROM Vehicle 
          WHERE userAccountID = @UserAccountID", conn);

            cmd.Parameters.AddWithValue("@UserAccountID", userAccountId);
            conn.Open();

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                vehicles.Add(new VehicleViewModel
                {
                    Id = reader.GetInt32(0),
                    LicensePlate = reader.GetString(1),
                    Year = reader.GetInt32(2),
                    Make = reader.GetString(3),
                    Model = reader.GetString(4),
                    Color = ""   // Default to empty
                });
            }

            return vehicles;
        }

        public void AddVehicle(string licenseNumber, int year, string make, string model, int userAccountID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand(@"INSERT INTO Vehicle (licenseNumber, year, make, model, userAccountID) VALUES (@LicenseNumber, @Year, @Make, @Model, @UserAccountID)", conn);

            cmd.Parameters.AddWithValue("@licenseNumber", licenseNumber);
            cmd.Parameters.AddWithValue("@Year", year);
            cmd.Parameters.AddWithValue("@Make", make);
            cmd.Parameters.AddWithValue("@Model", model);
            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void EditVehicle(string licenseNumber, int year, string make, string model, string userAccountID, int vehicleID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE Vehicle SET licenseNumber = @LicenseNumber, year = @Year, make = @Make, model = @Model, userAccountID = @UserAccountID WHERE vehicleID = @VehicleID", conn);

            cmd.Parameters.AddWithValue("@licenseNumber", licenseNumber);
            cmd.Parameters.AddWithValue("@Year", year);
            cmd.Parameters.AddWithValue("@Make", make);
            cmd.Parameters.AddWithValue("@Model", model);
            cmd.Parameters.AddWithValue("@UserAccountID", userAccountID);
            cmd.Parameters.AddWithValue("@VehicleID", vehicleID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeleteVehicle(int vehicleID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM Vehicle WHERE vehicleID = @VehicleID", conn);
            cmd.Parameters.AddWithValue("@VehicleID", vehicleID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // Necessary to check for a client record (and create if missing) before adding a vehicle, otherwise things break.
        public void EnsureClientRecordExists(int userAccountId)
        {
            // Check if Client record already exists
            using var conn = new SqlConnection(_connectionString);
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Client WHERE userAccountID = @UserId", conn);
            checkCmd.Parameters.AddWithValue("@UserId", userAccountId);
            conn.Open();

            var count = (int)checkCmd.ExecuteScalar();

            if (count > 0)
                return; // Already exists

            // Create minimal Client record
            var insertCmd = new SqlCommand(
                @"INSERT INTO Client (userAccountID, militaryAffiliation, billingStreet, billingCity, billingState, billingZip)
          VALUES (@UserId, '', '', '', '', '')", conn);

            insertCmd.Parameters.AddWithValue("@UserId", userAccountId);
            insertCmd.ExecuteNonQuery();
        }


        // *****************
        // VehicleReservation Table
        // *****************
        public void LinkVehicleToReservation(int vehicleID, int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("INSERT INTO VehicleReservation (vehicleID, reservationID) VALUES (@VehicleID, @ReservationID)", conn);

            cmd.Parameters.AddWithValue("@VehicleID", vehicleID);
            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void RemoveAllVehiclesFromReservation(int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM VehicleReservation WHERE reservationID = @ReservationID", conn);

            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        // *****************
        // LotReservation
        // *****************
        public void AssignLotToReservation(int lotID, int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("INSERT INTO LotReservation (lotID, reservationID) VALUES (@LotID, @ReservationID)", conn);

            cmd.Parameters.AddWithValue("@LotID", lotID);
            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void ClearLotsFromReservation(int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM LotReservation WHERE reservationID = @ReservationID", conn);

            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        // *****************
        // Lot
        // *****************

        public List<Lot> getLots()
        {
            List<Lot> lots = new List<Lot>(); 

            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT LotId, IsOccupied, LotType FROM Lot", conn);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lots.Add(new Lot
                {
                    LotId = reader.GetInt32(0),
                    IsOccupied = reader.GetBoolean(1),
                    LotType = reader.GetInt32(2)
                });
            }

            return lots;


        }
        public int getLotID(int lotNumber)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT lotID FROM Lot WHERE lotNumber = @LotNumber", conn);

            cmd.Parameters.AddWithValue("@LotNumber", lotNumber);

            conn.Open();
            var lotID = cmd.ExecuteScalar();

            return Convert.ToInt32(lotID);
        }
        public void UpdateLotOccupancy(int lotID, bool isOccupied)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE Lot SET isOccupied = @Status WHERE lotID = @LotID", conn);

            cmd.Parameters.AddWithValue("@Status", isOccupied);
            cmd.Parameters.AddWithValue("@LotID", lotID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public List<Lot> GetVacantLots(DateOnly startDate, DateOnly endDate)
        {
            List<Lot> lots = new List<Lot>();

            using var conn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand(
                """
                SELECT l.LotId, l.IsOccupied, l.LotType
                FROM Lot l
                WHERE l.IsOccupied = 0
                AND l.LotId NOT IN (
                    SELECT lr.LotId
                    FROM LotReservation lr
                    JOIN Reservation r ON lr.ReservationId = r.ReservationID
                    WHERE NOT (
                        r.EndDate <= @StartDate OR r.StartDate >= @EndDate
                    )
                )
                """, conn);

            cmd.Parameters.Add("@StartDate", SqlDbType.Date)
                .Value = startDate.ToDateTime(TimeOnly.MinValue);

            cmd.Parameters.Add("@EndDate", SqlDbType.Date)
                .Value = endDate.ToDateTime(TimeOnly.MinValue);

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lots.Add(new Lot
                {
                    LotId = reader.GetInt32(0),
                    IsOccupied = reader.GetBoolean(1),
                    LotType = reader.GetInt32(2)
                });
            }

            return lots;

        }


        // *****************
        // Report
        // *****************
        public void LogReportGeneration(int userAccountID, string reportType)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("INSERT INTO Report (userAccountID, generatedDate, reportType) VALUES (@UserID, GETDATE(), @Type)", conn);

            cmd.Parameters.AddWithValue("@UserID", userAccountID);
            cmd.Parameters.AddWithValue("@Type", reportType);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // *****************
        // Payments
        // *****************
        public int getPaymentIdByReservation(int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);

            var cmd = new SqlCommand("SELECT paymentsID FROM Payments WHERE reservationID = @ReservationID", conn);

            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            var lotID = cmd.ExecuteScalar();

            return Convert.ToInt32(lotID);
        }

        public List<paymentModel> getPaymentsByUserID(int userID)
        {
			using var conn = new SqlConnection(_connectionString);

			var cmd = new SqlCommand("select p.paymentDate, p.reservationID, p.paymentsID, p.stripeCode, p.summary, p.taxAmount, p.total from Payments p\r\ninner join Reservation r on p.reservationID = r.reservationID\r\ninner join Client c on c.userAccountID = r.userAccountID\r\ninner join UserAccount ua on c.userAccountID = ua.userAccountID\r\nwhere ua.userAccountID = @userID", conn);

			cmd.Parameters.AddWithValue("@userID", userID);
            conn.Open();

			using var reader = cmd.ExecuteReader();
            List<paymentModel> userPayments = new();

			while (reader.Read())
			{
                userPayments.Add(new paymentModel()
                {
					paymentDate = reader.GetDateTime(0),
					reservationID = reader.GetInt32(1),
					id = reader.GetInt32(2),
					stripeID = reader.GetString(3),
					summary = reader.GetString(4),
					tax = reader.GetDecimal(5),
					total = reader.GetDecimal(6)
				});
			}
			return userPayments;

		}

        public void AddPayment(decimal total, decimal tax, string summary, string stripeCode, int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("INSERT INTO Payments (total, taxAmount, paymentDate, summary, stripeCode, reservationID) VALUES (@Total, @Tax, GETDATE(), @Summary, @Stripe, @ReservationID)", conn);

            cmd.Parameters.AddWithValue("@Total", total);
            cmd.Parameters.AddWithValue("@Tax", tax);
            cmd.Parameters.AddWithValue("@Summary", summary);
            cmd.Parameters.AddWithValue("@Stripe", stripeCode);
            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public bool IsReservationPaid(int reservationID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Payments WHERE reservationID = @ReservationID", conn);
            cmd.Parameters.AddWithValue("@ReservationID", reservationID);

            conn.Open();
            int count = (int)cmd.ExecuteScalar();
            return count > 0;
        }

        public void UpdatePaymentSummary(int paymentsID, string newSummary)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE Payments SET summary = @Summary WHERE paymentsID = @PaymentID", conn);

            cmd.Parameters.AddWithValue("@Summary", newSummary);
            cmd.Parameters.AddWithValue("@PaymentID", paymentsID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void DeletePayment(int paymentsID)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("DELETE FROM Payments WHERE paymentsID = @PaymentID", conn);

            cmd.Parameters.AddWithValue("@PaymentID", paymentsID);

            conn.Open();
            cmd.ExecuteNonQuery();
        }



        // *****************
        // LotType
        // *****************
        public LotType? GetLotTypeByLotNumber(int lotNumber)
        {
            using var conn = new SqlConnection(_connectionString);
            // Joining Lot and LotType to get the full pricing details for a specific site
            var cmd = new SqlCommand(@"SELECT lt.lotType, lt.typeName, lt.basePrice, lt.lotSize 
                FROM LotType lt
                JOIN Lot l ON lt.lotType = l.lotType
                WHERE l.lotNumber = @LotNum", conn);

            cmd.Parameters.AddWithValue("@LotNum", lotNumber);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new LotType
                {
                    LotTypeID = reader.GetInt32(0),
                    TypeName = reader.GetString(1),
                    BasePrice = reader.GetDecimal(2),
                    LotSize = reader.GetInt32(3)
                };
            }
            return null;
        }


        public List<LotType> GetAllLotTypes()
        {
            var lotTypes = new List<LotType>();
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("SELECT lotType, typeName, basePrice, lotSize FROM LotType", conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lotTypes.Add(new LotType
                {
                    LotTypeID = reader.GetInt32(0),
                    TypeName = reader.GetString(1),
                    BasePrice = reader.GetDecimal(2),
                    LotSize = reader.GetInt32(3)
                });
            }
            return lotTypes;
        }

        public bool UpdateLotTypeBasePrice(int lotTypeId, decimal newPrice)
        {
            using var conn = new SqlConnection(_connectionString);
            var cmd = new SqlCommand("UPDATE LotType SET basePrice = @Price WHERE lotType = @ID", conn);


            cmd.Parameters.AddWithValue("@Price", newPrice);
            cmd.Parameters.AddWithValue("@ID", lotTypeId);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }

}