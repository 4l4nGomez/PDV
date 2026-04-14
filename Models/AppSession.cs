namespace BakeryPOS.Models
{
    public static class AppSession
    {
        public static User CurrentUser { get; set; }
        
        public static bool IsAdmin => CurrentUser?.Role?.ToLower() == "admin";
        public static bool IsCashier => CurrentUser?.Role?.ToLower() == "cajero";
    }
}
