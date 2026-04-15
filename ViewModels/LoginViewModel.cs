using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Windows;
using System;
using System.Collections.ObjectModel;

namespace BakeryPOS.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        public Action OnLoginSuccess { get; set; }

        public ObservableCollection<User> Users { get; } = new();

        [ObservableProperty]
        private User _selectedUser;

        [ObservableProperty]
        private string _password;

        [ObservableProperty]
        private string _errorMessage;

        public LoginViewModel()
        {
            _context = new AppDbContext();
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                var usersList = _context.Users.OrderBy(u => u.Username).ToList();
                foreach (var user in usersList)
                {
                    Users.Add(user);
                }
                
                // Pre-select the first user if any
                if (Users.Any())
                {
                    SelectedUser = Users.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar usuarios: " + ex.Message;
            }
        }

        [RelayCommand]
        private void Login(object passwordProvider)
        {
            ErrorMessage = string.Empty;

            if (SelectedUser == null)
            {
                ErrorMessage = "Seleccione un usuario.";
                return;
            }

            // El proveedor de contraseña será el PasswordBox enviado desde la Vista
            string passwordToVerify = string.Empty;
            if (passwordProvider is System.Windows.Controls.PasswordBox pb)
            {
                passwordToVerify = pb.Password;
            }

            if (string.IsNullOrWhiteSpace(passwordToVerify))
            {
                ErrorMessage = "Ingrese la contraseña.";
                return;
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(passwordToVerify, SelectedUser.PasswordHash))
            {
                ErrorMessage = "Contraseña incorrecta.";
                return;
            }

            // Set session
            AppSession.CurrentUser = SelectedUser;
            
            // Trigger success navigation
            OnLoginSuccess?.Invoke();
        }
    }
}
