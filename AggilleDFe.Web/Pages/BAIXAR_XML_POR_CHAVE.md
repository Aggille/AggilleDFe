# Tela de Baixar XML por Chave

# Baixa sob demanda uma NFe específica pela chave de acesso, fora do ciclo normal por NSU

# Conteúdo da tela:

- Um `MudSelect` de Empresa (mesma lista de `GET /api/v1/empresas` usada em
  outras telas) e um `MudTextField` de Chave de Acesso (44 dígitos).
- Botão "Baixar" → `POST /api/v1/empresas/{id}/baixar-xml-por-chave`
  (corpo `{ chave }`) — ver
  `AggilleDFe.Infrastructure/Integrations/BAIXAR_POR_CHAVE.md`.
- Valida a chave no front-end (44 dígitos numéricos) antes de chamar a API,
  mesma regra validada de novo no servidor.
- Usa um `HttpClient` próprio com timeout de 2 min (chamada única à SEFAZ,
  bem mais rápida que o ciclo completo de `BaixarXmls.razor`, mas ainda
  assim uma chamada SOAP externa).
- Mostra o resultado (`ResultadoBaixarPorChaveDto`) num `MudAlert` —
  sucesso (verde) se `Encontrado`, aviso (amarelo) caso contrário.
- Acessível pelo menu lateral ("Baixar por Chave"), atrás da permissão
  `AcessoBaixarPorChave` (ver `AggilleDFe.Domain/Entities/USUARIO.md`).
- Diferente de `BaixarXmls.razor` (ciclo completo por NSU, todas ou uma
  empresa) — aqui é uma chave específica, sem afetar o `UltimoNsu` da
  empresa.
