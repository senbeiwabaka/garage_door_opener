using System.Device.Gpio;
using MQTTnet.Client;

namespace Garage.Door.Opener
{
    public sealed class MqttBackgroundService : BackgroundService
    {
        private readonly ILogger<MqttBackgroundService> logger;
        private readonly IMqttClient mqttClient;
        private readonly MqttClientOptions mqttClientOptions;
        private readonly GpioController gpioController;

        public MqttBackgroundService(
            ILogger<MqttBackgroundService> logger,
            IMqttClient mqttClient,
            MqttClientOptions mqttClientOptions,
            GpioController gpioController)
        {
            this.logger = logger;
            this.mqttClient = mqttClient;
            this.mqttClientOptions = mqttClientOptions;
            this.gpioController = gpioController;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("MQTT Hosted Service running.");

            await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);

            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            logger.LogInformation("MQTT Hosted Service is working.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var openedPinValue = gpioController.Read(Constants.GarageDoorOpenedPinNumber);
                var closedPinValue = gpioController.Read(Constants.GarageDoorClosedPinNumber);
                var message = string.Empty;

                if (openedPinValue == PinValue.Low)
                {
                    message = "Garage door is open";
                }

                if (closedPinValue == PinValue.Low)
                {
                    message = "Garage door is closed";
                }

                if (openedPinValue == PinValue.High && closedPinValue == PinValue.High)
                {
                    message = "Garage door is partially opened";
                }

                var payload = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
                    new
                    {
                        message
                    });

                await mqttClient.PublishBinaryAsync("test", payload);

                await Task.Delay(2000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("MQTT Hosted Service is stopping.");

            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), cancellationToken: stoppingToken);

            await base.StopAsync(stoppingToken);
        }
    }
}