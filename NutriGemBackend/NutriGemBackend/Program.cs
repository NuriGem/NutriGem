using System;
using System.Threading;
using Microsoft.Data.SqlClient;


class Program
{
    static void Main()
    {
        UserService userService = new UserService();
        DietPlanService dietPlanService = new DietPlanService();

        while (true) // Keeps the program running until explicitly exited
        {
            Console.Clear();
            Console.WriteLine("🔹 Welcome to NutriGem!");
            Console.WriteLine("1️⃣ Register as User");
            Console.WriteLine("2️⃣ Register as Admin");
            Console.WriteLine("3️⃣ Login");
            Console.WriteLine("0️⃣ Exit");
            Console.Write("Enter choice: ");
            string choice = GetNonEmptyInput("choice");

            if (choice == "0")
            {
                Console.WriteLine("👋 Exiting NutriGem. Goodbye!");
                break;
            }

            switch (choice)
            {
                case "1":
                case "2":
                    HandleRegistration(userService, dietPlanService, choice == "1" ? "User" : "Admin");
                    break;

                case "3":
                    HandleLogin(userService, dietPlanService);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice. Please try again.");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    private static void HandleRegistration(UserService userService, DietPlanService dietPlanService, string role)
    {
        Console.Write("👤 Full Name: ");
        string fullName = GetNonEmptyInput("Full Name");

        Console.Write("📧 Email: ");
        string email = GetValidEmail();

        Console.Write("🔒 Password (min 8 characters): ");
        string password = GetValidPassword();

        Console.Write("🎂 Age: ");
        int age = GetValidIntegerInput("Age");

        Console.Write("📏 Height (cm): ");
        decimal height = GetValidDecimalInput("Height");

        Console.Write("⚖️ Weight (kg): ");
        decimal weight = GetValidDecimalInput("Weight");

        Console.Write("🏥 Health Conditions (if any, or type 'None'): ");
        string healthConditions = Console.ReadLine()?.Trim() ?? "None";

        bool registrationSuccess = userService.RegisterUser(fullName, email, password, age, height, weight, healthConditions, role);

        if (registrationSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ {role} registered successfully! Redirecting to menu...");
            Console.ResetColor();
            Thread.Sleep(1500);

            if (role == "User")
            {
                int userId = userService.GetUserIdByEmail(email);
                if (userId != -1)
                    ShowUserMenu(dietPlanService, userService, userId);
            }
            else
            {
                ShowAdminMenu(userService, dietPlanService);
            }
        }
    }

    private static void HandleLogin(UserService userService, DietPlanService dietPlanService)
    {
        Console.Write("📧 Enter Email: ");
        string email = GetValidEmail();

        Console.Write("🔒 Enter Password: ");
        string password = GetNonEmptyInput("Password");

        var (success, role, userId) = userService.LoginUser(email, password);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Logged in as {role}!");
            Console.ResetColor();
            Thread.Sleep(1500);

            if (role == "User")
                ShowUserMenu(dietPlanService, userService, userId);
            else
                ShowAdminMenu(userService, dietPlanService);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid email or password. Please try again.");
            Console.ResetColor();
            Thread.Sleep(1500);
        }
    }

    private static void ShowUserMenu(DietPlanService dietPlanService, UserService userService, int userId)
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("👤 User Dashboard");
            Console.WriteLine("1️⃣ View Your Diet Plan");
            Console.WriteLine("2️⃣ Add Diet Plan");
            Console.WriteLine("3️⃣ Update Diet Plan");
            Console.WriteLine("4️⃣ Delete Diet Plan");
            Console.WriteLine("5️⃣ Log Water Intake");
            Console.WriteLine("6️⃣ View Water Intake History");
            Console.WriteLine("7️⃣ Log Weight Progress");
            Console.WriteLine("8️⃣ View Weight Progress");
            Console.WriteLine("9 Log an Exercise");
            Console.WriteLine("1️0 View Calories Burned from Exercise Today");
            Console.WriteLine("11 View Your Health Summary");
            Console.WriteLine("0️⃣ Logout");

            Console.Write("Enter your choice: ");
            string action = GetNonEmptyInput("choice");

            switch (action)
            {
                case "1":
                    dietPlanService.ViewDietPlan(userId);
                    break;

                case "2":
                    AddDietPlan(dietPlanService, userService, userId);
                    break;

                case "3":
                    UpdateDietPlan(dietPlanService, userId);
                    break;

                case "4":
                    dietPlanService.DeleteDietPlan(userId);
                    break;

                case "5":
                    LogWaterIntake(userService, userId);
                    break;

                case "6":
                    userService.ViewWaterIntake(userId);
                    break;

                case "7":
                    LogWeightProgress(userService, userId);
                    break;

                case "8":
                    userService.ViewWeightProgress(userId);
                    break;
                case "9":
                    LogExercise(userService, userId);
                    break;
                case "10":
                    ViewExerciseCaloriesBurned(userService, userId);
                    break;

                case "11":
                    ViewUserHealthSummary(userService, userId);
                    break;
               

                case "0":
                    keepRunning = false;
                    Console.WriteLine("👋 Logging out...");
                    Thread.Sleep(1000);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice.");
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine("🔄 Press Enter to continue...");
            Console.ReadLine();
        }
    }

    private static void ShowAdminMenu(UserService userService, DietPlanService dietPlanService)
    {
        bool keepRunning = true;
        while (keepRunning)
        {
            Console.Clear();
            Console.WriteLine("🛠️ Admin Dashboard");
            Console.WriteLine("1️⃣ View All Users");
            Console.WriteLine("2️⃣ Delete a User");
            Console.WriteLine("3️⃣ Promote/Demote User Role");
            Console.WriteLine("4️⃣ View All Diet Plans");
            Console.WriteLine("0️⃣ Logout");

            Console.Write("Enter your choice: ");
            string adminChoice = GetNonEmptyInput("choice");

            switch (adminChoice)
            {
                case "1":
                    userService.ViewAllUsers();
                    break;

                case "2":
                    DeleteUser(userService);
                    break;

                case "3":
                    UpdateUserRole(userService);
                    break;

                case "4":
                    dietPlanService.ViewAllDietPlans();
                    break;

                case "0":
                    keepRunning = false;
                    Console.WriteLine("👋 Logging out...");
                    Thread.Sleep(1000);
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Invalid choice.");
                    Console.ResetColor();
                    break;
            }
            Console.WriteLine("🔄 Press Enter to continue...");
            Console.ReadLine();
        }
    }

    private static void AddDietPlan(DietPlanService dietPlanService, UserService userService, int userId)
    {
        Console.WriteLine("🍽️ Choose a Diet Goal:");
        Console.WriteLine("1️⃣ Weight Loss");
        Console.WriteLine("2️⃣ Muscle Gain");
        Console.Write("Enter choice: ");
        string goalChoice = Console.ReadLine()?.Trim();

        string planType;
        string dietCategory;

        if (goalChoice == "1")
        {
            planType = "Weight Loss";
            dietCategory = "WeightLoss";
        }
        else if (goalChoice == "2")
        {
            planType = "Muscle Gain";
            dietCategory = "MuscleGain";
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ Invalid selection.");
            Console.ResetColor();
            return;
        }

        // Select a meal from MealLibrary
        int mealId = SelectMealFromLibrary(userService, dietCategory);
        if (mealId == -1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ No meal selected.");
            Console.ResetColor();
            return;
        }

        // Retrieve meal details from MealLibrary
        using (SqlConnection conn = new SqlConnection(userService.GetConnectionString()))
        {
            conn.Open();
            string query = "SELECT Calories, Proteins, Carbs, Fats FROM MealLibrary WHERE MealID = @MealID";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MealID", mealId);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int calories = (int)reader["Calories"];
                    decimal proteins = (decimal)reader["Proteins"];
                    decimal carbs = (decimal)reader["Carbs"];
                    decimal fats = (decimal)reader["Fats"];

                    reader.Close();

                    // Add the selected meal to the user's diet plan
                    dietPlanService.AddDietPlan(userId, planType, mealId, calories, proteins, carbs, fats);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Diet Plan Created Successfully!");
                    Console.ResetColor();
                }
            }
        }
    }

    private static int SelectMealFromLibrary(UserService userService, string category)
    {
        using (SqlConnection conn = new SqlConnection(userService.GetConnectionString()))
        {
            conn.Open();

            Console.WriteLine($"🍽️ Select a meal from MealLibrary ({category}):");
            string query = "SELECT MealID, MealName FROM MealLibrary WHERE Category = @Category";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Category", category);
                SqlDataReader reader = cmd.ExecuteReader();
                Dictionary<int, string> mealOptions = new Dictionary<int, string>();

                while (reader.Read())
                {
                    int mealId = (int)reader["MealID"];
                    string mealName = reader["MealName"].ToString();
                    Console.WriteLine($"{mealId} - {mealName}");
                    mealOptions.Add(mealId, mealName);
                }
                reader.Close();

                if (mealOptions.Count == 0)
                {
                    Console.WriteLine("❌ No meals available for this category.");
                    return -1;
                }
            }

            Console.Write("Enter Meal ID to select: ");
            int selectedMealId;
            if (int.TryParse(Console.ReadLine(), out selectedMealId))
            {
                return selectedMealId;
            }

            Console.WriteLine("❌ Invalid meal selection.");
            return -1;
        }
    }


    private static void UpdateDietPlan(DietPlanService dietPlanService, int userId)
    {
        Console.Write("🔥 New Calories: ");
        int calories = GetValidIntegerInput("Calories");

        Console.Write("💪 New Proteins (g): ");
        decimal proteins = GetValidDecimalInput("Proteins");

        Console.Write("🥖 New Carbs (g): ");
        decimal carbs = GetValidDecimalInput("Carbs");

        Console.Write("🧈 New Fats (g): ");
        decimal fats = GetValidDecimalInput("Fats");

        dietPlanService.UpdateDietPlan(userId, calories, proteins, carbs, fats);
    }

    private static void LogWaterIntake(UserService userService, int userId)
    {
        Console.Write("💧 Enter water intake (ml): ");
        int waterIntake = GetValidIntegerInput("Water Intake");
        userService.LogWaterIntake(userId, waterIntake);
    }

    private static void LogWeightProgress(UserService userService, int userId)
    {
        Console.Write("⚖️ Enter your weight (kg): ");
        decimal weight = GetValidDecimalInput("Weight");
        userService.LogWeightProgress(userId, weight);
    }

    private static void LogExercise(UserService userService, int userId)
    {
        Console.WriteLine("\n💪 Choose a Muscle Group:");
        Console.WriteLine("1️⃣ Cardio");
        Console.WriteLine("2️⃣ Chest");
        Console.WriteLine("3️⃣ Legs");
        Console.Write("Enter choice: ");
        string muscleChoice = Console.ReadLine()?.Trim();

        string muscleGroup;
        switch (muscleChoice)
        {
            case "1": muscleGroup = "Cardio"; break;
            case "2": muscleGroup = "Chest"; break;
            case "3": muscleGroup = "Legs"; break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Invalid selection.");
                Console.ResetColor();
                return;
        }

        using (SqlConnection conn = new SqlConnection(userService.GetConnectionString()))
        {
            conn.Open();
            string query = "SELECT ExerciseID, ExerciseName FROM ExerciseLibrary WHERE MuscleGroup = @MuscleGroup";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@MuscleGroup", muscleGroup);
                SqlDataReader reader = cmd.ExecuteReader();
                Dictionary<int, string> exerciseOptions = new Dictionary<int, string>();

                while (reader.Read())
                {
                    int exerciseId = (int)reader["ExerciseID"];
                    string exerciseName = reader["ExerciseName"].ToString();
                    Console.WriteLine($"{exerciseId} - {exerciseName}");
                    exerciseOptions.Add(exerciseId, exerciseName);
                }
                reader.Close();

                if (exerciseOptions.Count == 0)
                {
                    Console.WriteLine("❌ No exercises available for this category.");
                    return;
                }
            }

            Console.Write("Enter Exercise ID to log: ");
            if (int.TryParse(Console.ReadLine(), out int selectedExerciseId))
            {
                Console.Write("Enter duration in minutes: ");
                if (int.TryParse(Console.ReadLine(), out int durationMinutes) && durationMinutes > 0)
                {
                    userService.LogExercise(userId, selectedExerciseId, durationMinutes);
                }
                else
                {
                    Console.WriteLine("❌ Invalid duration.");
                }
            }
            else
            {
                Console.WriteLine("❌ Invalid exercise selection.");
            }
        }
    }
    private static void ViewExerciseCaloriesBurned(UserService userService, int userId)
    {
        Console.WriteLine("📊 Fetching today's exercise calories burned...");
        userService.ViewExerciseCaloriesBurned(userId);
    }

    private static void ViewUserHealthSummary(UserService userService, int userId)
    {
        Console.WriteLine("📊 Fetching your health summary...");
        userService.ViewUserHealthSummary(userId);
    }

    private static void DeleteUser(UserService userService)
    {
        Console.Write("📌 Enter User Email to delete: ");
        string email = GetValidEmail();
        userService.DeleteUser(email);
    }

    private static void UpdateUserRole(UserService userService)
    {
        Console.Write("📌 Enter User Email to change role: ");
        string email = GetValidEmail();

        Console.Write("🔄 New Role (User/Admin): ");
        string newRole = GetNonEmptyInput("Role");

        userService.UpdateUserRole(email, newRole);
    }

    // ✅ Helper methods for input validation
    private static string GetNonEmptyInput(string fieldName)
    {
        string input;
        do
        {
            input = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"❌ {fieldName} cannot be empty. Please enter again: ");
                Console.ResetColor();
            }
        } while (string.IsNullOrEmpty(input));
        return input;
    }

    private static string GetValidEmail()
    {
        string email;
        do
        {
            email = GetNonEmptyInput("Email");
            if (!email.Contains("@") || !email.Contains("."))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("❌ Invalid email format. Please enter a valid email: ");
                Console.ResetColor();
            }
        } while (!email.Contains("@") || !email.Contains("."));
        return email;
    }

    private static string GetValidPassword()
    {
        string password;
        do
        {
            password = GetNonEmptyInput("Password");
            if (password.Length < 8)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("❌ Password must be at least 8 characters. Please try again: ");
                Console.ResetColor();
            }
        } while (password.Length < 8);
        return password;
    }

    private static int GetValidIntegerInput(string fieldName)
    {
        int value;
        while (true)
        {
            string input = GetNonEmptyInput(fieldName);
            if (int.TryParse(input, out value) && value > 0)
                return value;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"❌ Invalid {fieldName}. Please enter a positive number: ");
            Console.ResetColor();
        }
    }

    private static decimal GetValidDecimalInput(string fieldName)
    {
        decimal value;
        while (true)
        {
            string input = GetNonEmptyInput(fieldName);
            if (decimal.TryParse(input, out value) && value > 0)
                return value;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"❌ Invalid {fieldName}. Please enter a positive number: ");
            Console.ResetColor();
        }
    }
}