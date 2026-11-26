using NotasMax.Helpers;
using NotasMax.ViewModels;
using System.Globalization;

namespace NotasMax.ContentsViews;

public partial class SimuladoItemContentView : ContentView
{
    public SimuladoItemContentView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not NotaSimulado nota)
            return;

        label_nome_simulado.Text = nota.Bimestre is > 0
            ? $"Simulado {nota.Numero} - {nota.Bimestre}º Bimestre"
            : $"Simulado {nota.Numero}";
        string dataFormatada = nota.Data.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("pt-BR"));
        label_data.Text = dataFormatada;
        label_valor_nota.Text = nota.Nota.ToString("N2");
        label_valor_nota.TextColor = ColorHelper.DefinirCor(nota.Nota);
    }
}