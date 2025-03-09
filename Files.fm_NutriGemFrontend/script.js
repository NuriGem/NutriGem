// Toggle between registration and login sections
document.getElementById('show-register').addEventListener('click', function() {
  document.getElementById('registration-section').style.display = 'block';
  document.getElementById('login-section').style.display = 'none';
});

document.getElementById('show-login').addEventListener('click', function() {
  document.getElementById('registration-section').style.display = 'none';
  document.getElementById('login-section').style.display = 'block';
});

// Handle registration form submission
document.getElementById('registration-form').addEventListener('submit', async function(e) {
  e.preventDefault();

  const data = {
    fullName: document.getElementById('fullName').value,
    email: document.getElementById('regEmail').value,
    password: document.getElementById('regPassword').value,
    age: parseInt(document.getElementById('age').value),
    height: parseFloat(document.getElementById('height').value),
    weight: parseFloat(document.getElementById('weight').value),
    healthConditions: document.getElementById('healthConditions').value,
    role: document.getElementById('userRole').value  // New: Role selection
  };

  try {
    const response = await fetch("http://localhost:5158/api/user/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    });
    const result = await response.text();
    document.getElementById('registration-result').innerText = result;
  } catch (error) {
    console.error("Registration error:", error);
    document.getElementById('registration-result').innerText = "Registration failed: " + error;
  }
});

// Handle login form submission
document.getElementById('login-form').addEventListener('submit', async function(e) {
  e.preventDefault();

  const data = {
    email: document.getElementById('loginEmail').value,
    password: document.getElementById('loginPassword').value
  };

  try {
    const response = await fetch("http://localhost:5158/api/user/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    });
    if (response.ok) {
      const result = await response.json();
      // Echo the user's full name along with the role on successful login
      document.getElementById('login-result').innerText = "Login successful! Welcome, " + result.FullName + " (" + result.Role + ")";
      // Redirect based on role if needed:
      if(result.Role === "Admin") {
         window.location.href = "adminDashboard.html";
      } else {
         window.location.href = "userDashboard.html";
      }
    } else {
      document.getElementById('login-result').innerText = "Login failed: Invalid email or password.";
    }
  } catch (error) {
    console.error("Login error:", error);
    document.getElementById('login-result').innerText = "Login failed: " + error;
  }
});
