using Microsoft.AspNetCore.Components;

namespace MarFin_Final.Services
{
    public class AuthService
    {
        private readonly NavigationManager _navigationManager;
        private bool _isAuthenticated = false;
        private string _currentUser = string.Empty;

        public event Action? OnAuthStateChanged;

        public AuthService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public bool IsAuthenticated => _isAuthenticated;
        public string CurrentUser => _currentUser;

        public bool Login(string username, string password)
        {
            // Default credentials
            const string defaultUsername = "admin";
            const string defaultPassword = "Admin_111111";

            if (username == defaultUsername && password == defaultPassword)
            {
                _isAuthenticated = true;
                _currentUser = username;
                NotifyAuthStateChanged();
                return true;
            }

            return false;
        }

        public void Logout()
        {
            _isAuthenticated = false;
            _currentUser = string.Empty;
            NotifyAuthStateChanged();
            _navigationManager.NavigateTo("/login");
        }

        private void NotifyAuthStateChanged()
        {
            OnAuthStateChanged?.Invoke();
        }
    }
}