namespace shop.DTO;

public class RegisterRequest
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginRequest
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
