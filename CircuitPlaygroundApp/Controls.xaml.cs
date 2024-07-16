
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;

namespace CircuitPlaygroundApp;

public partial class Controls : ContentPage
{
    bool connected = false;

    string dataInServiceUuid = "4A98aaaa-1CC4-E7C1-C757-F1267DD021E8";
    string dataInCharacteristicUuid = "11f836a0-5da3-4bba-815e-d55d2d1c08bc";

    ICharacteristic writeCharacteristic;
    string dataOutServiceUuid = "69137a96-521e-4f05-b3d2-446d68da158a";
    string dataOutCharacteristicUuid = "506561bf-2208-4ffb-b0c1-f5e75bec35ad";

    public Controls()
    {
        InitializeComponent();
        Connect();
    }

    async Task Connect()
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
                ActivityConnecting.IsRunning = true;
                await adapter.StopScanningForDevicesAsync();

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
                        // Update connected state when it happens
                        adapter.DeviceConnected += (s, a) =>
                        {
                            connected = true;
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                OnBtn.IsEnabled = true;
                                OffBtn.IsEnabled = true;
                                StateLbl.Text = "Connected";
                            });
                        };

                        adapter.DeviceDisconnected += (s, a) =>
                        {
                            connected = false;
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                OnBtn.IsEnabled = false;
                                OffBtn.IsEnabled = false;
                                StateLbl.Text = "Disconnected";
                            });
                        };

                        await adapter.ConnectToDeviceAsync(dev);

                        var service = await dev.GetServiceAsync(Guid.Parse(dataInServiceUuid));
                        var characteristic = await service.GetCharacteristicAsync(Guid.Parse(dataInCharacteristicUuid));

                        var writeService = await dev.GetServiceAsync(Guid.Parse(dataOutServiceUuid));
                        writeCharacteristic = await writeService.GetCharacteristicAsync(Guid.Parse(dataOutCharacteristicUuid));

                        characteristic.ValueUpdated += (o, args) =>
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                byte[] v = args.Characteristic.Value;
                                DataLbl.Text = System.Text.Encoding.ASCII.GetString(v);
                            });
                        };

                        await characteristic.StartUpdatesAsync();
                        break;
                    }
                }

                ActivityConnecting.IsRunning = false;
                if (!connected)
                {
                    ConnectBtn.IsVisible = true;
                    StateLbl.Text = "No devices found";
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

    }


    private async void OnActivateClicked(object sender, EventArgs e)
    {
        if (!connected)
        {
            await Connect();
        }

        if (connected)
        {
            writeCharacteristic.WriteAsync( new byte[] { 65 });
        }
    }
    private void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (connected)
        {
            writeCharacteristic.WriteAsync(new byte[] { 66 });
        }
    }

    private async void OnConnectClicked(object sender, EventArgs e)
    {
        if (!connected)
        {
            await Connect();
        }
    }
}