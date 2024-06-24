
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

namespace CircuitPlaygroundApp;

public partial class Controls : ContentPage
{
    public bool connected { get; private set; } = false;

    public Controls()
    {
        InitializeComponent();
        Connect();
    }

    async void Connect()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        PermissionStatus blestat = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
        if (status == PermissionStatus.Granted)
        {
            if (blestat != PermissionStatus.Granted)
            {
                blestat = await Permissions.RequestAsync<Permissions.Bluetooth>();
            }
        }
        if (blestat == PermissionStatus.Granted)
        {
            var ble = CrossBluetoothLE.Current;
            var adapter = CrossBluetoothLE.Current.Adapter;

            ble.StateChanged += (s, e) =>
            {
                Debug.WriteLine($"The bluetooth state changed to {e.NewState}");
            };

            try
            {
                // May need to request location services also
                StateLbl.Text = "Scanning...";

                List<IDevice> deviceList = new List<IDevice>();
                adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
                adapter.ScanTimeout = 10000;
                await adapter.StartScanningForDevicesAsync();
                StateLbl.Text = "Scan done";

                foreach (IDevice dev in deviceList)
                {
                    if (dev.Name.Contains("Playground"))
                    {
                        //await adapter.StopScanningForDevicesAsync(dev);

                        DevicesLbl.Text = dev.Name;
                        await adapter.ConnectToDeviceAsync(dev);
                        StateLbl.Text = "Connected";

                        // "4A98aaaa-1CC4-E7C1-C757-F1267DD021E8" from Franklin code
                        string guid = "11f836a0-5da3-4bba-815e-d55d2d1c08bc";
                        var service = await dev.GetServiceAsync(Guid.Parse(guid));
                        var characteristic = await service.GetCharacteristicAsync(Guid.Parse(guid));
                        characteristic.ValueUpdated += (o, args) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                byte[] v = args.Characteristic.Value;
                                DataLbl.Text = String.Format("Light intensity {0}", System.Text.Encoding.ASCII.GetString(v));
                            });
                        };

                        await characteristic.StartUpdatesAsync();
                        break;
                    }
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

    }
    private async void OnScanClicked(object sender, EventArgs e)
    {
         Connect();
    }
}