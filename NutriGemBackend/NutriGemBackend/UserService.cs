using System;
using Microsoft.Data.SqlClient;
using BCrypt.Net;




class UserService
{
    private readonly string _connectionString = "Server=localhost;Database=NutriGem;Trusted_Connection=True;TrustServerCertificate=True;";

    // ✅ Register User
    public bool RegisterUser(string fullName, string email, string password, int age, decimal height, decimal weight, string healthConditions, string role)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error: Full Name, Email, and Password are required.");
            Console.ResetColor();
            return false;
        }

        if (password.Length < 8)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error: Password must be at least 8 characters.");
            Console.ResetColor();
            return false;
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();

                // Check if email already exists
                string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkEmailQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Email", email);
                    int existingUserCount = (int)checkCmd.ExecuteScalar();

                    if (existingUserCount > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Error: This email is already registered.");
                        Console.ResetColor();
                        return false;
                    }
                }

                // Insert new user
                string query = @"
                    INSERT INTO Users (FullName, Email, PasswordHash, Age, Height, Weight, HealthConditions, UserRole)
                    VALUES (@FullName, @Email, @PasswordHash, @Age, @Height, @Weight, @HealthConditions, @UserRole)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    cmd.Parameters.AddWithValue("@Age", age);
                    cmd.Parameters.AddWithValue("@Height", height);
                    cmd.Parameters.AddWithValue("@Weight", weight);
                    cmd.Parameters.AddWithValue("@HealthConditions", healthConditions);
                    cmd.Parameters.AddWithValue("@UserRole", role);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ {role} registered successfully!");
                        Console.ResetColor();
                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to register user.");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
    }

    // ✅ Login User
    public (bool success, string role, int userId) LoginUser(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error: Email and Password are required.");
            Console.ResetColor();
            return (false, null, -1);
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT UserID, PasswordHash, UserRole FROM Users WHERE Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedHash = reader["PasswordHash"].ToString();
                        bool isMatch = BCrypt.Net.BCrypt.Verify(password, storedHash);

                        if (isMatch)
                        {
                            int userId = Convert.ToInt32(reader["UserID"]);
                            string role = reader["UserRole"].ToString();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"✅ Logged in as {role}!");
                            Console.ResetColor();
                            return (true, role, userId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Invalid email or password.");
        Console.ResetColor();
        return (false, null, -1);
    }

    // ✅ Get User ID by Email
    public int GetUserIdByEmail(string email)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT UserID FROM Users WHERE Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
                return -1;
            }
        }
    }

    // ✅ View All Users (Admin Only)
    public void ViewAllUsers()
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT UserID, FullName, Email, UserRole FROM Users";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    Console.WriteLine("\n📋 All Users:");

                    while (reader.Read())
                    {
                        Console.WriteLine($"🆔 UserID: {reader["UserID"]}");
                        Console.WriteLine($"👤 Full Name: {reader["FullName"]}");
                        Console.WriteLine($"📧 Email: {reader["Email"]}");
                        Console.WriteLine($"🔑 Role: {reader["UserRole"]}");
                        Console.WriteLine("-----------------------------");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ Delete User (Admin Only)
    public void DeleteUser(string email)
    {
        Console.Write("Are you sure you want to delete this user? (yes/no): ");
        string confirmation = Console.ReadLine()?.ToLower();
        if (confirmation != "yes")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("❌ Deletion canceled.");
            Console.ResetColor();
            return;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "DELETE FROM Users WHERE Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ User deleted successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ No user found with this email.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ Update User Role (Admin Only)
    public void UpdateUserRole(string email, string newRole)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "UPDATE Users SET UserRole = @UserRole WHERE Email = @Email";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@UserRole", newRole);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ User role updated successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ No user found with this email.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ Log Water Intake
    public void LogWaterIntake(int userId, int amountML)
    {
        Console.Write("Enter date (yyyy-MM-dd) or press Enter for today: ");
        string input = Console.ReadLine();
        DateTime date = string.IsNullOrEmpty(input) ? DateTime.Today : DateTime.Parse(input);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "INSERT INTO WaterIntake (UserID, AmountML, IntakeDate) VALUES (@UserID, @AmountML, @IntakeDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@AmountML", amountML);
                    cmd.Parameters.AddWithValue("@IntakeDate", date);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Water intake logged successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to log water intake.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ View Water Intake History
    public void ViewWaterIntake(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT IntakeDate, AmountML FROM WaterIntake WHERE UserID = @UserID ORDER BY IntakeDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    Console.WriteLine("\n💧 Water Intake History:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"📅 {reader["IntakeDate"]}: {reader["AmountML"]}ml");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ Log Weight Progress
    public void LogWeightProgress(int userId, decimal weightKG)
    {
        Console.Write("Enter date (yyyy-MM-dd) or press Enter for today: ");
        string input = Console.ReadLine();
        DateTime date = string.IsNullOrEmpty(input) ? DateTime.Today : DateTime.Parse(input);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "INSERT INTO WeightProgress (UserID, WeightKG, EntryDate) VALUES (@UserID, @WeightKG, @EntryDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@WeightKG", weightKG);
                    cmd.Parameters.AddWithValue("@EntryDate", date);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Weight progress logged successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to log weight progress.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    // ✅ View Weight Progress
    public void ViewWeightProgress(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT EntryDate, WeightKG FROM WeightProgress WHERE UserID = @UserID ORDER BY EntryDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    Console.WriteLine("\n📊 Weight Progress History:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"📅 {reader["EntryDate"]}: {reader["WeightKG"]} kg");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}