# DTO de Usuário (UsuarioDto)

Contrato usado pela API (`GET /api/v1/usuarios`, `GET /api/v1/usuarios/{id}`,
`POST /api/v1/usuarios`, `PUT /api/v1/usuarios/{id}`, `DELETE /api/v1/usuarios/{id}`)
e pela tela de Usuários (grid + `UsuarioDialog` nos modos Incluir/Alterar),
visível só pra quem tem `Administrador = true`.

- Id: int — 0 ao incluir
- Login: string — obrigatório, único, máx. 50 caracteres
- Nome: string?
- Senha: string? — obrigatória ao incluir; em branco na alteração mantém a
  senha atual. Nunca é devolvida pela API (o DO só guarda `SenhaHash`, essa
  propriedade é só de entrada)
- Administrador: bool — controla só o acesso à tela de Usuários, é
  independente das 7 permissões de módulo abaixo
- AcessoXmlsBaixados, AcessoRegistros, AcessoEmpresas, AcessoConfiguracao,
  AcessoImportacao, AcessoBaixarXml, AcessoExportarXmls, AcessoBaixarPorChave: bool
  — um por item de menu da tela (ver `AggilleDFe.Web/Layout/NavMenu.razor`);
  controlam o que aparece no menu e quais rotas o usuário pode abrir no Blazor.
  AcessoExportarXmls é da tela "Exportar XMLs" (zip de NFe/CTe do período,
  ver `AggilleDFe.Infrastructure/Integrations/EXPORTACAO_XMLS.md`) —
  separada de AcessoBaixarXml (download via SEFAZ) e AcessoXmlsBaixados
  (grid de visualização). AcessoBaixarPorChave é da tela "Baixar por Chave"
  (baixa uma NFe específica sob demanda, ver
  `AggilleDFe.Infrastructure/Integrations/BAIXAR_POR_CHAVE.md`)
- Inativo: bool — mesmo padrão de `EmpresaDto.Inativo`, desativa sem excluir

Ver também `AggilleDFe.Domain/Entities/USUARIO.md` (DO) e
`AggilleDFe.Application/Services/AUTENTICACAO.md` (login/JWT).
