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
                string query = "INSERT INTO WaterIntake (UserID, WaterAmountML, LogDate) VALUES (@UserID, @WaterAmountML, @LogDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@WaterAmountML", amountML);
                    cmd.Parameters.AddWithValue("@LogDate", date);


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
                string query = "SELECT LogDate, WaterAmountML FROM WaterIntake WHERE UserID = @UserID ORDER BY LogDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    Console.WriteLine("\n💧 Water Intake History:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"📅 {reader["LogDate"]}: {reader["WaterAmountML"]}ml");
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
                string query = "INSERT INTO WeightTracking (UserID, Weight, LogDate) VALUES (@UserID, @Weight, @LogDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Weight", weightKG);
                    cmd.Parameters.AddWithValue("@LogDate", date);


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
                string query = "SELECT LogDate, Weight FROM WeightTracking WHERE UserID = @UserID ORDER BY LogDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    Console.WriteLine("\n📊 Weight Progress History:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"📅 {reader["LogDate"]}: {reader["Weight"]} kg");
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

    public void ViewUserHealthSummary(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
            -- Get user's diet plan
            SELECT dp.PlanType, ml.MealName, ml.FoodItems, dp.CaloriesPerDay, dp.ProteinsPerDay, dp.CarbsPerDay, dp.FatsPerDay
            FROM DietPlans dp
            LEFT JOIN MealLibrary ml ON dp.MealID = ml.MealID
            WHERE dp.UserID = @UserID;

            -- Get most recent water intake
            SELECT TOP 1 WaterAmountML, LogDate
            FROM WaterIntake 
            WHERE UserID = @UserID 
            ORDER BY LogDate DESC;

            -- Get most recent weight entry
            SELECT TOP 1 Weight, LogDate 
            FROM WeightTracking 
            WHERE UserID = @UserID 
            ORDER BY LogDate DESC;
        ";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                SqlDataReader reader = cmd.ExecuteReader();

                Console.WriteLine("\n📊 Your Health Summary:");

                // ✅ Display Diet Plan
                if (reader.Read())
                {
                    Console.WriteLine("\n🍽️ Diet Plan:");
                    Console.WriteLine($"🔥 Type: {reader["PlanType"]}");
                    Console.WriteLine($"🍽️ Meal: {reader["MealName"]}");
                    Console.WriteLine($"🥗 Food Items: {reader["FoodItems"]}");
                    Console.WriteLine($"🔥 Calories: {reader["CaloriesPerDay"]} kcal");
                    Console.WriteLine($"💪 Proteins: {reader["ProteinsPerDay"]} g");
                    Console.WriteLine($"🥖 Carbs: {reader["CarbsPerDay"]} g");
                    Console.WriteLine($"🧈 Fats: {reader["FatsPerDay"]} g");
                }
                else
                {
                    Console.WriteLine("❌ No diet plan found.");
                }

                // ✅ Display Most Recent Water Intake
                if (reader.NextResult() && reader.Read())
                {
                    Console.WriteLine("\n💧 Most Recent Water Intake:");
                    Console.WriteLine($"📅 Date: {reader["LogDate"]}");
                    Console.WriteLine($"💦 Amount: {reader["WaterAmountML"]} ml");
                }
                else
                {
                    Console.WriteLine("❌ No water intake records found.");
                }

                // ✅ Display Most Recent Weight Entry
                if (reader.NextResult() && reader.Read())
                {
                    Console.WriteLine("\n⚖️ Current Weight:");
                    Console.WriteLine($"📅 Date: {reader["LogDate"]}");
                    Console.WriteLine($"⚖️ Weight: {reader["Weight"]} kg");
                }
                else
                {
                    Console.WriteLine("❌ No weight records found.");
                }
            }
        }
    }
    public void LogExercise(int userId, int exerciseId, int durationMinutes)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            // Get exercise details
            string getExerciseQuery = "SELECT CaloriesBurnedPerHour FROM ExerciseLibrary WHERE ExerciseID = @ExerciseID";

            using (SqlCommand cmd = new SqlCommand(getExerciseQuery, conn))
            {
                cmd.Parameters.AddWithValue("@ExerciseID", exerciseId);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int caloriesPerHour = (int)reader["CaloriesBurnedPerHour"];
                    reader.Close();

                    // Calculate calories burned based on duration
                    int caloriesBurned = (caloriesPerHour * durationMinutes) / 60;

                    // Insert into ExerciseLogs
                    string insertQuery = "INSERT INTO ExerciseLogs (UserID, ExerciseID, DurationMinutes, CaloriesBurned, LogDate) VALUES (@UserID, @ExerciseID, @Duration, @Calories, GETDATE())";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@UserID", userId);
                        insertCmd.Parameters.AddWithValue("@ExerciseID", exerciseId);
                        insertCmd.Parameters.AddWithValue("@Duration", durationMinutes);
                        insertCmd.Parameters.AddWithValue("@Calories", caloriesBurned);

                        insertCmd.ExecuteNonQuery();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✅ Exercise logged! Duration: {durationMinutes} mins, Calories Burned: {caloriesBurned} kcal.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid exercise selection.");
                    Console.ResetColor();
                }
            }
        }
    }

    public void ViewExerciseCaloriesBurned(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
            -- Get total calories burned from exercises today
            SELECT SUM(el.CaloriesBurned) AS TotalCaloriesBurned
            FROM ExerciseLogs el
            WHERE el.UserID = @UserID AND CAST(el.LogDate AS DATE) = CAST(GETDATE() AS DATE);

            -- Get detailed breakdown of exercises performed today
            SELECT el.LogDate, ex.ExerciseName, el.DurationMinutes, el.CaloriesBurned
            FROM ExerciseLogs el
            INNER JOIN ExerciseLibrary ex ON el.ExerciseID = ex.ExerciseID
            WHERE el.UserID = @UserID AND CAST(el.LogDate AS DATE) = CAST(GETDATE() AS DATE)
            ORDER BY el.LogDate DESC;
        ";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                SqlDataReader reader = cmd.ExecuteReader();

                Console.WriteLine("\n🔥 Today's Exercise Summary:");

                // ✅ Display Total Calories Burned
                if (reader.Read())
                {
                    int totalCalories = reader["TotalCaloriesBurned"] != DBNull.Value ? (int)reader["TotalCaloriesBurned"] : 0;
                    Console.WriteLine($"🔥 Total Calories Burned: {totalCalories} kcal");
                }

                // ✅ Display Breakdown of Exercises
                if (reader.NextResult())
                {
                    Console.WriteLine("\n📋 Exercise Breakdown:");
                    bool hasExercises = false;
                    while (reader.Read())
                    {
                        hasExercises = true;
                        Console.WriteLine($"📅 {reader["LogDate"]}: {reader["ExerciseName"]} - {reader["DurationMinutes"]} mins, {reader["CaloriesBurned"]} kcal");
                    }

                    if (!hasExercises)
                    {
                        Console.WriteLine("❌ No exercises logged today.");
                    }
                }
            }
        }
    }





    public string GetConnectionString()
    {
        return _connectionString;
    }

}

