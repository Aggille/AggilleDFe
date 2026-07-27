# Tela de Empresas

# Tela que exibe as empresas cadastradas e permite o CRUD das entidades

# Classe D.O. EMPRESA

# Conteúdo da tela:

- Um Datagrid com os seguintes campos:
  - Razão Social
  - CNPJ ( formatado )
  - UF
  - ( "Cidade" removida do grid — não existe campo correspondente no DO Empresa )
  - Ícones de ação:
    - Consultar — abre `EmpresaDialog` (Dialogs/EmpresaDialog.razor) em modo somente-leitura
    - Alterar — abre `EmpresaDialog` em modo edição, salva via `PUT /api/v1/empresas/{id}`
    - Excluir — confirmação via `ShowMessageBoxAsync`, exclui via `DELETE /api/v1/empresas/{id}`
    - Baixar XMLs — dispara `POST /api/v1/empresas/{id}/baixar-xmls`, execução
      **manual e síncrona** (a chamada aguarda o download terminar; timeout do
      `HttpClient` elevado para 15 min só nessa chamada) da Distribuição DFe
      de NFe e CTe via `DistribuicaoDfeService` (ver
      `AggilleDFe.Infrastructure/Integrations/DISTRIBUICAO_DFE.md`), a partir
      do último NSU salvo na empresa; mostra o resumo (quantidade de XMLs
      baixados) em um Snackbar. O loop automático pelo `Worker` (respeitando
      Hora Inicial/Hora Final) ainda não foi implementado
    - Verificar Status do SEFAZ — consulta `GET
      /api/v1/empresas/{id}/status-sefaz`, que usa o Zeus DFe.NET
      (`ZeusConfiguracaoFactory` + `NFe.Servicos.ServicosNFe.NfeStatusServico`,
      ver `AggilleDFe.Infrastructure/Integrations/ZEUS_CONFIGURACAO.md`) para
      consultar o status do serviço da SEFAZ na UF da empresa; mostra o
      resultado (cStat/xMotivo) em um Snackbar

  - Um header com campo de pesquisa (480px) e botão de pesquisa, que pesquisa pelo nome e cnpj (`GET /api/v1/empresas?busca=`), e outro botão para incluir uma nova empresa

# Diálogo de Empresa (Incluir / Alterar / Consultar)

Um único componente (`EmpresaDialog.razor`) cobre os 3 modos, controlado pelo
parâmetro `Modo` (enum `ModoDialogoEmpresa`). Aberto com `DialogOptions {
MaxWidth = Large, FullWidth = true }`, conteúdo em um `<div>` de tamanho fixo
(100% da largura do dialog, altura 434px) para não redimensionar ao trocar de
aba. Campos organizados em grid (`MudGrid`/`MudItem`) dentro de abas
(`MudTabs`), cobrindo todos os campos do DO `Empresa`:

- **Dados Gerais**: C.N.P.J. — só editável no modo Incluir (`MudMask` com
  ícone de busca que consulta `GET /api/v1/empresas/consulta-cnpj/{cnpj}` e
  pré-preenche Razão Social e UF); nos modos Alterar/Consultar é um
  `MudTextField` somente-leitura mostrando o CNPJ formatado (não pode ser
  alterado depois de cadastrado — usar `MudMask` também nesses modos causava
  um bug do MudBlazor que zerava o valor e quebrava o salvamento). Razão
  Social, UF, I.E., Posição, switches "Ciência da Operação Automática" (DO
  `MANIFESTA`) e Inativo
- **Certificado Digital**: Caminho do Certificado Digital (campo de texto
  livre com o caminho completo do `.pfx` no servidor onde a API roda — sem
  upload; a API lê qualquer caminho que a conta do SO tenha permissão de
  acessar), Senha do Certificado Digital, Pasta de XMLs (caminho completo,
  não é mais sufixo), Ambiente da NFe (Produção=`"P"`/Homologação=`"H"`),
  Último N.S.U. NFe e CTe
- **Execução**: TimeOut, Tempo Retorno, Intervalo Tentativas, Qtd. Tentativas
  (todos em MSEG), Hora Inicial e Hora Final (`MudTimePicker`, janela de
  horário para o download automático — ver `EMPRESA_DTO.md` e
  `JANELA_EXECUCAO.md`). Só `TimeOut` é usado hoje pelo Zeus.Net; Tempo
  Retorno/Intervalo/Qtd. Tentativas ficam reservados para a lógica de
  retentativa do Worker (ver `ZEUS_CONFIGURACAO.md`). Os campos SSL
  Lib/Crypt/HTTP Lib/XML Sign Lib/Type que apareciam na tela legada foram
  removidos — não existem na versão .NET do Zeus DFe
- **E-mail Envio (SMTP)**: e-mail de notificações, servidor/porta/usuário/senha/
  e-mail SMTP, Autenticação SMTP (dropdown autTLS=0/autSSL=1)
- **E-mail Recebimento (POP)**: servidor/porta/usuário/senha/e-mail POP,
  Autenticação POP (mesmo dropdown do SMTP)

Em modo Consultar todos os campos ficam somente-leitura e só aparece o botão
"Fechar". Rótulos dos campos alinhados com a tela legada de cadastro de
empresa (referência visual fornecida pelo usuário).
