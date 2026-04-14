using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        public string PasswordHash { get; set; }
        
        // admin, cajero, panadero
        public string Role { get; set; } 
    }
}
