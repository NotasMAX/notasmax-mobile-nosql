using NotasMax.Services.NavigationService;
using NotasMax.Services.Settings;
using NotasMax.Views;
using NotasMax.Views.Aluno;
using NotasMax.Views.Professor;
using NotasMax.Views.EsqueceuSenha;

namespace NotasMax
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;
        private readonly ISettingsService _settingsService;

        public AppShell(INavigationService navigationService, ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _settingsService = settingsService;
            InitializeRouting();
            InitializeComponent();
            UpdateBottomMenu();
        }

        protected override async void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler is not null)
            {
                await _navigationService.Initializeasync();
            }
        }

        public void UpdateBottomMenu()
        {
            var role = _settingsService.UserRoleKey.Trim().ToLowerInvariant();

            aluno_tabbar.IsVisible = role == "aluno";
            professor_tabbar.IsVisible = role == "professor";
        }

        // Método para registrar rotas de Navegação.
        public static void InitializeRouting()
        {
            Routing.RegisterRoute("Login", typeof(LoginView));
            Routing.RegisterRoute("HomeAluno", typeof(HomeAlunoView));
            Routing.RegisterRoute("HomeProfessor", typeof(HomeProfessorView));
            Routing.RegisterRoute("DesempenhoTurma", typeof(DesempenhoTurmaView));
            Routing.RegisterRoute("Turmas", typeof(TurmasView));
            Routing.RegisterRoute("MeuDesempenho", typeof(MeuDesempenhoView));
            Routing.RegisterRoute("Simulado", typeof(ExibirSimuladoView));
            Routing.RegisterRoute("CalendarioAluno", typeof(CalendarioAlunoView));
            Routing.RegisterRoute("DesempenhoAluno", typeof(DesempenhoAlunoView));
            Routing.RegisterRoute("Materia", typeof(ExibirMateriaView));
            Routing.RegisterRoute("MinhasMaterias", typeof(MinhasMateriasView));
            Routing.RegisterRoute("DadosPessoaisAluno", typeof(DadosPessoaisAlunoView));
            Routing.RegisterRoute("LerQrCode", typeof(LerQrCodeView));
            Routing.RegisterRoute("informaremail", typeof(InformarEmailView));
            Routing.RegisterRoute("informarcodigo", typeof(InformarCodigoRecuperacaoView));
            Routing.RegisterRoute("novasenha", typeof(NovaSenhaView));
        }

        protected override void OnNavigating(ShellNavigatingEventArgs args)
        {
            base.OnNavigating(args);

            if (args.Source == ShellNavigationSource.ShellSectionChanged || args.Source == ShellNavigationSource.ShellItemChanged)
            {
                var navigation = Shell.Current?.Navigation;
                if (navigation != null)
                {
                    var stack = navigation.NavigationStack?.ToArray();
                    if (stack != null && stack.Length > 1)
                    {
                        var pagesToRemove = stack.Skip(1).ToList();

                        Dispatcher.Dispatch(() =>
                        {
                            foreach (var page in pagesToRemove)
                            {
                                try
                                {
                                    navigation.RemovePage(page);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Erro ao limpar pilha de navegação: {ex.Message}");
                                }
                            }
                        });
                    }
                }
            }
        }
    }
}
