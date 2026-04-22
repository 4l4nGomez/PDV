using System;
using System.IO;

namespace BakeryPOS.Models
{
    public static class Settings
    {
        private static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BakeryPOS");

        public static string DatabasePath
        {
            get
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                return Path.Combine(AppFolder, "bakery_pos.db");
            }
        }

        public static string BackupFolderPath
        {
            get
            {
                var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var backupFolder = Path.Combine(myDocuments, "BakeryPOS_Backups", "Automaticas");
                if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
                return backupFolder;
            }
        }

        public static string LogFilePath
        {
            get
            {
                if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
                return Path.Combine(AppFolder, "bakerypos.log");
            }
        }
    }
}