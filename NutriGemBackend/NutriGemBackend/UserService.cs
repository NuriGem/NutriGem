using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using BCrypt.Net;

public class UserService
{
    private readonly string _connectionString = "Server=localhost;Database=NutriGem;Trusted_Connection=True;TrustServerCertificate=True;";

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

        decimal heightInMeters = height / 100;
        decimal bmi = weight / (heightInMeters * heightInMeters);
        Console.WriteLine($"DEBUG: Calculated BMI = {bmi:F2} for UserID (to be inserted) with Height={height}cm, Weight={weight}kg");

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        Console.WriteLine("What is your activity level? (Sedentary, Lightly Active, Moderately Active, Very Active): ");
        string activityLevel = Console.ReadLine()?.Trim() ?? "Sedentary";
        if (!new[] { "Sedentary", "Lightly Active", "Moderately Active", "Very Active" }.Contains(activityLevel))
        {
            activityLevel = "Sedentary";
            Console.WriteLine("Invalid activity level. Defaulting to Sedentary.");
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            SqlTransaction transaction = null;
            try
            {
                conn.Open();
                Console.WriteLine("DEBUG: Database connection opened successfully");

                transaction = conn.BeginTransaction();
                Console.WriteLine("DEBUG: Transaction started");

                string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                using (SqlCommand checkCmd = new SqlCommand(checkEmailQuery, conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@Email", email);
                    int existingUserCount = (int)checkCmd.ExecuteScalar();
                    Console.WriteLine($"DEBUG: Existing user count = {existingUserCount}");

                    if (existingUserCount > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Error: This email is already registered.");
                        Console.ResetColor();
                        transaction.Rollback();
                        Console.WriteLine("DEBUG: Transaction rolled back due to existing email");
                        return false;
                    }
                }

                string query = @"
                    INSERT INTO Users (FullName, Email, PasswordHash, Age, Height, Weight, HealthConditions, UserRole, BMI, CreatedAt, ActivityLevel)
                    VALUES (@FullName, @Email, @PasswordHash, @Age, @Height, @Weight, @HealthConditions, @UserRole, @BMI, @CreatedAt, @ActivityLevel)";
                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                    cmd.Parameters.AddWithValue("@Age", age);
                    cmd.Parameters.AddWithValue("@Height", height);
                    cmd.Parameters.AddWithValue("@Weight", weight);
                    cmd.Parameters.AddWithValue("@HealthConditions", healthConditions);
                    cmd.Parameters.AddWithValue("@UserRole", role);
                    cmd.Parameters.AddWithValue("@BMI", bmi);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ActivityLevel", activityLevel);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    Console.WriteLine($"DEBUG: Rows affected during registration = {rowsAffected}");
                    if (rowsAffected > 0)
                    {
                        string verifyQuery = "SELECT TOP 1 BMI, Height, Weight FROM Users WHERE Email = @Email ORDER BY UserID DESC";
                        using (SqlCommand verifyCmd = new SqlCommand(verifyQuery, conn, transaction))
                        {
                            verifyCmd.Parameters.AddWithValue("@Email", email);
                            using (SqlDataReader reader = verifyCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    decimal verifiedBmi = reader["BMI"] != DBNull.Value ? Convert.ToDecimal(reader["BMI"]) : 0m;
                                    decimal verifiedHeight = Convert.ToDecimal(reader["Height"]);
                                    decimal verifiedWeight = Convert.ToDecimal(reader["Weight"]);
                                    Console.WriteLine($"DEBUG: Verified BMI={verifiedBmi:F2}, Height={verifiedHeight}cm, Weight={verifiedWeight}kg");
                                }
                                else
                                {
                                    Console.WriteLine("DEBUG: Failed to verify inserted user data");
                                }
                            }
                        }

                        transaction.Commit();
                        Console.WriteLine("DEBUG: Transaction committed successfully");
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ {role} registered successfully! Your BMI is {bmi:F2}.");
                        Console.ResetColor();
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.WriteLine("DEBUG: Transaction rolled back due to no rows affected");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to register user.");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                    Console.WriteLine("DEBUG: Transaction rolled back due to exception");
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                return false;
            }
        }
    }

    public (bool success, string? role, int userId) LoginUser(string email, string password)
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
                        string storedHash = reader["PasswordHash"]?.ToString() ?? string.Empty;
                        bool isMatch = BCrypt.Net.BCrypt.Verify(password, storedHash);

