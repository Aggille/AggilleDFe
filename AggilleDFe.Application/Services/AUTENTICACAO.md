# Autenticação (AutenticacaoService / IAutenticacaoService)

Serviço usado só por `POST /api/v1/auth/login`
(`AggilleDFe.API/Endpoints/AutenticacaoEndpoints.cs`). Recebe
`LoginRequestDto` (Login/Senha), busca o `Usuario` pelo login
(`IUsuarioRepository.ObterPorLoginAsync`), recusa se não existir, estiver
`Inativo = 'S'` ou a senha não bater com `SenhaHash`
(`PasswordHasher<Usuario>.VerifyHashedPassword`, mesmo hasher usado por
`UsuarioService` pra gravar a senha).

Se autenticar, monta um JWT assinado (HMAC-SHA256, `System.IdentityModel.Tokens.Jwt`)
com claims:

- `ClaimTypes.NameIdentifier` = Id do usuário
- `ClaimTypes.Name` = Login
- `administrador` = `"true"`/`"false"`
- `permissao` (claim repetida, uma por módulo liberado) — valores
  `xmls-baixados`, `registros`, `empresas`, `configuracao`, `importacao`,
  `baixar-xml`

Chave de assinatura e tempo de expiração vêm de `JwtOptions`
(`appsettings.json:Jwt:Key` / `Jwt:ExpiraMinutos`, padrão 600 min).

O token retornado (`LoginResponseDto.Token`) é decodificado pelo
`TokenAuthenticationStateProvider` do Blazor Web pra virar um `ClaimsPrincipal`
local — a API **não** valida esse token nos demais endpoints ainda (ver
`AggilleDFe.Domain/Entities/USUARIO.md`); o gate de acesso hoje é só no
front-end.
