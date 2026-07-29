// Dispara o download de um arquivo no navegador a partir de um stream vindo
// do .NET (Blazor WASM) - usado pela tela Exportar XMLs pra evitar buscar o
// mesmo arquivo duas vezes (uma pra conferir erro, outra pra baixar de
// verdade), que era o padrão anterior via NavigationManager.NavigateTo.
window.downloadFileFromStream = async (nomeArquivo, streamRef) => {
    const bytes = await streamRef.arrayBuffer();
    const blob = new Blob([bytes]);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = nomeArquivo;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};
