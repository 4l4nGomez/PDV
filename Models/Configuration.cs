using System;
using System.ComponentModel.DataAnnotations;

namespace BakeryPOS.Models
{
    public class Configuration
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
