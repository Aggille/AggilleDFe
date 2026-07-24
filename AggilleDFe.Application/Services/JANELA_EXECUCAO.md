# Janela de Execução (JanelaExecucaoService)

Helper estático (`AggilleDFe.Application/Services/JanelaExecucaoService.cs`)
que decide se o download automático de XMLs de uma empresa pode rodar agora,
com base nos campos `HoraInicial`/`HoraFinal` da entidade `Empresa` (ver
`AggilleDFe.Domain/Entities/EMPRESA.md`).

## Regra

- `PodeExecutar(empresa, horaAtual, execucaoManual)`:
  - Se `execucaoManual == true` → sempre retorna `true` (ignora a janela).
    Usado quando o disparo vem de uma ação manual do usuário (ex.: botão
    "Baixar XMLs" na tela de Empresas).
  - Se `HoraInicial` ou `HoraFinal` estiverem vazios → sempre retorna `true`
    (sem restrição configurada para essa empresa).
  - Se os dois estiverem preenchidos → retorna `true` somente se a hora atual
    estiver dentro do intervalo `[HoraInicial, HoraFinal]`. Suporta janelas
    que cruzam a meia-noite (ex.: `HoraInicial = 22:00`, `HoraFinal = 06:00`
    → válido entre 22:00 e 23:59 OU entre 00:00 e 06:00).

## Uso (quando o Worker implementar o loop de download)

```csharp
if (!JanelaExecucaoService.PodeExecutar(empresa, DateTime.Now, execucaoManual: false))
{
    continue; // pula essa empresa neste ciclo do Worker
}
```

No fluxo manual (endpoint/botão "Baixar XMLs" da tela de Empresas), a mesma
chamada deve passar `execucaoManual: true`, ignorando a janela.

**Status atual**: o `AggilleDFe.Worker` ainda não tem o loop real de download
de XMLs implementado (só o esqueleto gerado pelo template `worker`) — este
helper já está pronto e testável, para ser plugado quando essa funcionalidade
for construída.
