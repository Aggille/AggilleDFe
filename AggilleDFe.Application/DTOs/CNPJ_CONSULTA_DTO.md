# DTO de Consulta de CNPJ (CnpjConsultaResultadoDto)

Resultado da consulta de dados cadastrais de uma empresa a partir do CNPJ,
usado para pré-preencher o formulário de inclusão de Empresa.

Fonte: API pública do CNPJ.ws (`https://publica.cnpj.ws/cnpj/{cnpj}`), gratuita,
sem chave de autenticação, limite de 3 consultas por minuto por CNPJ. Dados vêm
da base cadastral da Receita Federal.

- RazaoSocial: string
- NomeFantasia: string?
- SituacaoCadastral: string? — ex: "Ativa"
- Logradouro: string?
- Numero: string?
- Complemento: string?
- Bairro: string?
- Cep: string?
- Cidade: string?
- Uf: string? — sigla (ex: "RJ")
- Ddd: string?
- Telefone: string?
