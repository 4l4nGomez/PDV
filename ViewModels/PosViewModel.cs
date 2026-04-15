using BakeryPOS.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BakeryPOS.ViewModels
{
    public partial class PosViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private List<Product> _allAvailableProducts;
        private SaleItem _itemToDiscount;

        public event Action RequestSearchFocus;
        public event Action RequestCartFocus;

        [ObservableProperty]
        private ObservableCollection<Product> _filteredProducts;

        [ObservableProperty]
        private ObservableCollection<SaleItem> _currentTicket;

        [ObservableProperty]
        private decimal _ticketTotal;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _cashReceivedText = "";

        [ObservableProperty]
        private decimal _changeDue;

        [ObservableProperty]
        private bool _isDiscountDialogVisible;

        [ObservableProperty]
        private decimal _proposedPrice;

        [ObservableProperty]
        private SaleItem _selectedTicketItem;

        public decimal CashReceivedNum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CashReceivedText)) return 0;
                if (decimal.TryParse(CashReceivedText, out decimal result)) return result;
                return 0;
            }
        }

        public PosViewModel()
        {
            _context = new AppDbContext();
            CurrentTicket = new ObservableCollection<SaleItem>();
            LoadData();
        }

        [RelayCommand]
        private void RemoveSelected()
        {
            if (SelectedTicketItem != null)
            {
                int index = CurrentTicket.IndexOf(SelectedTicketItem);
                RemoveFromCart(SelectedTicketItem);

                // Si quedan items, seleccionar el más cercano a la posición anterior y reenfocar (F4)
                if (CurrentTicket.Any())
                {
                    int nextIndex = Math.Min(index, CurrentTicket.Count - 1);
                    SelectedTicketItem = CurrentTicket[nextIndex];
                    RequestCartFocus?.Invoke();
                }
                else
                {
                    // Si el carrito quedó vacío, regresar al buscador (F3)
                    RequestSearchFocus?.Invoke();
                }
            }
        }

        [RelayCommand]
        private void IncrementSelected()
        {
            if (SelectedTicketItem != null)
            {
                IncrementQuantity(SelectedTicketItem);
            }
        }

        [RelayCommand]
        private void DecrementSelected()
        {
            if (SelectedTicketItem != null)
            {
                DecrementQuantity(SelectedTicketItem);
            }
        }

        [RelayCommand]
        private void ClearCart()
        {
            if (!CurrentTicket.Any()) return;

            var result = System.Windows.MessageBox.Show("¿Estás seguro de que deseas CANCELAR la venta actual? Esto borrará todos los productos del carrito.", "Confirmar Cancelación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                // Restaurar stock de cada producto antes de borrar
                foreach (var item in CurrentTicket)
                {
                    var product = _allAvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                    }
                }

                CurrentTicket.Clear();
                RecalculateTotal();
                RequestSearchFocus?.Invoke();
            }
        }

        private void LoadData()
        {
            _allAvailableProducts = _context.Products.Where(p => p.Stock > 0).ToList();
            FilterProducts();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterProducts();
        }

        partial void OnCashReceivedTextChanged(string value)
        {
            RecalculateChange();
        }

        private void RecalculateChange()
        {
            ChangeDue = CashReceivedNum - TicketTotal;
        }

        private void FilterProducts()
        {
            var query = _allAvailableProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lower = SearchText.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lower) || (p.Code != null && p.Code.ToLower().Contains(lower)));
            }

            FilteredProducts = new ObservableCollection<Product>(query.OrderBy(p => p.Name));
        }

        [RelayCommand]
        private void AddToCart(Product product)
        {
            if (product == null || product.Stock <= 0) return;

            var existingItem = CurrentTicket.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                CurrentTicket.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    Discount = 0
                });
            }

            product.Stock--;
            RecalculateTotal();
        }

        [RelayCommand]
        private void IncrementQuantity(SaleItem item)
        {
            if (item == null) return;
            var product = _allAvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null && product.Stock > 0)
            {
                item.Quantity++;
                product.Stock--;
                RecalculateTotal();
            }
        }

        [RelayCommand]
        private void DecrementQuantity(SaleItem item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity--;
                var product = _allAvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null) product.Stock++;
                RecalculateTotal();
            }
            else
            {
                RemoveFromCart(item);
            }
        }

        [RelayCommand]
        private void RemoveFromCart(SaleItem item)
        {
            if (item == null) return;
            var product = _allAvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null) product.Stock += item.Quantity;
            
            CurrentTicket.Remove(item);
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            TicketTotal = CurrentTicket.Sum(i => i.SubTotal);
            RecalculateChange();
        }

        [RelayCommand]
        private void FocusSearch()
        {
            RequestSearchFocus?.Invoke();
        }

        [RelayCommand]
        private void FocusCart()
        {
            RequestCartFocus?.Invoke();
        }

        [RelayCommand]
        private void AddByCodeOrSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            // Intentar encontrar el producto por código exacto (ideal para escáner)
            var product = _allAvailableProducts.FirstOrDefault(p => 
                p.Code != null && p.Code.Trim().Equals(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));

            // Si no hay por código, tomar el primero de la lista filtrada
            if (product == null)
            {
                product = FilteredProducts.FirstOrDefault();
            }

            if (product != null)
            {
                AddToCart(product);
                SearchText = string.Empty; // Limpiar para el siguiente escaneo
            }
        }

        [RelayCommand]
        private void Checkout()
        {
            if (!CurrentTicket.Any()) return;

            // Siempre abrir el diálogo de pago para efectivo
            var checkoutWin = new Views.CheckoutView(this);
            checkoutWin.Owner = System.Windows.Application.Current.MainWindow;
            if (checkoutWin.ShowDialog() != true) return;

            var sale = DoCheckout();
            if (sale != null) 
            {
                LoadData(); 
            }
        }

        [RelayCommand]
        private void CheckoutAndPrint()
        {
            if (CurrentTicket == null || !CurrentTicket.Any()) return;

            // 1. CAPTURA INMEDIATA (Copia aislada para que no importe si el carrito se borra)
            var listaItemsParaTicket = new System.Collections.ObjectModel.ObservableCollection<TicketItemData>();
            foreach (var item in CurrentTicket)
            {
                listaItemsParaTicket.Add(new TicketItemData
                {
                    ProductName = item.Product?.Name ?? "Producto",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal
                });
            }
            
            decimal totalFinal = TicketTotal;
            string nombreCajero = AppSession.CurrentUser?.Username ?? "Cajero";

            // 2. VENTANA DE PAGO (Aquí el carrito aún existe)
            var checkoutWin = new Views.CheckoutView(this);
            checkoutWin.Owner = System.Windows.Application.Current.MainWindow;
            if (checkoutWin.ShowDialog() != true) return;

            // 3. REGISTRO EN BASE DE DATOS (Aquí se limpia el carrito)
            var sale = DoCheckout();
            if (sale != null) 
            {
                // 4. CREAR TICKET CON LA COPIA AISLADA
                var ticketData = new TicketData
                {
                    SaleDate = DateTime.Now,
                    CashierName = nombreCajero,
                    PaymentMethod = "Efectivo",
                    TotalAmount = totalFinal,
                    Items = listaItemsParaTicket
                };

                var ticketWin = new Views.TicketView(ticketData);
                ticketWin.Owner = System.Windows.Application.Current.MainWindow;
                ticketWin.ShowDialog();
                
                LoadData(); 
            }
        }

        private Sale DoCheckout()
        {
            if (!CurrentTicket.Any()) return null;

            using (var context = new AppDbContext())
            {
                var shift = context.Shifts.FirstOrDefault(s => !s.IsClosed);
                if (shift == null)
                {
                    System.Windows.MessageBox.Show("No hay un turno abierto para registrar la venta.", "Error de Turno", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return null;
                }

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    UserId = AppSession.CurrentUser.Id,
                    ShiftId = shift.Id,
                    TotalAmount = TicketTotal,
                    PaymentMethod = "Efectivo"
                };

                context.Sales.Add(sale);
                context.SaveChanges(); // Para obtener SaleId

                foreach (var item in CurrentTicket)
                {
                    var saleItem = new SaleItem
                    {
                        SaleId = sale.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Discount = item.Discount,
                        SubTotal = item.SubTotal
                    };
                    context.SaleItems.Add(saleItem);

                    // Actualizar stock en DB
                    var dbProduct = context.Products.Find(item.ProductId);
                    if (dbProduct != null)
                    {
                        dbProduct.Stock -= item.Quantity;
                    }
                }

                context.SaveChanges();

                CurrentTicket.Clear();
                RecalculateTotal();
                CashReceivedText = "";
                RequestSearchFocus?.Invoke();
                return sale;
            }
        }

        [RelayCommand]
        private void RequestDiscount(SaleItem item)
        {
            _itemToDiscount = item;
            ProposedPrice = item.CustomPrice;
            IsDiscountDialogVisible = true;
        }

        [RelayCommand]
        private void CloseDiscountDialog()
        {
            IsDiscountDialogVisible = false;
            _itemToDiscount = null;
        }

        public void ApproveDiscount(string pin)
        {
            var admin = _context.Users.FirstOrDefault(u => u.Role == "admin");
            if (admin != null && BCrypt.Net.BCrypt.Verify(pin, admin.PasswordHash))
            {
                if (_itemToDiscount != null)
                {
                    _itemToDiscount.CustomPrice = ProposedPrice;
                    RecalculateTotal();
                }
                IsDiscountDialogVisible = false;
                _itemToDiscount = null;
            }
            else
            {
                System.Windows.MessageBox.Show("PIN de Administrador incorrecto.", "Acceso Denegado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
