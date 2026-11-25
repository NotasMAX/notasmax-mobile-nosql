using NotasMax.Models;

namespace NotasMax.ViewModels;

public class MediaTurmaAlunos
{
    public int TurmaId { get; set; }
    public int Serie { get; set; }
    public double Media { get; set; }
    public int QuantidadeAlunos { get; set; } 

    public string SerieComposta { get
        {
            return Serie + "º EM";
        }
    }
}

public class NotaSimulado
{
    public string? Numero { get; set; }
    public int? Bimestre { get; set; }
    public string IdentificacaoGrafico => Bimestre is > 0
        ? $"{Bimestre}ºB/S{Numero}"
        : $"S{Numero} ({Data:dd/MM})";
    public double Nota { get; set; }
    public DateTime Data { get; set; }
}

public class NotaMateria
{
    public string? Materia { get; set; }
    public double Nota { get; set; }
}

public class NotaAluno
{
    public Guid AlunoId { get; set; }
    public string? Nome { get; set; }
    public double Nota { get; set; }
}

public class DistribuicaoNotas
{
    public string? Faixa { get; set; }
    public int Quantidade { get; set; }
}

public class ViewModelExemplo
{
    public List<NotaSimulado> NotaSimulados { get; set; }
    public List<NotaMateria> NotaMaterias { get; set; }
    public List<NotaAluno> NotaAlunos { get; set; }
    public List<DistribuicaoNotas> DistribuicaoNotas { get; set; }
    public List<MediaTurmaAlunos> MediaTurmaAlunos { get; set; }
    public List<Turma> Turmas { get; set; }

    public ViewModelExemplo()
    {
        // Gerando alguns dados de exemplo (Mock / Simulação) para o gráfico de desempenho
        NotaSimulados = new List<NotaSimulado>
        {
            new NotaSimulado { Numero = "1", Nota = 6.54, Data = new DateTime(2025, 2, 10) },
            new NotaSimulado { Numero = "2", Nota = 7.45, Data = new DateTime(2025, 3, 1)  },
            new NotaSimulado { Numero = "3", Nota = 6.30, Data = new DateTime(2025, 4, 5)  },
            new NotaSimulado { Numero = "4", Nota = 8.56, Data = new DateTime(2025, 5, 12) },
            new NotaSimulado { Numero = "5", Nota = 9.14, Data = new DateTime(2025, 6, 20) }
        };

        NotaMaterias = new List<NotaMateria>
        {
            new NotaMateria { Materia = "Matemática", Nota = 7.5 },
            new NotaMateria { Materia = "Português", Nota = 8.0 },
            new NotaMateria { Materia = "Química", Nota = 6.5 },
            new NotaMateria { Materia = "História", Nota = 7.0 },
            new NotaMateria { Materia = "Geografia", Nota = 8.5 },
            new NotaMateria { Materia = "Inglês", Nota = 3.5 },
            new NotaMateria { Materia = "Física", Nota = 8.0 },
            new NotaMateria { Materia = "Biologia", Nota = 6.5 }
        };

        NotaAlunos = new List<NotaAluno>
        {
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "João Cardoso Silveira Alencar", Nota = 7.5 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Maria Alencar", Nota = 8.0 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Pedro Oliveira", Nota = 6.5 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Ana Carolina", Nota = 7.0 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Lucas Silveira", Nota = 8.5 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Sofia Veronez", Nota = 3.5 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Rafael Souza", Nota = 8.0 },
            new NotaAluno {AlunoId= Guid.NewGuid(), Nome = "Isabela Lombardi", Nota = 6.5 }
        };

        DistribuicaoNotas = new List<DistribuicaoNotas>
        {
            new DistribuicaoNotas { Faixa = "5-7", Quantidade = 2 },
            new DistribuicaoNotas { Faixa = "<5", Quantidade = 1 },
            new DistribuicaoNotas { Faixa = ">7", Quantidade = 5 },
        };

        Turmas = new List<Turma>
        {
            new Turma {Serie = 1, Id= Guid.NewGuid()},
            new Turma {Serie = 2, Id= Guid.NewGuid()},
            new Turma {Serie = 3, Id= Guid.NewGuid()}
        };

        MediaTurmaAlunos = new List<MediaTurmaAlunos>
        {
            new MediaTurmaAlunos { Serie = 1, Media = 6.5, QuantidadeAlunos = 30 },
            new MediaTurmaAlunos { Serie = 2, Media = 3.5, QuantidadeAlunos = 28 },
            new MediaTurmaAlunos { Serie = 3, Media = 8.2, QuantidadeAlunos = 32 }
        };

    }
}
