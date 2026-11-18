using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using BarcodeScanning;
using NotasMax.Services.NavigationService;
using NotasMax.Services.Presencas;
using NotasMax.Services.RequestProvider;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.Views;
using NotasMax.Views.Aluno;
using NotasMax.Views.EsqueceuSenha;
using NotasMax.Views.Professor;
using NotasMax.ViewModels;
using Syncfusion.Maui.Toolkit.Hosting;

namespace NotasMax
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeScanning()
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Inter.ttf", "Inter");
                    fonts.AddFont("Roboto.ttf", "Roboto");
                    fonts.AddFont("SpaceGrotesk.ttf", "SpaceGrotesk");
                     fonts.AddFont("RobotoMono.ttf", "RobotoMono");
                })
                .RegisterAppServices()
                .RegisterViews();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Configuração personalizada para remover bordas de Entry no Windows
            builder.ConfigureMauiHandlers(handlers =>
            {
                EntryHandler.Mapper.AppendToMapping("BorderlessEntry", (handler, view) =>
                {
#if WINDOWS
                    var nativeView = handler.PlatformView;
                    nativeView.Loaded += (s, e) =>
                    {
                        nativeView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                        nativeView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.Colors.Transparent);
                        nativeView.FocusVisualPrimaryThickness   = new Microsoft.UI.Xaml.Thickness(0);
                        nativeView.FocusVisualSecondaryThickness = new Microsoft.UI.Xaml.Thickness(0);
                    };
#endif
                });
            });
            return builder.Build();
        }

        // Registrar os serviços da aplicação
        public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder app)
        {
            app.Services.AddSingleton<IRequestProvider, RequestProvider>();
            app.Services.AddSingleton<IUsuarioService, UsuarioService>();
            app.Services.AddSingleton<IPresencaService, PresencaService>();
            app.Services.AddSingleton<ISettingsService, SettingsService>();
            app.Services.AddSingleton<INavigationService, NavigationService>();
            app.Services.AddSingleton<ISimuladoService, SimuladoService>();
            return app;
        }

        // Registrar as Views que usarão os serviços da aplicação
        public static MauiAppBuilder RegisterViews(this MauiAppBuilder app)
        {
            app.Services.AddSingleton<LoginView>();
            app.Services.AddSingleton<HomeAlunoView>();
            app.Services.AddSingleton<HomeProfessorView>();
            app.Services.AddSingleton<TurmasView>();
            app.Services.AddSingleton<DesempenhoTurmaView>();
            app.Services.AddSingleton<PerfilView>();
            app.Services.AddSingleton<PerfilProfessorView>();
            app.Services.AddTransient<MinhasMateriasView>();
            app.Services.AddTransient<LerQrCodeViewModel>();
            app.Services.AddTransient<LerQrCodeView>();
            app.Services.AddTransient<DesempenhoAlunoView>();
            app.Services.AddTransient<DadosPessoaisAlunoView>();
            app.Services.AddTransient<InformarEmailView>();
            app.Services.AddTransient<InformarCodigoRecuperacaoView>();
            app.Services.AddTransient<NovaSenhaView>();
            app.Services.AddTransient<ExibirMateriaView>();
            return app;
        }

    }
}
