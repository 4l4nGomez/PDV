using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BakeryPOS.Models
{
    public partial class SaleItem : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        
        public int SaleId { get; set; }
        public Sale Sale { get; set; }
        
        public int ProductId { get; set; }
        public Product Product { get; set; }
        
        private int _quantity;
        public int Quantity 
        { 
            get => _quantity; 
            set 
            {
                if (SetProperty(ref _quantity, value))
                {
                    RecalculateSubTotal();
                }
            }
        }

        [ObservableProperty]
        private decimal _unitPrice;

        [ObservableProperty]
        private decimal _discount;

        [NotMapped]
        public decimal CustomPrice
        {
            get => UnitPrice - Discount;
            set
            {
                Discount = UnitPrice - value;
                OnPropertyChanged(nameof(CustomPrice));
            }
        }

        [ObservableProperty]
        private decimal _subTotal;

        partial void OnUnitPriceChanged(decimal value)
        {
            OnPropertyChanged(nameof(CustomPrice));
            RecalculateSubTotal();
        }

        partial void OnDiscountChanged(decimal value)
        {
            OnPropertyChanged(nameof(CustomPrice));
            RecalculateSubTotal();
        }

        private void RecalculateSubTotal()
        {
            SubTotal = Quantity * CustomPrice < 0 ? 0 : Quantity * CustomPrice;
        }
    }
}
