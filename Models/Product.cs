using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BakeryPOS.Models
{
    public partial class Product : ObservableObject
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        private string _name;
        public string Name 
        { 
            get => _name; 
            set => SetProperty(ref _name, value); 
        }
        
        private string _category;
        public string Category 
        { 
            get => _category; 
            set => SetProperty(ref _category, value); 
        }
        
        [Required]
        private decimal _price;
        public decimal Price 
        { 
            get => _price; 
            set => SetProperty(ref _price, value); 
        }
        
        // Código de producto
        private string _code;
        public string Code 
        { 
            get => _code; 
            set => SetProperty(ref _code, value); 
        }
        
        // Días de disponibilidad
        private string _availableDays;
        public string AvailableDays 
        { 
            get => _availableDays; 
            set => SetProperty(ref _availableDays, value); 
        }
        
        // Inventario en piso de ventas (mostrador)
        [ObservableProperty]
        private int _stock;
    }
}