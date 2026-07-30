// Exibe o DANFE/DACTE (PDF gerado pela API) numa aba nova, sem deixar a aba
// em branco enquanto o servidor processa (a geração via Chromium headless -
// ver PDF.md - não é instantânea). Usado pela tela XMLs Baixados.
window.aggilleDfe = window.aggilleDfe || {};

// Abre (ou reaproveita, pelo nome da janela) uma aba mostrando uma mensagem
// de "gerando, aguarde" - precisa ser chamada de forma síncrona, como
// primeira instrução do clique, sem nenhum await antes, senão o navegador
// trata como pop-up não solicitado e bloqueia.
window.aggilleDfe.abrirJanelaCarregando = function (nomeJanela) {
    const janela = window.open('', nomeJanela);
    if (janela) {
        // Mesmo spinner/estilo da tela de splash inicial (index.html, #ag-splash),
        // pra manter o padrão visual do app (CLAUDE.md: "mantenha sempre o mesmo
        // estilo visual nas páginas").
        janela.document.write(
            '<!doctype html><html><head><title>Gerando PDF...</title></head>' +
            '<body style="display:flex;align-items:center;justify-content:center;flex-direction:column;gap:16px;' +
            'height:100vh;margin:0;font-family:Roboto,Arial,sans-serif;color:#666;">' +
            '<svg width="40" height="40" viewBox="0 0 48 48" style="animation:ag-spin 0.9s linear infinite;">' +
            '<circle cx="24" cy="24" r="18" stroke="#D98C90" stroke-width="4" fill="none" ' +
            'stroke-dasharray="75 30" stroke-linecap="round" /></svg>' +
            '<p>Gerando PDF, aguarde...</p>' +
            '<style>@keyframes ag-spin { to { transform: rotate(360deg); } }</style>' +
            '</body></html>'
        );
    }
    return janela !== null;
};

// Troca o conteúdo da aba aberta por abrirJanelaCarregando pelo PDF de
// verdade, a partir de um stream vindo do .NET. Navegar a aba pra url do
// blob (em vez de simular clique num <a download>) faz o navegador exibir o
// PDF inline, no visualizador nativo, em vez de baixar o arquivo.
window.aggilleDfe.exibirPdfNaJanela = async function (nomeJanela, streamRef) {
    const bytes = await streamRef.arrayBuffer();
    const blob = new Blob([bytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    window.open(url, nomeJanela);
};

// Mostra uma mensagem de erro na aba aberta por abrirJanelaCarregando - pra
// quando a geração do PDF falha (chave não encontrada, etc.), em vez de
// deixar a aba presa em "Gerando PDF, aguarde...".
window.aggilleDfe.fecharJanelaComErro = function (nomeJanela, mensagem) {
    const janela = window.open('', nomeJanela);
    if (janela) {
        janela.document.body.innerHTML = '<p style="color:#b00020;font-family:Roboto,Arial,sans-serif;">' + mensagem + '</p>';
    }
};