                        if (isMatch)
                        {
                            int userId = Convert.ToInt32(reader["UserID"]);
                            string? role = reader["UserRole"]?.ToString();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"✅ Logged in as {role ?? "Unknown Role"}!");
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
                        Console.WriteLine($"📅 {reader["LogDate"]}: {reader["WaterAmountML"]} ml");
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

    public void LogWeightProgress(int userId, decimal weightKG)
    {
        Console.Write("Enter date (yyyy-MM-dd) or press Enter for today: ");
        string input = Console.ReadLine();
        DateTime date = string.IsNullOrEmpty(input) ? DateTime.Today : DateTime.Parse(input);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            SqlTransaction transaction = null;
            try
            {
                conn.Open();
                Console.WriteLine($"DEBUG: Logging weight for UserID = {userId}");
                transaction = conn.BeginTransaction();

                string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE UserID = @UserID";
                using (SqlCommand checkCmd = new SqlCommand(checkUserQuery, conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@UserID", userId);
                    int userCount = (int)checkCmd.ExecuteScalar();
                    if (userCount == 0)
                    {
                        Console.WriteLine($"DEBUG: No user found for UserID = {userId}");
                        transaction.Rollback();
                        return;
                    }
                }

                string heightQuery = "SELECT Height FROM Users WHERE UserID = @UserID";
                decimal height;
                using (SqlCommand heightCmd = new SqlCommand(heightQuery, conn, transaction))
                {
                    heightCmd.Parameters.AddWithValue("@UserID", userId);
                    object heightResult = heightCmd.ExecuteScalar();
                    if (heightResult != null && heightResult != DBNull.Value)
                    {
                        height = Convert.ToDecimal(heightResult);
                        Console.WriteLine($"DEBUG: Retrieved Height = {height} cm");
                    }
                    else
                    {
                        Console.WriteLine("DEBUG: No height found for UserID " + userId);
                        transaction.Rollback();
                        return;
                    }
                }

                decimal heightInMeters = height / 100;
                decimal newBmi = weightKG / (heightInMeters * heightInMeters);
                Console.WriteLine($"DEBUG: Calculated new BMI = {newBmi:F2} with Weight={weightKG}kg");

                string insertQuery = "INSERT INTO WeightTracking (UserID, Weight, LogDate) VALUES (@UserID, @Weight, @LogDate)";
                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@UserID", userId);
                    insertCmd.Parameters.AddWithValue("@Weight", weightKG);
                    insertCmd.Parameters.AddWithValue("@LogDate", date);

                    int insertRows = insertCmd.ExecuteNonQuery();
                    Console.WriteLine($"DEBUG: Rows affected during weight insert = {insertRows}");
                    if (insertRows > 0)
                    {
                        string updateBmiQuery = "UPDATE Users SET BMI = @BMI, Weight = @Weight WHERE UserID = @UserID";
                        using (SqlCommand updateCmd = new SqlCommand(updateBmiQuery, conn, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@BMI", newBmi);
                            updateCmd.Parameters.AddWithValue("@Weight", weightKG);
                            updateCmd.Parameters.AddWithValue("@UserID", userId);
                            int updateRows = updateCmd.ExecuteNonQuery();
                            Console.WriteLine($"DEBUG: Rows affected during BMI update = {updateRows}");

                            string verifyQuery = "SELECT BMI, Weight FROM Users WHERE UserID = @UserID";
                            using (SqlCommand verifyCmd = new SqlCommand(verifyQuery, conn, transaction))
                            {
                                verifyCmd.Parameters.AddWithValue("@UserID", userId);
                                using (SqlDataReader reader = verifyCmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        decimal verifiedBmi = reader["BMI"] != DBNull.Value ? Convert.ToDecimal(reader["BMI"]) : 0m;
                                        decimal verifiedWeight = Convert.ToDecimal(reader["Weight"]);
                                        Console.WriteLine($"DEBUG: Verified BMI={verifiedBmi:F2}, Weight={verifiedWeight}kg");
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Weight progress logged successfully! New BMI: {newBmi:F2}");
                        Console.ResetColor();
                    }
                    else
                    {
                        transaction.Rollback();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to log weight progress.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
            }
        }
    }

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
            try
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

                    -- Get BMI from Users table
                    SELECT BMI FROM Users WHERE UserID = @UserID;

                    -- Get today's calorie intake
                    SELECT SUM(CalorieAmount) AS TotalCalories
                    FROM CalorieIntake 
                    WHERE UserID = @UserID AND CAST(LogDate AS DATE) = CAST(GETDATE() AS DATE);

                    -- Get goal details
                    SELECT TOP 1 TargetWeight, TargetCalories, GoalType, TargetDate
                    FROM Goals 
                    WHERE UserID = @UserID 
                    ORDER BY CreatedAt DESC;

                    -- Get most recent sleep log
                    SELECT TOP 1 SleepHours, SleepQuality, LogDate
                    FROM SleepLogs 
                    WHERE UserID = @UserID 
                    ORDER BY LogDate DESC;
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    Console.WriteLine("\n📊 Your Health Summary:");

                    // Display Diet Plan
                    if (reader.Read() && reader["PlanType"] != DBNull.Value)
                    {
                        Console.WriteLine("\n🍽️ Diet Plan:");
                        Console.WriteLine($"🔥 Type: {(reader["PlanType"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"🍽️ Meal: {(reader["MealName"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"🥗 Food Items: {(reader["FoodItems"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"🔥 Calories: {(reader["CaloriesPerDay"] != DBNull.Value ? reader["CaloriesPerDay"].ToString() : "N/A")} kcal");
                        Console.WriteLine($"💪 Proteins: {(reader["ProteinsPerDay"] != DBNull.Value ? Convert.ToDecimal(reader["ProteinsPerDay"]).ToString("F2") : "N/A")} g");
                        Console.WriteLine($"🥖 Carbs: {(reader["CarbsPerDay"] != DBNull.Value ? Convert.ToDecimal(reader["CarbsPerDay"]).ToString("F2") : "N/A")} g");
                        Console.WriteLine($"🧈 Fats: {(reader["FatsPerDay"] != DBNull.Value ? Convert.ToDecimal(reader["FatsPerDay"]).ToString("F2") : "N/A")} g");
                    }
                    else
                    {
                        Console.WriteLine("❌ No diet plan found.");
                    }

                    // Display Most Recent Water Intake
                    if (reader.NextResult() && reader.Read() && reader["WaterAmountML"] != DBNull.Value)
                    {
                        Console.WriteLine("\n💧 Most Recent Water Intake:");
                        Console.WriteLine($"📅 Date: {(reader["LogDate"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"💦 Amount: {reader["WaterAmountML"]} ml");
                    }
                    else
                    {
                        Console.WriteLine("❌ No water intake records found.");
                    }

                    // Display Most Recent Weight Entry
                    if (reader.NextResult() && reader.Read() && reader["Weight"] != DBNull.Value)
                    {
                        Console.WriteLine("\n⚖️ Current Weight:");
                        Console.WriteLine($"📅 Date: {(reader["LogDate"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"⚖️ Weight: {Convert.ToDecimal(reader["Weight"]):F2} kg");
                    }
                    else
                    {
                        Console.WriteLine("❌ No weight records found.");
                    }

                    // Display BMI
                    decimal? dietCalories = null;
                    if (reader.NextResult() && reader.Read() && reader["BMI"] != DBNull.Value)
                    {
                        Console.WriteLine($"DEBUG: BMI data retrieved = {reader["BMI"]}");
                        decimal bmi = Convert.ToDecimal(reader["BMI"]);
                        Console.WriteLine("\n📏 Current BMI:");
                        Console.WriteLine($"📏 BMI: {bmi:F2}");
                    }
                    else
                    {
                        Console.WriteLine("DEBUG: No BMI data retrieved");
                        Console.WriteLine("❌ BMI not available.");
                    }

                    // Display Today's Calorie Intake
                    if (reader.NextResult() && reader.Read() && reader["TotalCalories"] != DBNull.Value)
                    {
                        int totalCalories = Convert.ToInt32(reader["TotalCalories"]);
                        Console.WriteLine("\n🍽️ Today's Calorie Intake:");
                        Console.WriteLine($"🔥 Total Calories Consumed: {totalCalories} kcal");
                        if (dietCalories.HasValue)
                        {
                            Console.WriteLine($"📊 Compared to Goal: {(totalCalories > dietCalories ? "Over by " : "Under by ")}{Math.Abs(totalCalories - dietCalories.Value)} kcal");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No calorie intake records for today.");
                    }

                    // Display Goal Progress
                    if (reader.NextResult() && reader.Read())
                    {
                        decimal targetWeight = Convert.ToDecimal(reader["TargetWeight"]);
                        int targetCalories = Convert.ToInt32(reader["TargetCalories"]);
                        string goalType = reader["GoalType"].ToString();
                        DateTime targetDate = Convert.ToDateTime(reader["TargetDate"]);

                        Console.WriteLine("\n🎯 Current Goal:");
                        Console.WriteLine($"🏋️‍♂️ Goal Type: {goalType}");
                        Console.WriteLine($"⚖️ Target Weight: {targetWeight:F2} kg");
                        Console.WriteLine($"🔥 Target Calories: {targetCalories} kcal/day");
                        Console.WriteLine($"📅 Target Date: {targetDate.ToShortDateString()}");
                    }
                    else
                    {
                        Console.WriteLine("❌ No goals set.");
                    }

                    // Display Most Recent Sleep Log
                    if (reader.NextResult() && reader.Read() && reader["SleepHours"] != DBNull.Value)
                    {
                        Console.WriteLine("\n🛌 Most Recent Sleep Log:");
                        Console.WriteLine($"📅 Date: {(reader["LogDate"]?.ToString() ?? "N/A")}");
                        Console.WriteLine($"🛌 Hours: {Convert.ToDecimal(reader["SleepHours"]):F1} hours");
                        Console.WriteLine($"🌙 Quality: {reader["SleepQuality"]}");
                        decimal sleepHours = Convert.ToDecimal(reader["SleepHours"]);
                        if (sleepHours < 7)
                        {
                            Console.WriteLine("⚠️ You're getting less than 7 hours of sleep. Aim for 7-9 hours for optimal health.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ No sleep records found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Error in ViewUserHealthSummary: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack Trace: {ex.StackTrace}");
            }

            string recommendation = GenerateHealthRecommendation(userId);
            Console.WriteLine("\n💡 Health Recommendation:");
            Console.WriteLine(recommendation);
        }
    }

    public string GenerateHealthRecommendation(int userId)
    {
        decimal? bmi = null;
        int? totalCaloriesToday = null;
        int? totalCaloriesBurnedToday = null;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
                    SELECT BMI FROM Users WHERE UserID = @UserID;
                    SELECT SUM(CalorieAmount) AS TotalCalories
                    FROM CalorieIntake 
                    WHERE UserID = @UserID AND CAST(LogDate AS DATE) = CAST(GETDATE() AS DATE);
                    SELECT SUM(CaloriesBurned) AS TotalCaloriesBurned
                    FROM ExerciseLogs 
                    WHERE UserID = @UserID AND CAST(LogDate AS DATE) = CAST(GETDATE() AS DATE);
                ";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read() && reader["BMI"] != DBNull.Value)
                {
                    bmi = Convert.ToDecimal(reader["BMI"]);
                }

                if (reader.NextResult() && reader.Read() && reader["TotalCalories"] != DBNull.Value)
                {
                    totalCaloriesToday = Convert.ToInt32(reader["TotalCalories"]);
                }

                if (reader.NextResult() && reader.Read() && reader["TotalCaloriesBurned"] != DBNull.Value)
                {
                    totalCaloriesBurnedToday = Convert.ToInt32(reader["TotalCaloriesBurned"]);
                }
            }
        }

        if (!bmi.HasValue)
        {
            return "❓ Please update your weight to receive health recommendations.";
        }

        List<string> recommendations = new List<string>();
        if (bmi < 18.5m)
        {
            recommendations.Add("Your BMI is low (underweight). Consider increasing your calorie intake with nutrient-dense foods like nuts, avocados, and lean proteins.");
        }
        else if (bmi >= 18.5m && bmi < 25m)
        {
            recommendations.Add("Your BMI is in the healthy range. Maintain your current diet and exercise routine.");
        }
        else if (bmi >= 25m && bmi < 30m)
        {
            recommendations.Add("Your BMI indicates you are overweight. Aim to reduce calorie intake and increase physical activity.");
        }
        else
        {
            recommendations.Add("Your BMI indicates obesity. Consult a healthcare provider for a personalized weight loss plan.");
        }

        if (totalCaloriesToday.HasValue && totalCaloriesToday > 2500)
        {
            recommendations.Add("You've consumed a high amount of calories today. Consider lighter meals for the rest of the day.");
        }

        if (totalCaloriesBurnedToday.HasValue && totalCaloriesBurnedToday < 200)
        {
            recommendations.Add("Your activity level is low today. Try to incorporate a 30-minute walk or workout.");
        }

        return recommendations.Count > 0 ? string.Join(" | ", recommendations) : "No specific recommendations at this time.";
    }

    public void LogExercise(int userId, int exerciseId, int durationMinutes)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            string getExerciseQuery = "SELECT CaloriesBurnedPerHour FROM ExerciseLibrary WHERE ExerciseID = @ExerciseID";

            using (SqlCommand cmd = new SqlCommand(getExerciseQuery, conn))
            {
                cmd.Parameters.AddWithValue("@ExerciseID", exerciseId);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int caloriesPerHour = (int)reader["CaloriesBurnedPerHour"];
                    reader.Close();

                    int caloriesBurned = (caloriesPerHour * durationMinutes) / 60;

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

    public void SetGoal(int userId, decimal targetWeight, int targetCalories, string goalType, DateTime targetDate)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "INSERT INTO Goals (UserId, TargetWeight, TargetCalories, GoalType, TargetDate) VALUES (@UserId, @TargetWeight, @TargetCalories, @GoalType, @TargetDate)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@TargetWeight", targetWeight);
                cmd.Parameters.AddWithValue("@TargetCalories", targetCalories);
                cmd.Parameters.AddWithValue("@GoalType", goalType);
                cmd.Parameters.AddWithValue("@TargetDate", targetDate);
                cmd.ExecuteNonQuery();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Goal set successfully!");
                Console.ResetColor();
            }
        }
    }

    public void ViewGoalProgress(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
                    SELECT g.TargetWeight, g.TargetCalories, g.GoalType, g.TargetDate, u.Weight
                    FROM Goals g
                    JOIN Users u ON g.UserId = u.UserId
                    WHERE g.UserId = @UserId";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                SqlDataReader reader = cmd.ExecuteReader();
                Console.WriteLine("\n🎯 Goal Progress:");
                if (reader.Read())
                {
                    decimal targetWeight = Convert.ToDecimal(reader["TargetWeight"]);
                    int targetCalories = Convert.ToInt32(reader["TargetCalories"]);
                    string goalType = reader["GoalType"].ToString();
                    DateTime targetDate = Convert.ToDateTime(reader["TargetDate"]);
                    decimal currentWeight = Convert.ToDecimal(reader["Weight"]);

                    Console.WriteLine($"🏋️‍♂️ Goal Type: {goalType}");
                    Console.WriteLine($"⚖️ Target Weight: {targetWeight:F2} kg");
                    Console.WriteLine($"🔥 Target Calories: {targetCalories} kcal/day");
                    Console.WriteLine($"📅 Target Date: {targetDate.ToShortDateString()}");
                    Console.WriteLine($"⚖️ Current Weight: {currentWeight:F2} kg");
                    Console.WriteLine($"📈 Progress: {(targetWeight > currentWeight ? "Need to gain" : "Need to lose")} {Math.Abs(targetWeight - currentWeight):F2} kg");
                }
                else
                {
                    Console.WriteLine("❌ No goals set.");
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
                    SELECT SUM(el.CaloriesBurned) AS TotalCaloriesBurned
                    FROM ExerciseLogs el
                    WHERE el.UserID = @UserID AND CAST(el.LogDate AS DATE) = CAST(GETDATE() AS DATE);

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

                if (reader.Read())
                {
                    int totalCalories = reader["TotalCaloriesBurned"] != DBNull.Value ? (int)reader["TotalCaloriesBurned"] : 0;
                    Console.WriteLine($"🔥 Total Calories Burned: {totalCalories} kcal");
                }

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

    public void LogSleep(int userId, decimal sleepHours, string sleepQuality)
    {
        Console.Write("Enter date (yyyy-MM-dd) or press Enter for today: ");
        string input = Console.ReadLine();
        DateTime date = string.IsNullOrEmpty(input) ? DateTime.Today : DateTime.Parse(input);

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "INSERT INTO SleepLogs (UserId, SleepHours, LogDate, SleepQuality) VALUES (@UserId, @SleepHours, @LogDate, @SleepQuality)";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@SleepHours", sleepHours);
                cmd.Parameters.AddWithValue("@LogDate", date);
                cmd.Parameters.AddWithValue("@SleepQuality", sleepQuality);
                int rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Sleep logged successfully!");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Failed to log sleep.");
                    Console.ResetColor();
                }
            }
        }
    }

    public void ViewSleepHistory(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT LogDate, SleepHours, SleepQuality FROM SleepLogs WHERE UserId = @UserId ORDER BY LogDate DESC";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                SqlDataReader reader = cmd.ExecuteReader();
                Console.WriteLine("\n🛌 Sleep History:");
                while (reader.Read())
                {
                    Console.WriteLine($"📅 {reader["LogDate"]}: {reader["SleepHours"]} hours - Quality: {reader["SleepQuality"]}");
                }
            }
        }
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }
}