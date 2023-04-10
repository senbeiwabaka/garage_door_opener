using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Garage.Door.Opener.Services;

namespace Garage.Door.Opener.Pages
{
    public sealed class DevicesModel : PageModel
    {
        private readonly ILogger<DevicesModel> logger;
        private readonly ConcurrentDictionary<string, (string?, bool)> bag;
        private readonly IBluetoothService bluetoothService;

        public DevicesModel(
            ILogger<DevicesModel> logger,
            ConcurrentDictionary<string, (string?, bool)> bag,
            IBluetoothService bluetoothService)
        {
            this.logger = logger;
            this.bag = bag;
            this.bluetoothService = bluetoothService;
        }

        public IList<SelectListItem> Devices { get; set; } = new List<SelectListItem>();

        [BindProperty]
        public string SelectedDevice { get; set; } = default!;

        public void OnGet()
        {
            foreach (var data in bag)
            {
                Devices.Add(new SelectListItem(data.Value.Item1, data.Key, data.Value.Item2));
            }
        }

        public async Task<IActionResult> OnPost()
        {
            logger.LogInformation("Index POST SelectedDevice: {SelectedDevice}", SelectedDevice);

            if (!ModelState.IsValid)
            {
                foreach (var data in bag)
                {
                    Devices.Add(new SelectListItem(data.Value.Item1, data.Key, data.Value.Item2));
                }

                return Page();
            }

            var successful = await bluetoothService.ConnectDeviceAsync(SelectedDevice);

            if (successful)
            {
                bag.TryGetValue(SelectedDevice, out var value);

                var selectedOldValue = value;
                var selectedNewValue = (value.Item1, true);

                foreach (var record in bag)
                {
                    var old = record.Value;
                    var newValue = (record.Value.Item1, false);

                    bag.TryUpdate(record.Key, newValue, old);
                }

                bag.TryUpdate(SelectedDevice, selectedNewValue, selectedOldValue);
            }

            return Page();
        }

        public IActionResult OnGetData()
        {
            var items = new List<object>();

            foreach (var data in bag)
            {
                items.Add(new { data.Value.Item1, data.Key, data.Value.Item2 });
            }

            return Content(System.Text.Json.JsonSerializer.Serialize(items));
        }
    }
}
