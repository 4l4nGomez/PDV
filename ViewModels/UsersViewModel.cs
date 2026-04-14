using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace BakeryPOS.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<User> _users;

        [ObservableProperty]
        private string _newUsername;

        [ObservableProperty]
        private string _newPassword;

        [ObservableProperty]
        private string _selectedRole = "cajero";

        public List<string> Roles { get; } = new List<string> { "admin", "cajero", "panadero" };

        public UsersViewModel()
        {
            _context = new AppDbContext();
            LoadUsers();
        }

        private void LoadUsers()
        {
            var allUsers = _context.Users.ToList();
            Users = new ObservableCollection<User>(allUsers);
        }

        [RelayCommand]
        private void CreateUser()
        {
            if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewPassword))
            {
                MessageBox.Show("Usuario y contraseña son requeridos.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_context.Users.Any(u => u.Username.ToLower() == NewUsername.ToLower()))
            {
                MessageBox.Show("El nombre de usuario ya existe.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newUser = new User
            {
                Username = NewUsername,
                Role = SelectedRole,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPassword)
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            NewUsername = string.Empty;
            NewPassword = string.Empty;
            LoadUsers();
            
            MessageBox.Show($"Usuario {newUser.Username} creado exitosamente con el rol {newUser.Role}.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void DeleteUser(User user)
        {
            if (user == null) return;
            
            if (user.Username.ToLower() == "admin")
            {
                MessageBox.Show("No se puede eliminar al administrador principal.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (user.Id == AppSession.CurrentUser.Id)
            {
                MessageBox.Show("No puedes eliminarte a ti mismo mientras estás en sesión.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = MessageBox.Show($"¿Estás seguro de que deseas eliminar al usuario {user.Username}?", "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                LoadUsers();
            }
        }
    }
}
