using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace BakeryPOS.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty]
        private ObservableCollection<Product> _products;

        [ObservableProperty]
        private string _newProductName = string.Empty;

        [ObservableProperty]
        private string _newProductPrice = string.Empty;

        [ObservableProperty]
        private string _newProductCode = string.Empty;

        [ObservableProperty]
        private string _newProductAvailableDays = "1,2,3,4,5,6,7";

        public bool IsAdmin => AppSession.IsAdmin;

        public ProductsViewModel()
        {
            _context = new AppDbContext();
            LoadData();
        }

        public void LoadData()
        {
            // Forzar recarga de entidades
            _context.ChangeTracker.Entries().ToList().ForEach(e => e.Reload());
            Products = new ObservableCollection<Product>(_context.Products.ToList());
        }

        [RelayCommand]
        private void AddProduct()
        {
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("Solo administradores pueden agregar productos.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Validación estricta de campos obligatorios
            var missingFields = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(NewProductName)) missingFields.Add("Nombre");
            if (string.IsNullOrWhiteSpace(NewProductCode)) missingFields.Add("Código");
            
            // Intentar parsear el precio desde el string
            decimal priceValue = 0;
            string cleanPrice = NewProductPrice.Replace(",", "."); // Normalizar para evitar errores de cultura
            if (!decimal.TryParse(cleanPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out priceValue) || priceValue <= 0)
            {
                missingFields.Add("Precio válido (mayor a 0)");
            }

            if (missingFields.Any())
            {
                string message = "Para registrar el producto falta completar:\n- " + string.Join("\n- ", missingFields);
                System.Windows.MessageBox.Show(message, "Información Incompleta", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Verificar si el código ya existe
            if (_context.Products.Any(p => p.Code == NewProductCode))
            {
                System.Windows.MessageBox.Show($"El código '{NewProductCode}' ya está registrado con otro producto. Por favor, asigne un código único.", "Código Duplicado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var product = new Product
                {
                    Name = NewProductName,
                    Price = priceValue,
                    Code = NewProductCode,
                    Category = "General", // Valor por defecto interno para la DB
                    AvailableDays = string.IsNullOrWhiteSpace(NewProductAvailableDays) ? "1,2,3,4,5,6,7" : NewProductAvailableDays,
                    Stock = 0
                };

                _context.Products.Add(product);
                _context.SaveChanges();

                Products.Add(product);
                
                NewProductName = string.Empty;
                NewProductPrice = string.Empty;
                NewProductCode = string.Empty;
                NewProductAvailableDays = "1,2,3,4,5,6,7";
                
                System.Windows.MessageBox.Show("Producto guardado exitosamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar el producto: {ex.Message}\n{ex.InnerException?.Message}", "Error de Base de Datos", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _context.ChangeTracker.Clear(); // Evitar que el objeto fallido siga rastreado
            }
        }

        [RelayCommand]
        private void UpdateProduct(Product product)
        {
            if (product == null) return;

            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("Solo administradores pueden modificar productos.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                _context.SaveChanges();
                System.Windows.MessageBox.Show($"Producto '{product.Name}' actualizado correctamente.", "Éxito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al actualizar el producto: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _context.ChangeTracker.Clear();
                LoadData(); // Recargar para revertir cambios visuales no guardados
            }
        }

        [RelayCommand]
        private void DeleteProduct(Product product)
        {
            if (product == null) return;
            
            if (!IsAdmin)
            {
                System.Windows.MessageBox.Show("Solo administradores pueden eliminar productos.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show($"¿Está seguro de eliminar el producto '{product.Name}'?\nAdvertencia: Si tiene ventas asociadas, no se podrá eliminar.", "Confirmar Eliminación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _context.Products.Remove(product);
                    _context.SaveChanges();
                    Products.Remove(product);
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"No se puede eliminar el producto porque tiene ventas o registros de producción asociados.\nError: {ex.Message}", "Error al Eliminar", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _context.ChangeTracker.Clear();
                }
            }
        }
        [RelayCommand]
        private void ExportDatabase()
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string sourcePath = System.IO.Path.Combine(localAppData, "BakeryPOS", "bakery_pos.db");

                if (!System.IO.File.Exists(sourcePath))
                {
                    System.Windows.MessageBox.Show("No se encontró la base de datos actual para exportar.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Base de Datos SQLite (*.db)|*.db",
                    FileName = $"Inventario_Alan_{DateTime.Now:yyyyMMdd_HHmm}.db",
                    Title = "Exportar Respaldo de Inventario"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.Copy(sourcePath, saveFileDialog.FileName, true);
                    System.Windows.MessageBox.Show("Base de datos exportada exitosamente.", "Respaldo Creado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al exportar: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ImportDatabase()
        {
            var result = System.Windows.MessageBox.Show("¡ADVERTENCIA CRÍTICA!\n\nAl importar una base de datos, se REEMPLAZARÁ TODA la información actual (productos, stock, ventas y usuarios). \n\n¿Deseas continuar?", "Confirmar Importación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Base de Datos SQLite (*.db)|*.db",
                    Title = "Seleccionar Base de Datos para Importar"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string destPath = System.IO.Path.Combine(localAppData, "BakeryPOS", "bakery_pos.db");

                    // 1. Cerrar conexión actual para liberar el archivo
                    _context.Database.CloseConnection();
                    _context.Dispose();

                    // 2. Sobreescribir el archivo
                    System.IO.File.Copy(openFileDialog.FileName, destPath, true);

                    System.Windows.MessageBox.Show("Base de datos importada correctamente. El sistema se cerrará para aplicar los cambios.", "Importación Exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    
                    // 3. Reiniciar la aplicación para recargar todo
                    System.Windows.Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al importar: {ex.Message}\n\nSi el error persiste, asegúrate de que el archivo no esté abierto en otro programa.", "Error de Importación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
