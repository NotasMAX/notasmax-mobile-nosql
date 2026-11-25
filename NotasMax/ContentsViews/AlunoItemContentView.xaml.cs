using NotasMax.Helpers;
using NotasMax.ViewModels;
using NotasMax.Views.Professor;

namespace NotasMax.ContentsViews;

public partial class AlunoItemContentView : ContentView
{
    public AlunoItemContentView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (!(this.BindingContext is DesempenhoAlunoByDisciplina.DesempenhoAluno notaAluno))
            return;

        label_nota_aluno.TextColor = ColorHelper.DefinirCor(notaAluno.Media);
        label_nota_aluno.Text = notaAluno.Media.ToString("N2");
        label_nome_aluno.Text = TextoHelper.ReduzirTexto(notaAluno.Nome, 20);
        label_iniciais_aluno.Text = TextoHelper.PegarIniciais(notaAluno.Nome);
    }

    private async void TurmaItem_Tapped(object sender, TappedEventArgs e)
    {
        if (!(this.BindingContext is DesempenhoAlunoByDisciplina.DesempenhoAluno aluno))
            return;

        await Shell.Current.GoToAsync("DesempenhoAluno", new Dictionary<string, object>
        {
            { "AlunoId", aluno.Id },
            { "AlunoNome", aluno.Nome },
            { "DisciplinaId", aluno.DisciplinaId },
            { "DisciplinaNome", aluno.DisciplinaNome }
        });
    }
}