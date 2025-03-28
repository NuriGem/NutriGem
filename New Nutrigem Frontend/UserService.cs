using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace NutrigemApi2
{
    public class UserService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserService> _logger;

        public UserService(string connectionString, ILogger<UserService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("UserService initialized with connection string: {ConnectionString}", _connectionString);
        }

        // Register a new user and calculate BMI
        public (bool success, int userId) RegisterUser(string fullName, string email, string password, int age, decimal height, decimal weight, string healthConditions, string role)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Registration failed: Missing required fields (FullName, Email, or Password)");
                    return (false, 0);
                }

                email = email.ToLower().Trim();
                _logger.LogInformation("Registering user with email: {Email}", email);

                // Check for duplicate email
                using var connection = new SqlConnection(_connectionString);
                _logger.LogInformation("Opening database connection for registration");
                connection.Open();
                var checkCommand = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE LOWER(Email) = LOWER(@Email)", connection);
                checkCommand.Parameters.AddWithValue("@Email", email);
                int emailCount = (int)checkCommand.ExecuteScalar();
                if (emailCount > 0)
                {
                    _logger.LogWarning("Registration failed: Email {Email} already exists", email);
                    return (false, 0);
                }

                // Calculate BMI: weight (kg) / (height (m)^2)
                float heightInMeters = (float)height / 100; // Convert cm to meters
                float bmi = (float)weight / (heightInMeters * heightInMeters);

                // Hash password and insert user
                string hashedPassword = HashPassword(password.Trim());
                _logger.LogInformation("Hashed password for {Email}: {HashedPassword}", email, hashedPassword);

                var insertCommand = new SqlCommand(
                    "INSERT INTO dbo.Users (FullName, Email, Password, Age, Height, Weight, HealthConditions, Role, ActivityLevel, BMI) " +
                    "OUTPUT INSERTED.UserId " +
                    "VALUES (@FullName, @Email, @Password, @Age, @Height, @Weight, @HealthConditions, @Role, 'Sedentary', @BMI)",
                    connection);
                insertCommand.Parameters.AddWithValue("@FullName", fullName);
                insertCommand.Parameters.AddWithValue("@Email", email);
                insertCommand.Parameters.AddWithValue("@Password", hashedPassword);
                insertCommand.Parameters.AddWithValue("@Age", age);
                insertCommand.Parameters.AddWithValue("@Height", (float)height);
                insertCommand.Parameters.AddWithValue("@Weight", (float)weight);
                insertCommand.Parameters.AddWithValue("@HealthConditions", (object)healthConditions ?? DBNull.Value);
                insertCommand.Parameters.AddWithValue("@Role", role ?? "User");
                insertCommand.Parameters.AddWithValue("@BMI", bmi);

                int newUserId = (int)insertCommand.ExecuteScalar();
                _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}, BMI: {BMI}", email, newUserId, bmi);
                return (true, newUserId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during registration for {Email}: {Message}", email, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}: {Message}", email, ex.Message);
                return (false, 0);
            }
        }

        // Login user with improved logging
        public (bool success, string? role, int userId) LoginUser(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Login failed: Email or password is empty");
                    return (false, null, 0);
                }

                email = email.ToLower().Trim();
                string hashedPassword = HashPassword(password.Trim());
                _logger.LogInformation("Login attempt for {Email}, Hashed Password: {HashedPassword}", email, hashedPassword);

                _logger.LogInformation("Opening database connection for login");
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                _logger.LogInformation("Database connection opened successfully for {Email}", email);

                var command = new SqlCommand(
                    "SELECT UserId, Role FROM dbo.Users WHERE LOWER(Email) = LOWER(@Email) AND Password = @Password",
                    connection);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@Password", hashedPassword);

                _logger.LogInformation("Executing login query for {Email}", email);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int userId = reader.GetInt32(0);
                    string role = reader.GetString(1);
                    _logger.LogInformation("User logged in successfully: {Email}, UserId: {UserId}, Role: {Role}", email, userId, role);
                    return (true, role, userId);
                }

                _logger.LogWarning("Login failed: Invalid email or password for {Email}", email);
                return (false, null, 0);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error during login for {Email}: {Message}", email, ex.Message);
                throw; // Re-throw to let the endpoint handle it
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}: {Message}", email, ex.Message);
                return (false, null, 0);
            }
        }

        // Get user details by ID
        public UserData? GetUserById(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching user data for UserId: {UserId}", userId);
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand("SELECT * FROM dbo.Users WHERE UserId = @UserId", connection);
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var user = new UserData(
                        reader.GetString(1), // FullName
                        reader.GetString(2), // Email
                        reader.GetString(3), // Password (hashed)
                        reader.GetInt32(4),  // Age
                        (decimal)reader.GetFloat(5), // Height
                        (decimal)reader.GetFloat(6), // Weight
                        reader.IsDBNull(7) ? null : reader.GetString(7), // HealthConditions
                        reader.GetString(8),  // Role
                        reader.IsDBNull(10) ? (float?)null : reader.GetFloat(10) // BMI
                    )
                    { UserId = reader.GetInt32(0) };
                    _logger.LogInformation("User data retrieved for UserId: {UserId}", userId);
                    return user;
                }

                _logger.LogWarning("User not found for UserId: {UserId}", userId);
                return null;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching user by ID {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching user by ID {UserId}: {Message}", userId, ex.Message);
                return null;
            }
        }

        // Get all users
        public List<UserData> GetAllUsers()
        {
            var users = new List<UserData>();
            try
            {
                _logger.LogInformation("Fetching all users");
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand("SELECT * FROM dbo.Users", connection);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new UserData(
                        reader.GetString(1), // FullName
                        reader.GetString(2), // Email
                        reader.GetString(3), // Password (hashed)
                        reader.GetInt32(4),  // Age
                        (decimal)reader.GetFloat(5), // Height
                        (decimal)reader.GetFloat(6), // Weight
                        reader.IsDBNull(7) ? null : reader.GetString(7), // HealthConditions
                        reader.GetString(8),  // Role
                        reader.IsDBNull(10) ? (float?)null : reader.GetFloat(10) // BMI
                    )
                    { UserId = reader.GetInt32(0) });
                }
                _logger.LogInformation("Retrieved {Count} users", users.Count);
                return users;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching all users: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching all users: {Message}", ex.Message);
                return users; // Return empty list instead of throwing
            }
        }

        // Update height and weight, recalculate BMI
        public bool UpdateHeightWeight(int userId, decimal height, decimal weight)
        {
            try
            {
                _logger.LogInformation("Updating height and weight for UserId: {UserId}", userId);
                float heightInMeters = (float)height / 100; // Convert cm to meters
                float bmi = (float)weight / (heightInMeters * heightInMeters);

                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                var command = new SqlCommand(
                    "UPDATE dbo.Users SET Height = @Height, Weight = @Weight, BMI = @BMI WHERE UserId = @UserId",
                    connection);
                command.Parameters.AddWithValue("@Height", (float)height);
                command.Parameters.AddWithValue("@Weight", (float)weight);
                command.Parameters.AddWithValue("@BMI", bmi);
                command.Parameters.AddWithValue("@UserId", userId);

                int rowsAffected = command.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Height and weight updated for UserId: {UserId}, BMI: {BMI}", userId, bmi);
                    return true;
                }
                _logger.LogWarning("No user found to update for UserId: {UserId}", userId);
                return false;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error updating height and weight for UserId {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating height and weight for UserId {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        // Password hashing utility
        private string HashPassword(string password)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing password: {Message}", ex.Message);
                throw;
            }
        }
    }
}