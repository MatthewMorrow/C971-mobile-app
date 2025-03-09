using Microsoft.Maui.Controls;
using System;
using C971.Data;
using C971.Views;

namespace C971
{
    public partial class App : Application
    {
        private readonly AppDatabase db;

        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Dark;
            db = MauiProgram.ServiceProvider?.GetService<AppDatabase>()
                 ?? throw new InvalidOperationException("AppDatabase service not available.");
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new TermsPage(db)));
        }
    }
}