using AggilleDFe.Application.Interfaces;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace AggilleDFe.Infrastructure.Integrations;

// Converte HTML em PDF de verdade usando um Chromium headless (via
// PuppeteerSharp) - o DANFE/DACTE continuam gerados como HTML (DanfeService/
// DacteService), só a renderização final em PDF passa por aqui. Mantém um
// único navegador headless aberto (processo Chromium) reaproveitado entre
// requisições - abrir/fechar o Chromium a cada chamada seria caro demais.
// Ver PDF.md.
public sealed class PuppeteerHtmlToPdfService : IHtmlToPdfService, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IBrowser? _browser;

    public async Task<byte[]> ConverterAsync(string html, CancellationToken cancellationToken = default)
    {
        var browser = await ObterBrowserAsync(cancellationToken);

        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        return await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions { Top = "8mm", Bottom = "8mm", Left = "8mm", Right = "8mm" }
        });
    }

    private async Task<IBrowser> ObterBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsClosed: false })
        {
            return _browser;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_browser is { IsClosed: false })
            {
                return _browser;
            }

            var caminhoExecutavel = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
            if (string.IsNullOrWhiteSpace(caminhoExecutavel))
            {
                // Sem Chromium do sistema configurado (ex.: ambiente de dev fora do
                // Docker) - baixa um Chromium gerenciado pelo PuppeteerSharp.
                await new BrowserFetcher().DownloadAsync();
            }

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = string.IsNullOrWhiteSpace(caminhoExecutavel) ? null : caminhoExecutavel,
                // --no-sandbox: o container roda o processo do Chromium como root
                // (sem USER dedicado no Dockerfile), e o sandbox do Chromium se
                // recusa a rodar como root sem essa flag.
                Args = ["--no-sandbox", "--disable-setuid-sandbox"]
            });
            return _browser;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
        _lock.Dispose();
    }
}
