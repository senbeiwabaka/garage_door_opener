using System.Collections.Concurrent;
using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace Garage.Door.Opener
{
    internal sealed class ApplicationHost : IHostedService, IDisposable
    {
        private readonly ILogger<ApplicationHost> logger;
        private readonly ConcurrentDictionary<string, (string?, bool)> bag;

        private Timer? timer = null;
        private Adapter? adapter = null;

        public ApplicationHost(ILogger<ApplicationHost> logger, ConcurrentDictionary<string, (string?, bool)> bag)
        {
            this.logger = logger;
            this.bag = bag;
        }

        public void Dispose()
        {
            adapter?.Dispose();

            timer?.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            adapter = await BlueZManager.GetAdapterAsync("hci0");

            var adapterInformation = await adapter.GetAllAsync();

            // logger.LogDebug("adapterInformation.Address: {Value}", adapterInformation.Address);
            // logger.LogDebug("adapterInformation.AddressType: {Value}", adapterInformation.AddressType);
            // logger.LogDebug("adapterInformation.Alias: {Value}", adapterInformation.Alias);
            // logger.LogDebug("adapterInformation.Name: {Value}", adapterInformation.Name);
            // logger.LogDebug("adapterInformation.Discoverable: {Value}", adapterInformation.Discoverable);
            // logger.LogDebug("adapterInformation.DiscoverableTimeout: {Value}", adapterInformation.DiscoverableTimeout);
            // logger.LogDebug("adapterInformation.Pairable: {Value}", adapterInformation.Pairable);
            // logger.LogDebug("adapterInformation.Powered: {Value}", adapterInformation.Powered);

            await adapter.StartDiscoveryAsync();

            logger.LogInformation("Starting discovery");

            timer = new Timer(DoWork, cancellationToken, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping discovery");

            timer?.Dispose();

            bag.Clear();

            if (adapter is not null)
            {
                await adapter.StopDiscoveryAsync();

                adapter.Dispose();
            }
        }

        private async void DoWork(object? state)
        {
            try
            {
                var devices = await adapter.GetDevicesAsync();
                var items = new List<string>();

                foreach (var device in devices)
                {
                    var deviceProperties = await device.GetAllAsync();

                    if (string.IsNullOrWhiteSpace(deviceProperties.Name))
                    {
                        continue;
                    }

                    items.Add(deviceProperties.Address);

                    if (!bag.ContainsKey(deviceProperties.Address))
                    {
                        logger.LogInformation("all -> Address: {Address} Name: {Name} Connected: {Connected}", deviceProperties.Address, deviceProperties.Name, deviceProperties.Connected);

                        bag.TryAdd(deviceProperties.Address, (deviceProperties.Name, deviceProperties.Connected));
                    }
                }

                foreach (var item in bag)
                {
                    if (!items.Contains(item.Key))
                    {
                        bag.Remove(item.Key, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get devices");
            }
        }
    }
}