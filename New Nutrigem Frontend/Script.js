// Handle registration form submission
document.getElementById('registration-form').addEventListener('submit', async function (e) {
    e.preventDefault();

    const data = {
        fullName: document.getElementById('fullName').value,
        email: document.getElementById('regEmail').value,
        password: document.getElementById('regPassword').value,
        age: parseInt(document.getElementById('age').value),
        height: parseFloat(document.getElementById('height').value),
        weight: parseFloat(document.getElementById('weight').value),
        healthConditions: document.getElementById('healthConditions').value,
        role: document.getElementById('userRole').value
    };

    try {
        const response = await fetch("/api/user/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data)
        });
        const result = await response.text();
        document.getElementById('registration-result').innerText = result;
    } catch (error) {
        console.error("Registration error:", error);
        document.getElementById('registration-result').innerText = "Registration failed: " + error.message;
    }
});

// Handle login form submission
document.getElementById('login-form').addEventListener('submit', async function (e) {
    e.preventDefault();

    const data = {
        email: document.getElementById('loginEmail').value,
        password: document.getElementById('loginPassword').value
    };

    try {
        const response = await fetch("/api/user/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(data)
        });
        if (response.ok) {
            const result = await response.json();
            localStorage.setItem('userId', result.userId);
            document.getElementById('login-result').innerText = "Login successful! Welcome, " + result.fullName + " (" + result.role + ")";
            setTimeout(() => {
                if (result.role === "Admin") {
                    window.location.href = "adminDashboard.html";
                } else {
                    window.location.href = "userDashboard.html";
                }
            }, 1000); // Delay redirect to show the success message
        } else {
            document.getElementById('login-result').innerText = "Login failed: Invalid email or password.";
        }
    } catch (error) {
        console.error("Login error:", error);
        document.getElementById('login-result').innerText = "Login failed: " + error.message;
    }
});