using System.Configuration;
using System.Data;
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

        // Run migrations once on application startup
        using (var dbContext = new AppDbContext())
        {
            dbContext.Database.Migrate();
        }
    }
}

