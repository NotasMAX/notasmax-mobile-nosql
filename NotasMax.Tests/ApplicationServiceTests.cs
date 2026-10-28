using System.Text.Json;
using NotasMax.Models.Simulados;
using NotasMax.Models.Usuarios;
using NotasMax.Models.Usuarios.Response;
using NotasMax.Services.Simulados;
using NotasMax.Services.Usuarios;
using NotasMax.Tests.TestSupport;
using NotasMax.ViewModels;
using Xunit;

namespace NotasMax.Tests;

public class ApplicationServiceTests
{
    [Fact]
    public async Task UsuarioServiceMapsEachOperationToItsExistingEndpointAndPayload()
    {
        var requests = new RecordingRequestProvider();
        var service = new UsuarioService(requests);
        var user = new Usuario { Email = "synthetic@example.invalid" };

        requests.Response = user;
        Assert.Same(user, await service.Login(user));
        AssertCall(requests.Calls[^1], "POST", GlobalSettings.Instance.UsuarioLoginAuthEndpoint, user);

        var auth = new UsuarioAuth { Email = user.Email, Senha = "synthetic-password" };
        var token = new UserToken { Token = "synthetic-token", Usuario = user };
        requests.Response = token;
        Assert.Same(token, await service.Authenticate(auth));
        AssertCall(requests.Calls[^1], "POST", GlobalSettings.Instance.UsuarioLoginAuthEndpoint, auth);

        requests.Response = user;
        Assert.Same(user, await service.GetUsuarioById("student-7", "synthetic-token"));
        AssertCall(requests.Calls[^1], "GET", $"{GlobalSettings.Instance.UsuarioEndPonint}/Detalhes/student-7", null, "synthetic-token");

        var forgot = new ForgotPasswordResponse { Message = "synthetic response" };
        requests.Response = forgot;
        Assert.Same(forgot, await service.ForgotPassword(user.Email!));
        AssertCall(requests.Calls[^1], "POST", GlobalSettings.Instance.ForgotPasswordEndpoint, new ForgotPasswordRequest { Email = user.Email });

        var reset = new ResetPasswordResponse { Message = "synthetic response" };
        requests.Response = reset;
        Assert.Same(reset, await service.ResetPassword("synthetic-reset-token", "synthetic-new-password"));
        AssertCall(requests.Calls[^1], "POST", GlobalSettings.Instance.ResetPasswordEndpoint, new ResetPasswordRequest { Token = "synthetic-reset-token", NovaSenha = "synthetic-new-password" });
    }

    [Fact]
    public async Task SimuladoServiceMapsAllReadAndSaveOperationsToCurrentApiUris()
    {
        var requests = new RecordingRequestProvider();
        var settings = new FakeSettingsService { UserIdKey = "student-7", UserNameKey = "Test Teacher", UserRoleKey = "professor" };
        var service = new SimuladoService(requests, settings);
        var year = DateTime.UtcNow.Year;

        var aluno = new SimuladoAlunoResponse();
        requests.Response = aluno;
        Assert.Same(aluno, await service.GetByAluno("student-7", "synthetic-token"));

        var professor = new SimuladoProfessorResponse();
        requests.Response = professor;
        Assert.Same(professor, await service.GetByProfessor("teacher-2", "synthetic-token"));

        var byDiscipline = new DesempenhoAlunoByDisciplina();
        requests.Response = byDiscipline;
        Assert.Same(byDiscipline, await service.getDesempenhoByDisciplina("subject-3"));

        var performance = new DesempenhoAluno();
        requests.Response = performance;
        Assert.Same(performance, await service.GetDesempenhoAluno());

        var management = new GestaoNotasViewModel();
        requests.Response = management;
        Assert.Same(management, await service.GetGestaoNotas());

        var bySimulation = new DesempenhoAlunoBySimulado();
        requests.Response = bySimulation;
        Assert.Same(bySimulation, await service.GetDesempenhoAlunoBySimulado("simulation-4"));

        var simulations = new DesempenhoAlunoBySimulados();
        requests.Response = simulations;
        Assert.Same(simulations, await service.GetSimuladosAluno());
        Assert.Same(simulations, await service.GetSimuladosByAluno("student-8"));

        var turmas = new TurmasByAnoAndProfessor();
        requests.Response = turmas;
        Assert.Same(turmas, await service.GetByAnoAndProfessor());

        var calendar = new CalendarioByAluno();
        requests.Response = calendar;
        Assert.Same(calendar, await service.GetCalendarioByAluno());

        var alunos = new List<GestaoNotasSimuladoViewModel.Aluno> { new() { Id = "student-7", Acertos = 3 } };
        requests.Response = new object();
        await service.SaveGestaoNotas("simulation-9", "subject-3", alunos);

        var managementDetail = new GestaoNotasSimuladoViewModel();
        requests.Response = managementDetail;
        Assert.Same(managementDetail, await service.GetGestaoNotasSimulado("simulation-9", "subject-3"));

        Assert.Collection(requests.Calls,
            call => AssertCall(call, "GET", $"{GlobalSettings.Instance.SimuladoEndpoint}/getByAluno/student-7", null, "synthetic-token"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Simulados/getByProfessor/teacher-2", null, "synthetic-token"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Simulado/Disciplina/id=subject-3"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Desempenho/ano={year}/aluno=student-7"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Professor/student-7/data/anoAtual"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Desempenho/aluno=student-7/simulado=simulation-4"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Simulados/ano={year}/aluno=student-7"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Simulados/ano={year}/aluno=student-8"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Turmas/ano={year}/professor=student-7"),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Calendario/aluno=student-7"),
            call => AssertCall(call, "POST", $"{GlobalSettings.DefaultEndpoint}/Simulado/simulation-9/Disciplina/subject-3/SalvarNotas", alunos),
            call => AssertCall(call, "GET", $"{GlobalSettings.DefaultEndpoint}/Simulado/simulation-9/Disciplina/subject-3"));
    }

    private static void AssertCall(RequestCall call, string method, string uri, object? payload = null, string token = "")
    {
        Assert.Equal(method, call.Method);
        Assert.Equal(uri, call.Uri);
        Assert.Equal(token, call.Token);
        if (payload is null)
            Assert.Null(call.Payload);
        else
            Assert.Equal(JsonSerializer.Serialize(payload), JsonSerializer.Serialize(call.Payload));
    }
}
