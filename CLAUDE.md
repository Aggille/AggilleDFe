# Projeto Download de Documentos Fiscais Eletrônico

# Especificações Técnica:

- Linguagem de Programação: C#
- Sempre usar EF Core
- Evitar sempre usar RAW SQL
- Bancos de Dasdos PostgreeSQL
- O nome da base de dados deve ser aggilledfe
- Projeto Web com Blazor
- App com .net Maui
- Usar padrões de projetos atuais
- Usar metodologia SAAS
- Deve ser usada Zeus DFE para tratar as requisições DFe ( https://github.com/ZeusAutomacao/DFe.NET )
- A API deve documentar seus endpoints via Swagger (Microsoft.AspNetCore.OpenApi + Swashbuckle.AspNetCore.SwaggerUI, disponível em /swagger)
- Plataforma deve rodar tanto em servidores windows como linux
- Plataforma deve dar opção de rodar em um container docker

# Regras Canônicas

- Você não tem permissão para comandos crud no banco de dados
- As classes de dados DO e DTO serão definidas dentro de arquivos .md

# Especificações do Projeto

- Objetivo: Essa plataforma deve ser executada em um tempo determinado, e executação a distribuição de documentos fiscais e baixar os xmls disponivies para a empresa
- OS xmls baixados devem ficar em uma pasta, separada por cnpj, periodo ( ano / mes ) e tipo de xml ( NFe, CTe, NFSe )
- Deve ter uma configuração para informar usuário e senha para ser usada no acesso a API
- Deve ser multi emppresa
- Deve ter opção de executar uma empresa a cada ciclo ou todas de uma vez ( em paralelo )
- Deve ter telas de configuração do ambiente e dados das empresas
- Deve ter endpoints de api para :
  - Receber a chave de uma n.f. e devolver o arquivo xml
  - Receber a chave de uma N.F. e devolver o pdf do danfe
- Deve ter opções de fazer a manifestação do destinatário, cancelar a operação ou o não reconhecimento
- Deve manter o registros dos logs das operações
- Deve armazenar sempre o último NSU das notas fiscais e conhecimentos para ser utilizado em cada uma das consuiltas.
- Deve armazenar os dados dos xmls bsixados e opção de imprimir um relatório dos xmls baixados
