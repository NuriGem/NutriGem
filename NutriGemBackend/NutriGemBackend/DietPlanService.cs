using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

public class DietPlanService
{
    private readonly string _connectionString = "Server=localhost;Database=NutriGem;Trusted_Connection=True;TrustServerCertificate=True;";

    public (int mealId, int calories, decimal proteins, decimal carbs, decimal fats) RecommendMeal(int userId, string category)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = @"
                SELECT ml.MealID, ml.MealName, ml.Calories, ml.Proteins, ml.Carbs, ml.Fats
                FROM MealLibrary ml
                WHERE ml.Category = @Category
                ORDER BY CASE 
                    WHEN @Category = 'MuscleGain' THEN ml.Proteins
                    WHEN @Category = 'WeightLoss' THEN -ml.Calories
                    ELSE ml.Calories
                END DESC";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Category", category);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int mealId = (int)reader["MealID"];
                    string mealName = reader["MealName"]?.ToString() ?? "Unnamed Meal";
                    int calories = (int)reader["Calories"];
                    decimal proteins = (decimal)reader["Proteins"];
                    decimal carbs = (decimal)reader["Carbs"];
                    decimal fats = (decimal)reader["Fats"];
                    Console.WriteLine($"📌 Recommended Meal: {mealName} (Calories: {calories}, Proteins: {proteins}g)");
                    return (mealId, calories, proteins, carbs, fats);
                }
                return (-1, 0, 0, 0, 0);
            }
        }
    }

    public void AddDietPlan(int userId, string planType, int mealId, int calories, decimal proteins, decimal carbs, decimal fats)
    {
        if (userId <= 0 || mealId <= 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid user ID or meal ID.");
            Console.ResetColor();
            return;
        }

        string activityLevel = GetActivityLevel(userId);
        decimal activityMultiplier = activityLevel switch
        {
            "Sedentary" => 1.0m,
            "Lightly Active" => 1.2m,
            "Moderately Active" => 1.4m,
            "Very Active" => 1.6m,
            _ => 1.0m
        };
        calories = (int)(calories * activityMultiplier);
        Console.WriteLine($"Adjusted calories for {activityLevel} lifestyle: {calories} kcal");

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            try
            {
                conn.Open();
                string query = @"
                    INSERT INTO DietPlans (UserID, PlanType, MealID, CaloriesPerDay, ProteinsPerDay, CarbsPerDay, FatsPerDay)
                    VALUES (@UserID, @PlanType, @MealID, @Calories, @Proteins, @Carbs, @Fats)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    cmd.Parameters.AddWithValue("@PlanType", planType);
                    cmd.Parameters.AddWithValue("@MealID", mealId);
                    cmd.Parameters.AddWithValue("@Calories", calories);
                    cmd.Parameters.AddWithValue("@Proteins", proteins);
                    cmd.Parameters.AddWithValue("@Carbs", carbs);
                    cmd.Parameters.AddWithValue("@Fats", fats);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Diet plan added successfully with meal!");
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

    public void AddDietPlan(int userId, string planType)
    {
        string dietCategory = planType == "Weight Loss" ? "WeightLoss" : "MuscleGain";
        var (recommendedMealId, recommendedCalories, recommendedProteins, recommendedCarbs, recommendedFats) = RecommendMeal(userId, dietCategory);
        if (recommendedMealId != -1)
        {
            AddDietPlan(userId, planType, recommendedMealId, recommendedCalories, recommendedProteins, recommendedCarbs, recommendedFats);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ No meals available for recommendation.");
            Console.ResetColor();
        }
    }

    private string GetActivityLevel(int userId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();
            string query = "SELECT ActivityLevel FROM Users WHERE UserId = @UserId";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                return cmd.ExecuteScalar()?.ToString() ?? "Sedentary";
            }
        }
    }

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
                    SELECT dp.PlanType, dp.CaloriesPerDay, dp.ProteinsPerDay, dp.CarbsPerDay, dp.FatsPerDay,
                           ml.MealName, ml.FoodItems
                    FROM DietPlans dp
                    LEFT JOIN MealLibrary ml ON dp.MealID = ml.MealID
                    WHERE dp.UserID = @UserID";

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
                            Console.WriteLine($"🍽️ Meal: {reader["MealName"]}");
                            Console.WriteLine($"🥗 Food Items: {reader["FoodItems"]}");
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