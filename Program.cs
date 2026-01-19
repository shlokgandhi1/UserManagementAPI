

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Global error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception e)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var errorMessage= new { error = "Internal server error.", details = e.Message };
        await context.Response.WriteAsJsonAsync(errorMessage);
    }
});


// Authentication middleware
app.Use(async (context, next) =>
{
   // Skip authentication for the root path 
   if (context.Request.Path == "/")
    {
        await next();
        return;
    }

    // Simple token-based authentication
    if (!context.Request.Headers.TryGetValue("Authorization", out var token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Missing Authorization header." });
        return;
    }

    // validate token
    if (token != "Bearer thisIsASecretToken")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid token." });
        return;
    }

    await next();
});


// Logging middleware
var logger = app.Logger;
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    var status = context.Response.StatusCode;
    logger.LogInformation("{Method} {Path} => {Status}", method, path, status);
});



// Endpoints (CRUD operations for User Management)
app.MapGet("/", () => "Welcome to the User Management API!");
var users = new List<User>();


// GET all users
app.MapGet("/users", () =>
{
    return Results.Ok(users.ToList());
});


// GET user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound("User not found.");
});


// POST create new user
app.MapPost("/users", (User user) =>
{
    if (string.IsNullOrWhiteSpace(user.Name))
        return Results.BadRequest("Name is required.");

    if (string.IsNullOrWhiteSpace(user.Email))
        return Results.BadRequest("Email is required.");

    if (!user.Email.Contains("@"))
        return Results.BadRequest("Invalid email format.");

    if (users.Any(u => u.Email == user.Email))
        return Results.BadRequest("Email already exists.");

    var newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    var newUser = user with { Id = newId };
    
    // Add the new user to the list
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
});


// PUT update existing user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound("User not found.");

    if (string.IsNullOrWhiteSpace(updatedUser.Name))
        return Results.BadRequest("Name is required.");

    if (string.IsNullOrWhiteSpace(updatedUser.Email))
        return Results.BadRequest("Email is required.");

    if (!updatedUser.Email.Contains("@"))
        return Results.BadRequest("Invalid email format.");

    if (users.Any(u => u.Email == updatedUser.Email && u.Id != id))
        return Results.BadRequest("Email already in use by another user.");

    var updatedUserRecord = user with
    {
        Name = updatedUser.Name,
        Email = updatedUser.Email
    };

    // Replace the old user record with the updated one
    users[users.IndexOf(user)] = updatedUserRecord;
    return Results.NoContent();
});


// DELETE user by ID
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound("User not found.");

    users.Remove(user);
    return Results.NoContent();
});



app.Run();

// User record definition

record User(int Id, string Name, string Email);
