namespace GutierrezPrinters.Web.Services;

// "Mantenerse despierto": en el plan gratis de Azure, si nadie entra en 20 minutos el sistema
// se apaga, y el siguiente que lo abre espera a que vuelva a arrancar (eso era lo lento).
// Este servicio visita su propia página /Home/Ping cada pocos minutos por la dirección pública,
// así Azure lo ve con actividad y no lo apaga. De paso, Supabase también ve actividad.
//
// Solo funciona en Azure: ahí existe la variable WEBSITE_HOSTNAME con la dirección del sistema.
// En la compu (Visual Studio) no hace nada.
public sealed class KeepAwakeService : BackgroundService
{
    // Menos de 5 minutos: también mantiene abierta la conexión con Supabase.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(4);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeepAwakeService> _logger;
    private readonly Uri? _pingUrl;

    public KeepAwakeService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<KeepAwakeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _pingUrl = BuildPingUrl(config["WEBSITE_HOSTNAME"]);
    }

    public static Uri? BuildPingUrl(string? hostName)
    {
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return null;
        }

        return Uri.TryCreate($"https://{hostName.Trim()}/Home/Ping", UriKind.Absolute, out var url) ? url : null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_pingUrl == null)
        {
            return; // no está en Azure
        }

        _logger.LogInformation("Despertador interno activo: {Url} cada {Minutes} minutos.", _pingUrl, Interval.TotalMinutes);

        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PingAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // El sistema se está apagando: no pasa nada.
        }
    }

    private async Task PingAsync(CancellationToken stoppingToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(KeepAwakeService));
            client.Timeout = TimeSpan.FromSeconds(60);
            using var response = await client.GetAsync(_pingUrl, stoppingToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("El despertador recibió {Status}.", (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
        {
            // Un fallo de red de vez en cuando no importa: se vuelve a intentar en el siguiente turno.
            _logger.LogWarning("El despertador no pudo visitar el sistema: {Error}", ex.Message);
        }
    }
}
