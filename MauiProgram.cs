using Microsoft.Extensions.Logging;
using C971.Data;
using Plugin.LocalNotification;
using Plugin.LocalNotification.iOSOption;

namespace C971
{
    public static class MauiProgram
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .UseLocalNotification(config =>
                {
                    config.AddCategory(new NotificationCategory(NotificationCategoryType.Status)
                    {
                        ActionList =
                        [
                            ..new List<NotificationAction>
                            {
                                new NotificationAction(100)
                                {
                                    Title = "Open App",
                                    Android =
                                    {
                                        LaunchAppWhenTapped = true
                                    },
                                    IOS =
                                    {
                                        Action = iOSActionType.Foreground
                                    }
                                },
                                new NotificationAction(101)
                                {
                                    Title = "Dismiss",
                                    Android =
                                    {
                                        LaunchAppWhenTapped = false
                                    },
                                    IOS =
                                    {
                                        Action = iOSActionType.Destructive
                                    }
                                }
                            }
                        ]
                    });
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<AppDatabase>();

            var app = builder.Build();
            ServiceProvider = app.Services;
            return app;
        }
    }
}
