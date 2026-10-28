using NotasMax.Models;
using NotasMax.Models.Simulados;
using NotasMax.Models.Usuarios;
using NotasMax.Models.Usuarios.Response;
using NotasMax.Services.NavigationService;
using NotasMax.Services.RequestProvider;
using NotasMax.Services.Settings;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.ViewModels;
using Xunit;

namespace NotasMax.Tests.TestSupport;

[CollectionDefinition("MAUI application fixture", DisableParallelization = true)]
public sealed class MauiTestCollection : ICollectionFixture<MauiTestAppFixture>
{
    public const string Name = "MAUI application fixture";
}

public sealed class MauiTestAppFixture
{
    public App Application { get; }

    public MauiTestAppFixture()
    {
        Application = new App(new FakeNavigationService(), new FakeSettingsService());
    }
}

public sealed class FakeSettingsService : ISettingsService
{
    public string AuthAccessToken { get; set; } = "synthetic-token";
    public string UserIdKey { get; set; } = "student-1";
    public string UserNameKey { get; set; } = "Test Student";
    public string UserRoleKey { get; set; } = "aluno";
    public string UserEmailKey { get; set; } = "student@example.invalid";
    public string UserNomeResponsavelKey { get; set; } = string.Empty;
    public string UserTelefoneResponsavelKey { get; set; } = string.Empty;

    public void Set(UserToken userToken)
    {
        AuthAccessToken = userToken.Token ?? string.Empty;
        UserIdKey = userToken.Usuario?.Id ?? string.Empty;
        UserNameKey = userToken.Usuario?.Nome ?? string.Empty;
        UserRoleKey = userToken.Usuario?.TipoUsuario ?? string.Empty;
        UserEmailKey = userToken.Usuario?.Email ?? string.Empty;
        UserNomeResponsavelKey = userToken.Usuario?.NomeResponsavel ?? string.Empty;
        UserTelefoneResponsavelKey = userToken.Usuario?.TelefoneResponsavel ?? string.Empty;
    }
}

public sealed class FakeNavigationService : INavigationService
{
    public List<(string Route, bool Absolute)> Navigations { get; } = new();

    public Task Initializeasync() => Task.CompletedTask;

    public Task NavigationAsync(string route, bool absolute)
    {
        Navigations.Add((route, absolute));
        return Task.CompletedTask;
    }
}

public sealed class FakeUsuarioService : IUsuarioService
{
    public Usuario User { get; set; } = new();
    public UserToken TokenResponse { get; set; } = new();
    public ForgotPasswordResponse ForgotPasswordResponse { get; set; } = new();
    public ResetPasswordResponse ResetPasswordResponse { get; set; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public Task<Usuario> Login(Usuario usuario) => Return(User);
    public Task<UserToken> Authenticate(UsuarioAuth auth) => Return(TokenResponse);
    public Task<Usuario> GetUsuarioById(string id, string token) => Return(User);
    public Task<ForgotPasswordResponse> ForgotPassword(string email) => Return(ForgotPasswordResponse);
    public Task<ResetPasswordResponse> ResetPassword(string token, string novaSenha) => Return(ResetPasswordResponse);

    private Task<T> Return<T>(T value)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;
        return Task.FromResult(value);
    }
}

public sealed class FakeSimuladoService : ISimuladoService
{
    public SimuladoAlunoResponse ByAluno { get; set; } = new();
    public SimuladoProfessorResponse ByProfessor { get; set; } = new();
    public DesempenhoAlunoByDisciplina DesempenhoByDisciplina { get; set; } = new();
    public DesempenhoAluno DesempenhoAluno { get; set; } = new();
    public CalendarioByAluno Calendario { get; set; } = new();
    public DesempenhoAlunoBySimulado DesempenhoBySimulado { get; set; } = new();
    public DesempenhoAlunoBySimulados SimuladosAluno { get; set; } = new();
    public TurmasByAnoAndProfessor TurmasByProfessor { get; set; } = new();
    public GestaoNotasViewModel GestaoNotas { get; set; } = new();
    public GestaoNotasSimuladoViewModel GestaoNotasSimulado { get; set; } = new();
    public Exception? ExceptionToThrow { get; set; }

    public List<(string Method, string Id, string Token)> Calls { get; } = new();
    public List<(string SimuladoId, string DisciplinaId, List<GestaoNotasSimuladoViewModel.Aluno> Alunos)> SavedGrades { get; } = new();

    public Task SaveGestaoNotas(string simuladoId, string disciplinaId, List<GestaoNotasSimuladoViewModel.Aluno> alunos)
    {
        ThrowIfNeeded();
        SavedGrades.Add((simuladoId, disciplinaId, alunos));
        return Task.CompletedTask;
    }

    public Task<DesempenhoAlunoByDisciplina> getDesempenhoByDisciplina(string disciplinaId) => Return("getDesempenhoByDisciplina", disciplinaId, string.Empty, DesempenhoByDisciplina);
    public Task<DesempenhoAluno> GetDesempenhoAluno() => Return("GetDesempenhoAluno", string.Empty, string.Empty, DesempenhoAluno);
    public Task<CalendarioByAluno> GetCalendarioByAluno() => Return("GetCalendarioByAluno", string.Empty, string.Empty, Calendario);
    public Task<DesempenhoAlunoBySimulado> GetDesempenhoAlunoBySimulado(string simuladoId) => Return("GetDesempenhoAlunoBySimulado", simuladoId, string.Empty, DesempenhoBySimulado);
    public Task<DesempenhoAlunoBySimulados> GetSimuladosAluno() => Return("GetSimuladosAluno", string.Empty, string.Empty, SimuladosAluno);
    public Task<TurmasByAnoAndProfessor> GetByAnoAndProfessor() => Return("GetByAnoAndProfessor", string.Empty, string.Empty, TurmasByProfessor);
    public Task<DesempenhoAlunoBySimulados> GetSimuladosByAluno(string alunoId) => Return("GetSimuladosByAluno", alunoId, string.Empty, SimuladosAluno);
    public Task<SimuladoAlunoResponse> GetByAluno(string aluno_id, string token) => Return("GetByAluno", aluno_id, token, ByAluno);
    public Task<SimuladoProfessorResponse> GetByProfessor(string professor_id, string token) => Return("GetByProfessor", professor_id, token, ByProfessor);
    public Task<GestaoNotasViewModel> GetGestaoNotas() => Return("GetGestaoNotas", string.Empty, string.Empty, GestaoNotas);
    public Task<GestaoNotasSimuladoViewModel> GetGestaoNotasSimulado(string simuladoId, string disciplinaId) => Return("GetGestaoNotasSimulado", $"{simuladoId}/{disciplinaId}", string.Empty, GestaoNotasSimulado);

    private Task<T> Return<T>(string method, string id, string token, T response)
    {
        ThrowIfNeeded();
        Calls.Add((method, id, token));
        return Task.FromResult(response);
    }

    private void ThrowIfNeeded()
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;
    }
}

public sealed class RecordingRequestProvider : IRequestProvider
{
    public object? Response { get; set; }
    public List<RequestCall> Calls { get; } = new();

    public Task<TResult> GetAsync<TResult>(string uri, string token = "")
    {
        Calls.Add(new RequestCall("GET", uri, token, null));
        return Task.FromResult((TResult)Response!);
    }

    public Task<TResult> PostAsync<TResult, TSend>(string uri, TSend data, string token = "")
    {
        Calls.Add(new RequestCall("POST", uri, token, data));
        return Task.FromResult((TResult)Response!);
    }
}

public sealed record RequestCall(string Method, string Uri, string Token, object? Payload);
