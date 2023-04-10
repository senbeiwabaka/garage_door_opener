using HashtagChris.DotNetBlueZ;
using HashtagChris.DotNetBlueZ.Extensions;

namespace Garage.Door.Opener.Services
{
    internal sealed class BluetoothService : IBluetoothService
    {
        private readonly ILogger<BluetoothService> logger;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(15);

        public BluetoothService(ILogger<BluetoothService> logger)
        {
            this.logger = logger;
        }

        async Task<bool> IBluetoothService.ConnectDeviceAsync(string deviceAddress)
        {
            try
            {
                using (var adapter = await BlueZManager.GetAdapterAsync("hci0"))
                {
                    using (var device = await adapter.GetDeviceAsync(deviceAddress))
                    {
                        if (device is not null)
                        {
                            await device.ConnectAsync();

                            await device.WaitForPropertyValueAsync("Connected", value: true, timeout);
                            await device.WaitForPropertyValueAsync("ServicesResolved", value: true, timeout);
                        }
                    }

                    using (var device = await adapter.GetDeviceAsync(deviceAddress))
                    {
                        var deviceProperties = await device.GetAllAsync();

                        return deviceProperties.Connected;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connected device {Device}", deviceAddress);
            }

            return false;
        }

        async Task IBluetoothService.DisconnectDeviceAsync(string deviceAddress)
        {
            using (var adapter = await BlueZManager.GetAdapterAsync("hci0"))
            {
                using (var device = await adapter.GetDeviceAsync(deviceAddress))
                {
                    if (device is not null)
                    {
                        await device.DisconnectAsync();
                    }
                }
            }
        }
    }
}