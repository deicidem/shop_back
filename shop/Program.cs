using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using shop.Data;
using shop.DTO;
using shop.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin() // Разрешаем все источники
            .AllowAnyMethod() // Разрешаем все методы (GET, POST, и т.д.)
            .AllowAnyHeader(); // Разрешаем все заголовки
    });
});

builder.Services.AddDbContext<ShopContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ShopContext>()
    .AddDefaultTokenProviders();

// Настройка JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("User", policy =>
    {
        policy.RequireRole("User");  // Политика для роли "User"
    })
    .AddPolicy("Admin", policy =>
    {
        policy.RequireRole("Admin");  // Политика для роли "Admin"
    });


var app = builder.Build();
app.UseCors("AllowAllOrigins"); 
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

DataSeeder.Seed(app);

// --------
// **AUTH** 
// --------
app.MapGet("/auth", (ClaimsPrincipal user) =>
{
    if (user.Identity is { IsAuthenticated: false })
        return Results.Unauthorized();

    return Results.Ok(new
    {
        Username = user.Identity.Name,
        Claims = user.Claims.Select(c => new { c.Type, c.Value })
    });
}).RequireAuthorization();

app.MapPost("/register", async ([FromBody] RegisterRequest request, [FromServices] UserManager<User> userManager, [FromServices] RoleManager<IdentityRole> roleManager) =>
{
    var user = new User { UserName = request.UserName, Email = request.Email };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded) return Results.BadRequest(result.Errors);

    // Назначаем пользователю роль "User"
    var addRoleResult = await userManager.AddToRoleAsync(user, "User");
    if (!addRoleResult.Succeeded)
        return Results.BadRequest(new { error = "Failed to assign User role" });

    
    return Results.Ok("User registered successfully");
});


app.MapPost("/login", async ([FromBody] LoginRequest request, [FromServices] UserManager<User> userManager, SignInManager<User> signInManager, [FromServices] IConfiguration config) =>
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user == null)
    {
        return Results.BadRequest(new { isSuccess = false, error = "Invalid email or password", data = (object)null });
    }
    
    var result = await signInManager.PasswordSignInAsync(user, request.Password, false, false);

    if (!result.Succeeded)
    {
        return Results.Unauthorized();
    } 

    var roles = await userManager.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Email, user.Email!),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: creds);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

app.MapPost("/logout", async ([FromServices] SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok(new { message = "Logged out successfully" });
}).RequireAuthorization();


// ------------
// **PRODUCTS** 
// ------------

// Получить все продукты
app.MapGet("/products", async (ShopContext db) =>
    await db.Products.ToListAsync()).RequireAuthorization("User");

// Получить продукт по ID
app.MapGet("/products/{id:int}", async (int id, ShopContext db) =>
    await db.Products.FindAsync(id) is Product product ? Results.Ok(product) : Results.NotFound());

// Добавить продукт
app.MapPost("/products", async (Product product, ShopContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
}).RequireAuthorization("Admin");

// Обновить продукт
app.MapPut("/products/{id:int}", async (int id, Product updatedProduct, ShopContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = updatedProduct.Name;
    product.Description = updatedProduct.Description;
    product.Price = updatedProduct.Price;
    product.Stock = updatedProduct.Stock;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");

// Удалить продукт
app.MapDelete("/products/{id:int}", async (int id, ShopContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization("Admin");


// ----------
// **ORDERS** 
// ----------

// Получить заказы пользователя
app.MapGet("/user-orders", async (ClaimsPrincipal user, ShopContext db) =>
{
    if (user.Identity is { IsAuthenticated: false })
        return Results.Unauthorized();

    var userEmail = user.FindFirst(ClaimTypes.Name)?.Value;
    if (string.IsNullOrEmpty(userEmail)) return Results.BadRequest("User email not found");

    var orders = await db.Orders
        .Where(o => o.CustomerEmail == userEmail)
        .Include(o => o.Items)
        .ToListAsync();

    return Results.Ok(orders);
}).RequireAuthorization();

// Получить заказы пользователя
app.MapGet("/user-orders/{id:int}", async (int id, ClaimsPrincipal user, ShopContext db) =>
{
    if (user.Identity is { IsAuthenticated: false })
        return Results.Unauthorized();

    var userEmail = user.FindFirst(ClaimTypes.Name)?.Value;
    if (string.IsNullOrEmpty(userEmail)) return Results.BadRequest("User email not found");

    var order = await db.Orders
        .Where(o => o.CustomerEmail == userEmail && o.Id == id)
        .Include(o => o.Items)
        .FirstOrDefaultAsync();

    return order is not null ? Results.Ok(order) : Results.NotFound();
}).RequireAuthorization();

// Оплата заказа
app.MapPut("/orders/{id:int}/pay", async (int id, ClaimsPrincipal user, ShopContext db) =>
{
    if (user.Identity is { IsAuthenticated: false })
        return Results.Unauthorized();

    var userEmail = user.FindFirst(ClaimTypes.Name)?.Value;
    var order = await db.Orders.FindAsync(id);

    if (order is null || order.CustomerEmail != userEmail)
        return Results.NotFound();

    order.Status = OrderStatus.Paid;
    await db.SaveChangesAsync();
    return Results.Ok("Order paid successfully");
}).RequireAuthorization();

// Отмена заказа
app.MapDelete("/orders/{id:int}/cancel", async (int id, ClaimsPrincipal user, ShopContext db) =>
{
    if (user.Identity is { IsAuthenticated: false })
        return Results.Unauthorized();

    var userEmail = user.FindFirst(ClaimTypes.Name)?.Value;
    var order = await db.Orders.FindAsync(id);

    if (order is null || order.CustomerEmail != userEmail)
        return Results.NotFound();

    if (order.Status == OrderStatus.Paid)
        return Results.BadRequest("Paid orders cannot be canceled");

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.Ok("Order canceled successfully");
}).RequireAuthorization();

// Получить все заказы
app.MapGet("/orders", async (ShopContext db) =>
    await db.Orders.Include(o => o.Items).ToListAsync()).RequireAuthorization("Admin");

// Получить заказ по ID
app.MapGet("/orders/{id:int}", async (int id, ShopContext db) =>
    await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id) 
        is Order order ? Results.Ok(order) : Results.NotFound()).RequireAuthorization("Admin");

// Создать заказ
app.MapPost("/orders", async (Order order, ShopContext db) =>
{
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
});

// Обновить статус заказа
app.MapPut("/orders/{id:int}/status", async (int id, OrderStatus status, ShopContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.Status = status;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Удалить заказ
app.MapDelete("/orders/{id:int}", async (int id, ShopContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();