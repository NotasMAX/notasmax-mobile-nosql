using System.Globalization;
using System.Net;
using NotasMax.Converters;
using NotasMax.Exceptions;
using NotasMax.Helpers;
using NotasMax.Models.Simulados;
using NotasMax.Models.Usuarios.Response;
using NotasMax.Services.Settings;
using NotasMax.ViewModels;
using NotasMax.Tests.TestSupport;
using Xunit;

namespace NotasMax.Tests;

[Collection(MauiTestCollection.Name)]
public class UtilityTests
{
    public UtilityTests(MauiTestAppFixture app) => Assert.NotNull(app.Application);

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Ana Maria", "Ana Maria")]
    [InlineData("João Cardoso Silveira", "João S.")]
    public void ReduzirNomeReturnsCurrentDisplayValue(string? input, string expected)
    {
        Assert.Equal(expected, TextoHelper.ReduzirNome(input));
    }

    [Fact]
    public void ReduzirNomeUsesTheRequestedMaximumLength()
    {
        Assert.Equal("Maria A.", TextoHelper.ReduzirNome("Maria Aparecida", 8));
        Assert.Equal("UmaPalavraExtensa", TextoHelper.ReduzirNome("UmaPalavraExtensa", 4));
    }

    [Theory]
    [InlineData(null, 4, "")]
    [InlineData("texto", 8, "texto")]
    [InlineData("NotasMAX", 5, "Notas...")]
    public void ReduzirTextoReturnsCurrentTruncation(string? input, int maxLength, string expected)
    {
        Assert.Equal(expected, TextoHelper.ReduzirTexto(input, maxLength));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" Ana Maria ", "AM")]
    [InlineData("Marina", "MM")]
    public void PegarIniciaisReturnsFirstAndLastInitials(string? input, string expected)
    {
        Assert.Equal(expected, TextoHelper.PegarIniciais(input));
    }

    [Theory]
    [InlineData(4.99, "#EF4343")]
    [InlineData(5, "#FFBB0F")]
    [InlineData(6.99, "#FFBB0F")]
    [InlineData(7, "#2EB867")]
    public void DefinirCorUsesCurrentGradeBands(double grade, string expected)
    {
        Assert.Equal(Color.FromArgb(expected), ColorHelper.DefinirCor(grade));
    }

    [Fact]
    public void UpperCaseConverterFormatsDateAndPassesThroughOtherValues()
    {
        var converter = new UpperCaseConverter();
        var date = new DateTime(2024, 1, 1);
        var formatted = converter.Convert(date, typeof(string), null!, CultureInfo.GetCultureInfo("pt-BR"));
        var other = new object();

        Assert.Equal("Seg.", formatted);
        Assert.Same(other, converter.Convert(other, typeof(object), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void UpperCaseConverterDoesNotImplementReverseConversion()
    {
        Assert.Throws<NotImplementedException>(() => new UpperCaseConverter().ConvertBack("texto", typeof(string), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ApiExceptionRetainsStatusAndMessage()
    {
        var exception = new ApiException(HttpStatusCode.Unauthorized, "unauthorized");

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("unauthorized", exception.Message);
    }

    [Fact]
    public void GlobalSettingsBuildsItsLocalApiEndpoints()
    {
        var settings = new GlobalSettings();

        Assert.Equal("http://localhost:5000/NotasMax/Auth/login", settings.UsuarioLoginAuthEndpoint);
        Assert.Equal("http://localhost:5000/NotasMax/Simulados", settings.SimuladoEndpoint);
        Assert.Equal("http://localhost:5000/NotasMax/Auth/forgot-mobile", settings.ForgotPasswordEndpoint);
    }

    [Fact]
    public void SettingsServiceRejectsAuthenticationResponseWithoutUser()
    {
        var settings = new SettingsService();

        Assert.Throws<InvalidOperationException>(() => settings.Set(new UserToken { Usuario = null! }));
    }


    [Fact]
    [Trait("Category", "Baseline non-normative")]
    public void ViewModelExampleCurrentlyInitializesDemoChartCollections()
    {
        // Characterizes only the existing demo data initializer; this is not a product requirement.
        var viewModel = new ViewModelExemplo();

        Assert.Equal(5, viewModel.NotaSimulados.Count);
        Assert.Equal(8, viewModel.NotaMaterias.Count);
        Assert.Equal(8, viewModel.NotaAlunos.Count);
        Assert.Equal(3, viewModel.DistribuicaoNotas.Count);
        Assert.Equal(3, viewModel.Turmas.Count);
        Assert.Equal(3, viewModel.MediaTurmaAlunos.Count);
    }

    [Fact]
    public void DisplayModelsExposeFormattedValues()
    {
        var turma = new TurmasByAnoAndProfessor.Turma { Serie = 2 };
        var mediaTurma = new MediaTurmaAlunos { Serie = 3 };
        var resumo = new TurmaResumo { Serie = 1, TotalAlunos = 24 };

        Assert.Equal("2º EM", turma.SerieFormatada);
        Assert.Equal("3º EM", mediaTurma.SerieComposta);
        Assert.Equal("1º EM", resumo.SerieAno);
        Assert.Equal("24 alunos", resumo.TotalAlunosFormatado);
    }

    [Fact]
    public void TurmaModelStoresItsClassFieldsAndFormatsItsSeries()
    {
        var turma = new NotasMax.Models.Turma
        {
            Id = Guid.NewGuid(),
            Serie = 2,
            Ano = 2026,
            Alunos = ["student-1", "student-2"]
        };

        Assert.NotEqual(Guid.Empty, turma.Id);
        Assert.Equal(2, turma.Serie);
        Assert.Equal(2026, turma.Ano);
        Assert.Equal(2, turma.Alunos.Count);
        Assert.Equal("2º EM", turma.SerieComposta);
    }
}
