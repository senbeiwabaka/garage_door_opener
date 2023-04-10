namespace Garage.Door.Opener.Services
{
    public interface IBluetoothService
    {
        Task<bool> ConnectDeviceAsync(string deviceAddress);

        Task DisconnectDeviceAsync(string deviceAddress);
    }
}