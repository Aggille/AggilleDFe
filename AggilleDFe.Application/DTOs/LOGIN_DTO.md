# DTOs de Login (LoginRequestDto / LoginResponseDto)

Contrato de `POST /api/v1/auth/login`, o único endpoint de autenticação —
não exige token (é ele quem emite o token). Consumido pela tela
`AggilleDFe.Web/Pages/Login.razor`.

## LoginRequestDto

- Login: string
- Senha: string

## LoginResponseDto

- Token: string — JWT assinado (`AutenticacaoService`, chave/tempo de
  expiração em `JwtOptions`/`appsettings.json:Jwt`), guardado pelo Blazor no
  `sessionStorage` (não `localStorage` — fechar o navegador/aba deve
  deslogar) e reaproveitado como claims pelo
  `TokenAuthenticationStateProvider`
- ExpiraEm: DateTime — usado pelo front pra saber quando descartar o token
  guardado e voltar pro login
- Login, Nome
- Administrador, AcessoXmlsBaixados, AcessoRegistros, AcessoEmpresas,
  AcessoConfiguracao, AcessoImportacao, AcessoBaixarXml: bool — repetidos
  como claims no token (claim `administrador` e um claim `permissao` por
  módulo liberado), é o que o `NavMenu`/`[Authorize(Policy=...)]` usa pra
  decidir o que mostrar/bloquear

Por enquanto só o front-end (Blazor) exige esse token — os demais endpoints
internos da API ainda respondem sem autenticação (ver
`AggilleDFe.Domain/Entities/USUARIO.md`, seção Observações). O
`TokenAuthorizationHandler` do Web já manda `Authorization: Bearer <token>`
em toda chamada, deixando a API pronta pra passar a exigir isso no futuro
sem precisar mexer no front.
