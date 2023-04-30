using Garage.Door.Opener;
using Garage.Door.Opener.Services;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using System.Collections.Concurrent;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddSingleton<ConcurrentDictionary<string, (string?, bool)>>(new ConcurrentDictionary<string, (string?, bool)>());

builder.Services.AddTransient<IBluetoothService, BluetoothService>();

// builder.Services.AddWebOptimizer(
//     pipeline =>
//     {
//         pipeline.MinifyCssFiles("css/**/*.css");
//         pipeline.MinifyJsFiles("js/site.js");
//     },
//     options =>
//     {
//         options.EnableDiskCache = true;
//         options.EnableMemoryCache = false;
//     });

builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.EnableDetailedErrors(true);
    options.EnableSensitiveDataLogging(true);
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres-Server"));
});
builder.Logging.AddEntityFramework<MyDbContext>();

builder.Services.AddSingleton<IMqttClient>(serviceProvider =>
{
    var factory = new MqttFactory();
    var mqttClient = factory.CreateMqttClient();

    return mqttClient;
});

builder.Services.AddSingleton<MqttClientOptions>(serviceProvider =>
{
    var options = new MqttClientOptionsBuilder()
            // .WithClientId("Client1")
            .WithTcpServer("infrastructure-pi-1.localdomain")
            .WithCredentials("homeassistant", "aeng2eegaePhude1Ux3ohievou3ar4choh3aifoonoobeix1weew5aigheighoku")
            // .WithTls()
            // .WithCleanSession()
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .Build();

    return options;
});

builder.Services.AddSingleton<GpioController>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var controller = new GpioController(PinNumberingScheme.Board, new RaspberryPi3Driver());

    controller.OpenPin(Constants.GarageDoorOpenedPinNumber, PinMode.InputPullUp);
    controller.OpenPin(Constants.GarageDoorClosedPinNumber, PinMode.InputPullUp);

    controller.RegisterCallbackForPinValueChangedEvent(
        Constants.GarageDoorOpenedPinNumber,
        PinEventTypes.Rising,
        (x, y) =>
        {
            logger.LogInformation("Pin number {PinNumber} has a value of {ChangeType}", y.PinNumber, y.ChangeType);
        });

    controller.RegisterCallbackForPinValueChangedEvent(
        Constants.GarageDoorOpenedPinNumber,
        PinEventTypes.Falling,
        (x, y) =>
        {
            logger.LogInformation("Pin number {PinNumber} has a value of {ChangeType}", y.PinNumber, y.ChangeType);
        });

    controller.RegisterCallbackForPinValueChangedEvent(
        Constants.GarageDoorClosedPinNumber,
        PinEventTypes.Rising,
        (x, y) =>
        {
            logger.LogInformation("Pin number {PinNumber} has a value of {ChangeType}", y.PinNumber, y.ChangeType);
        });

    controller.RegisterCallbackForPinValueChangedEvent(
        Constants.GarageDoorClosedPinNumber,
        PinEventTypes.Falling,
        (x, y) =>
        {
            logger.LogInformation("Pin number {PinNumber} has a value of {ChangeType}", y.PinNumber, y.ChangeType);
        });

    return controller;
});

// builder.Services.AddHostedService<ApplicationHost>();
builder.Services.AddHostedService<MqttBackgroundService>();

var app = builder.Build();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

// app.UseWebOptimizer();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();

    await context.Database.EnsureCreatedAsync();
}

await app.RunAsync();