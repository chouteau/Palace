using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace PalaceServer.Pages
{
    public class LoginForm
    {
        [Required]
        public string Key { get; set; }
    }

    public class LoginValidator : ComponentBase
    {
        private ValidationMessageStore _messageStore;
        [CascadingParameter]
        public EditContext CurrentEditContext { get; set; }

        protected override void OnInitialized()
        {
            if (CurrentEditContext == null)
            {
                throw new InvalidOperationException();
            }
            _messageStore = new(CurrentEditContext);
            CurrentEditContext.OnValidationRequested += (s, arg) => _messageStore.Clear();
        }

        public void DisplayErrors(Dictionary<string, List<string>> errors)
        {
            foreach (var error in errors)
            {
                _messageStore.Add(CurrentEditContext.Field(error.Key), error.Value);
            }
            CurrentEditContext.NotifyValidationStateChanged();
        }
    }

    public partial class Login
    {
        [Inject] Configuration.PalaceServerSettings PalaceServerSettings { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] Services.AdminLoginContext AdminLoginContext { get; set; }

        public LoginForm LoginForm { get; set; } = new();
        public LoginValidator LoginValidator { get; set; } = new();

        public void Validate()
        {
            var errors = new Dictionary<string, List<string>>();
            if (PalaceServerSettings.AdminKey != LoginForm.Key)
            {
                errors.Add(nameof(LoginForm.Key), new List<string> { "invalid key" });
            }

            if (errors.Any())
            {
                LoginValidator.DisplayErrors(errors);
                return;
            }
            var token = Guid.NewGuid();
            AdminLoginContext.AddToken(token);
            NavigationManager.NavigateTo($"/?Token={token}", true);
        }

    }
}
