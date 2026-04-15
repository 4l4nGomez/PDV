using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using BakeryPOS.Models;

namespace BakeryPOS;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // In production use EF migrations. Before applying migrations, create a backup of the DB if it exists.
        try
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BakeryPOS");
            string dbPath = Path.Combine(folder, "bakery_pos.db");

            if (File.Exists(dbPath))
            {
                try
                {
                    string backupPath = Path.Combine(folder, $"bakery_pos_backup_{DateTime.Now:yyyyMMddHHmmss}.db");
                    File.Copy(dbPath, backupPath, overwrite: true);
                }
                catch
                {
                    // If backup fails, continue but log where possible
                }
            }

            using (var dbContext = new AppDbContext())
            {
                // Apply any pending migrations
                dbContext.Database.Migrate();

                // FORZAR CREACIÓN DE TABLA DE CONFIGURACIÓN SI FALLA LA MIGRACIÓN
                try 
                {
                    dbContext.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Configurations (Key TEXT PRIMARY KEY, Value TEXT);");
                }
                catch { /* Ya existe o error menor */ }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying database migrations:\n{ex.Message}\nSe ha intentado crear una copia de respaldo si existía la BD.", "Error de base de datos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

