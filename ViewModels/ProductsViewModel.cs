using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;

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
        private decimal _newProductPrice;

        [ObservableProperty]
        private string _newProductCode = string.Empty;

        [ObservableProperty]
        private string _newProductAvailableDays = "1,2,3,4,5,6,7";

        public bool IsAdmin => AppSession.IsAdmin;

        public ProductsViewModel()
        {
            _context = new AppDbContext();
            LoadProducts();
        }

        private void LoadProducts()
        {
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
            if (NewProductPrice <= 0) missingFields.Add("Precio (mayor a 0)");
            if (string.IsNullOrWhiteSpace(NewProductCode)) missingFields.Add("Código");

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
                    Price = NewProductPrice,
                    Code = NewProductCode,
                    Category = "General", // Valor por defecto interno para la DB
                    AvailableDays = string.IsNullOrWhiteSpace(NewProductAvailableDays) ? "1,2,3,4,5,6,7" : NewProductAvailableDays,
                    Stock = 0
                };

                _context.Products.Add(product);
                _context.SaveChanges();

                Products.Add(product);
                
                NewProductName = string.Empty;
                NewProductPrice = 0;
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
    }
}
