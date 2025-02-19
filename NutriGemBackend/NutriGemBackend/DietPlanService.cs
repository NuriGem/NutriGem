using System;
using Microsoft.Data.SqlClient;

class DietPlanService
{
    private readonly string _connectionString = "Server=localhost;Database=NutriGem;Trusted_Connection=True;TrustServerCertificate=True;";

    // ✅ Add Diet Plan for User
    public void AddDietPlan(int userId, string planType, int calories, decimal proteins, decimal carbs, decimal fats)
    {
        if (userId <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid user ID.");
            Console.ResetColor();
            return;
        }

        if (proteins > 9999 || carbs > 9999 || fats > 9999)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Error: Values for Proteins, Carbs, or Fats are too high. Please enter a reasonable number.");
            Console.ResetColor();
            return;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = @"
                    INSERT INTO DietPlans (UserID, PlanType, CaloriesPerDay, ProteinsPerDay, CarbsPerDay, FatsPerDay)
                    VALUES (@UserID, @PlanType, @Calories, @Proteins, @Carbs, @Fats)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@PlanType", planType);
                    cmd.Parameters.AddWithValue("@Calories", calories);
                    cmd.Parameters.AddWithValue("@Proteins", proteins);
                    cmd.Parameters.AddWithValue("@Carbs", carbs);
                    cmd.Parameters.AddWithValue("@Fats", fats);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Diet plan added successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Failed to add diet plan.");
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

    // ✅ View Diet Plan for a User
    public void ViewDietPlan(int userId)
    {
        if (userId <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid user ID.");
            Console.ResetColor();
            return;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = @"
                    SELECT PlanType, CaloriesPerDay, ProteinsPerDay, CarbsPerDay, FatsPerDay
                    FROM DietPlans
                    WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows)
                    {
                        Console.WriteLine("\n📌 Your Diet Plan:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"🍽️ Type: {reader["PlanType"]}");
                            Console.WriteLine($"🔥 Calories: {reader["CaloriesPerDay"]}");
                            Console.WriteLine($"💪 Proteins: {reader["ProteinsPerDay"]}g");
                            Console.WriteLine($"🥖 Carbs: {reader["CarbsPerDay"]}g");
                            Console.WriteLine($"🧈 Fats: {reader["FatsPerDay"]}g");
                            Console.WriteLine("-----------------------------");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ No diet plan found. Please create one.");
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

    // ✅ Update Diet Plan
    public void UpdateDietPlan(int userId, int calories, decimal proteins, decimal carbs, decimal fats)
    {
        if (userId <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid user ID.");
            Console.ResetColor();
            return;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = @"
                    UPDATE DietPlans
                    SET CaloriesPerDay = @Calories,
                        ProteinsPerDay = @Proteins,
                        CarbsPerDay = @Carbs,
                        FatsPerDay = @Fats
                    WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@Calories", calories);
                    cmd.Parameters.AddWithValue("@Proteins", proteins);
                    cmd.Parameters.AddWithValue("@Carbs", carbs);
                    cmd.Parameters.AddWithValue("@Fats", fats);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Diet plan updated successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ No diet plan found to update.");
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

    // ✅ Delete Diet Plan
    public void DeleteDietPlan(int userId)
    {
        Console.Write("Are you sure you want to delete this diet plan? (yes/no): ");
        string confirmation = Console.ReadLine()?.ToLower();
        if (confirmation != "yes")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("❌ Deletion canceled.");
            Console.ResetColor();
            return;
        }

        if (userId <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid user ID.");
            Console.ResetColor();
            return;
        }

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "DELETE FROM DietPlans WHERE UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Diet plan deleted successfully!");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ No diet plan found to delete.");
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

    // ✅ View All Diet Plans (Admin Only)
    public void ViewAllDietPlans()
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = "SELECT * FROM DietPlans";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    Console.WriteLine("\n📋 All Diet Plans:");

                    while (reader.Read())
                    {
                        Console.WriteLine($"🆔 UserID: {reader["UserID"]}");
                        Console.WriteLine($"🍽️ Type: {reader["PlanType"]}");
                        Console.WriteLine($"🔥 Calories: {reader["CaloriesPerDay"]}");
                        Console.WriteLine($"💪 Proteins: {reader["ProteinsPerDay"]}g");
                        Console.WriteLine($"🥖 Carbs: {reader["CarbsPerDay"]}g");
                        Console.WriteLine($"🧈 Fats: {reader["FatsPerDay"]}g");
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
}