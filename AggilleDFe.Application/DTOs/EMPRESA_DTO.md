# DTO de Empresa (EmpresaDto)

Contrato usado pela API (`GET /api/v1/empresas`, `GET /api/v1/empresas/{id}`,
`POST /api/v1/empresas`, `PUT /api/v1/empresas/{id}`, `DELETE /api/v1/empresas/{id}`)
e pela tela de Empresas (grid + `EmpresaDialog` nos modos Incluir/Alterar/Consultar,
com abas para todos os campos do DO `Empresa`).

- Id: int — 0 ao incluir
- RazaoSocial: string — obrigatório, máx. 60 caracteres
- Cnpj: string — 14 caracteres alfanuméricos (sem máscara), obrigatório, único,
  dígitos verificadores validados. Somente editável no modo Incluir — no modo
  Alterar é exibido formatado, somente-leitura (não pode ser alterado depois
  de cadastrado)
- Uf: string — sigla de unidade federativa válida (2 letras), obrigatório
- CertificadoDigital: string? — caminho **completo** do arquivo .pfx no
  servidor onde a API roda (campo de texto livre, sem upload — a API acessa
  qualquer caminho que a conta do SO rodando o processo tenha permissão de
  ler, não só a própria pasta da aplicação)
- SenhaCertificado: string?
- PastaXml: string? — caminho **completo** (não é mais só um sufixo) onde os
  XMLs desta empresa devem ser salvos. Dentro desse caminho, aplica-se apenas
  a regra de subdivisão já definida no `CLAUDE.md` (ano/mês e tipo de XML —
  NFe/CTe/NFSe); a divisão por CNPJ já é resolvida por este campo ser
  por-empresa. Lógica de gravação em si ainda não implementada (fica para
  quando o Worker de download for construído)
- UltimoNsu: int? — último NSU consultado (NFe/CTe distribuição), controlado pelo Worker
- HoraInicial, HoraFinal: TimeOnly? — janela de horário para o download
  automático (Worker). Se ambos preenchidos, o Worker só baixa XMLs dessa
  empresa com a hora atual dentro da janela (suporta janela cruzando a
  meia-noite); se algum estiver vazio, sem restrição. Execução manual sempre
  ignora essa janela. Ver `AggilleDFe.Application/Services/JANELA_EXECUCAO.md`
- Ambiente: string? — `"P"` Produção (taProducao), `"H"` Homologação (taHomologacao)
- Timeout, TempoRetorno, IntervaloTentativas, QuantidadeTentativas: int? — parâmetros de execução das consultas ( valores em MSEG ). Apenas `Timeout` tem
  correspondência direta no Zeus.Net (`ConfiguracaoServico.TimeOut`); os demais
  são para uso futuro na lógica de retentativa do Worker (ver `ZEUS_CONFIGURACAO.md`)
- EmailEnvioNotificacoes, ServidorSmtp, UsuarioSmtp, SenhaSmtp, EmailSmtp: string?
- TipoAutenticacaoSmtp: int? — 0=autTLS, 1=autSSL
- ServidorPop, UsuarioPop, EmailPop, SenhaPop: string?
- TipoAutenticacaoPop: int? — 0=autTLS, 1=autSSL (mesma lista do SMTP)
- PortaPop, PortaSmtp: int?
- Ie: string? — inscrição estadual
- Manifesta: bool — DO `MANIFESTA` ("S"/"N"); na tela, rótulo "Ciência da Operação Automática"
- Posicao: int?
- Inativo: bool — DO `INATIVO` ("S"/"N")
- UltimoNsuCte: int?
