namespace PalaceServer.Pages;

public partial class Login : ComponentBase
{
    [Inject] Configuration.PalaceServerSettings PalaceServerSettings { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] Services.AdminLoginContext AdminLoginContext { get; set; }

    LoginForm loginForm = new();
    CustomValidator customValidator = new();

    public void Validate()
    {
        var errors = new Dictionary<string, List<string>>();
        if (PalaceServerSettings.AdminKey != loginForm.Key)
        {
            errors.Add(nameof(LoginForm.Key), new List<string> { "invalid key" });
        }

        if (errors.Any())
        {
            customValidator.DisplayErrors(errors);
            return;
        }
        var token = Guid.NewGuid();
        AdminLoginContext.AddToken(token);
        NavigationManager.NavigateTo($"/?Token={token}", true);
    }

}
