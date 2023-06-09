﻿using System.Collections.Concurrent;
using System.Device.Gpio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MQTTnet.Client;

namespace Garage.Door.Opener.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private readonly ConcurrentDictionary<string, (string?, bool)> bag;
        private readonly GpioController gpioController;

        public IndexModel(
            ILogger<IndexModel> logger,
            ConcurrentDictionary<string, (string?, bool)> bag,
            GpioController gpioController)
        {
            this.logger = logger;
            this.bag = bag;
            this.gpioController = gpioController;
        }

        [BindProperty]
        public bool IsAllowed { get; set; }

        public bool IsGarageClosed { get; private set; }

        public bool IsGarageInBetween { get; private set; }

        public string? Message { get; private set; }

        public void OnGet()
        {
            if (bag.Any(x => x.Value.Item2))
            {
                IsAllowed = true;
            }

            SetUIStatusOfGarageDoor();
        }

        public void OnPost()
        {
            try
            {
                logger.LogInformation("Index POST IsGarageClosed: {IsGarageClosed}", IsGarageClosed);
                logger.LogInformation("GarageDoorOpenerPinNumber PIN is open?: {IsPINOpen}", gpioController.IsPinOpen(Constants.GarageDoorOpenerPinNumber));

                if (!gpioController.IsPinOpen(Constants.GarageDoorOpenerPinNumber))
                {
                    gpioController.OpenPin(Constants.GarageDoorOpenerPinNumber, PinMode.Output);
                }

                gpioController.Write(Constants.GarageDoorOpenerPinNumber, PinValue.High);

                Thread.Sleep(500);

                gpioController.Write(Constants.GarageDoorOpenerPinNumber, PinValue.Low);

                IsGarageClosed = !IsGarageClosed;

                logger.LogInformation("Index POST IsGarageClosed: {IsGarageClosed}", IsGarageClosed);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to do garage good function");

                Message = "Failed to do garage door function. Try again or contact administrator.";
            }

            SetUIStatusOfGarageDoor();
        }

        private void SetUIStatusOfGarageDoor()
        {
            try
            {
                var openedPinValue = gpioController.Read(Constants.GarageDoorOpenedPinNumber);
                var closedPinValue = gpioController.Read(Constants.GarageDoorClosedPinNumber);

                if (openedPinValue == PinValue.Low)
                {
                    Message = "Garage door is open";
                }

                if (closedPinValue == PinValue.Low)
                {
                    Message = "Garage door is closed";
                    IsGarageClosed = true;
                }

                if (openedPinValue == PinValue.High && closedPinValue == PinValue.High)
                {
                    Message = "Garage door is partially opened";
                    IsGarageInBetween = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get PIN information");
            }
        }
    }
}