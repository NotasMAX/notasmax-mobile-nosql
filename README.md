# 🎓 NotasMAX - Sistema de Notas Escolares

## Projeto referente ao 4º e 5º semestres

Este projeto tem como objetivo desenvolver uma aplicação para gerenciamento de simulados escolares, permitindo que administradores criem e acompanhem simulados e que alunos visualizem seus resultados e desempenho por matéria. O sistema é projetado para lidar com simulados multidisciplinares, em que cada matéria possui peso e resultado específicos, possibilitando o cálculo automático da nota bimestral de cada aluno.

## Contexto
O projeto foi desenvolvido no âmbito do curso Desenvolvimento de Software Multiplataforma (DSM) da FATEC Jahu, como parte das atividades do Projeto Interdisciplinar. 

## Testes unitários e cobertura mobile

A suíte `NotasMax.Tests` executa no target neutro `net10.0`; ela não inicia emulador nem conecta à API ou ao MongoDB. Os testes usam doubles e dados sintéticos. O gate aprovado exige pelo menos 80% de cobertura global de linhas do código de produção mobile. O relatório Cobertura fica em `TestResults/mobile/`.

Pré-requisitos: .NET SDK 10.0.302 (ou patch posterior do mesmo feature band, conforme `global.json`) e workload .NET MAUI. No Windows, instale o workload com `dotnet workload install maui`.

```powershell
dotnet restore .\NotasMax.Tests\NotasMax.Tests.csproj --locked-mode -p:TargetFramework=net10.0
dotnet test .\NotasMax.Tests\NotasMax.Tests.csproj --framework net10.0 --no-restore --coverlet --coverlet-output-format cobertura --coverlet-threshold 80 --coverlet-threshold-type line --coverlet-threshold-stat total --results-directory .\TestResults\mobile
```

O workflow `Mobile tests and coverage` roda em todo push de branch e em pull request para `main`. O artifact `mobile-coverage` contém o relatório, inclusive quando a suíte falha e já produziu cobertura.
