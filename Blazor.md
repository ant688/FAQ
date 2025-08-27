@page "/login"
@inject NavigationManager Nav
@inject AuthenticationStateProvider AuthStateProvider
@inject Microsoft.JSInterop.IJSRuntime JS

<h3>Login</h3>

@if (!string.IsNullOrEmpty(error))
{
    <div class="alert alert-danger">@error</div>
}

<EditForm Model="@model" OnValidSubmit="HandleLogin" FormName="LoginForm">
    <AntiforgeryToken />
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

    <button class="btn btn-primary" type="submit" disabled="@busy">Login</button>
</EditForm>

@code {
    private readonly LoginModel model = new();
    private bool busy;
    private string? error;

    private async Task HandleLogin()
    {
        busy = true; error = null;
        var provider = (Authentication_example.Services.CustomAuthStateProvider)AuthStateProvider;

        if (await provider.SignInAsync(model.Username, model.Password))
        {
            var mod = await JS.InvokeAsync<IJSObjectReference>("import", "/authStorage.js");
            await mod.InvokeVoidAsync("setSessionValue", "BlazorAuthDemo:username", model.Username);
            await mod.DisposeAsync();

            Nav.NavigateTo("/", replace: true);
        }
        else
        {
            error = "Invalid username or password.";
        }
        busy = false;
    }

    public class LoginModel
    {
        [System.ComponentModel.DataAnnotations.Required] public string Username { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required] public string Password { get; set; } = "";
    }
}
