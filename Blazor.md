``` razor
@page "/login"
@using System.Security.Claims
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authentication.Cookies
@inject IHttpContextAccessor HttpCtx
@inject NavigationManager Nav

<h3>Login</h3>

@if (!string.IsNullOrEmpty(error))
{
    <div class="alert alert-danger">@error</div>
}

<EditForm Model="@model"
          OnValidSubmit="HandleLogin"
          FormName="LoginForm"
          Method="FormMethod.Post">
    <AntiforgeryToken FormName="LoginForm" />
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label class="form-label">Username</label>
        <InputText class="form-control" @bind-Value="model.Username" />
    </div>
    <div class="mb-3">
        <label class="form-label">Password</label>
        <InputText type="password" class="form-control" @bind-Value="model.Password" />
        <div class="form-text">Demo password is <code>123456</code>.</div>
    </div>

    <button class="btn btn-primary" type="submit">Login</button>
</EditForm>

@code {
    private readonly LoginModel model = new();
    private string? error;

    private async Task HandleLogin()
    {
        if (string.IsNullOrWhiteSpace(model.Username) || model.Password != "123456")
        {
            error = "Invalid username or password.";
            return;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, model.Username),
            new Claim(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpCtx.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        Nav.NavigateTo("/", replace: true);
    }

    public class LoginModel
    {
        [System.ComponentModel.DataAnnotations.Required] public string Username { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required] public string Password { get; set; } = "";
    }
}
