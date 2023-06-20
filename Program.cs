
//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Collections;

using nanoFramework.Device.Bluetooth;
using nanoFramework.Device.Bluetooth.Advertisement;
using nanoFramework.Device.Bluetooth.GenericAttributeProfile;

namespace Central2
{
    public struct BatteryProperties
    {
        public UInt16 Capacity;     // mAh
        public UInt16 Level;    // % * 10
        public UInt16 Voltage;  // mV
        public Int16 Current;   // mA
        public Int16 Temperature; // °C * 10
    }

    public struct CurrentDate
    {
        public UInt16 Year;     // mAh
        public Byte Month;    // % * 10
        public Byte Day;  // mV
    }
    public static class Program
    {
        // Devices found by watcher
        private readonly static Hashtable s_foundDevices = new();

        // Devices to collect from. Added when connected
        private readonly static Hashtable s_dataDevices = new();

        public static void Main()
        {
            Console.WriteLine("Sample Client/Central 2 : Collect data from Environmental sensors");
            Console.WriteLine("Searching for Environmental Sensors");

            // Create a watcher
            BluetoothLEAdvertisementWatcher watcher = new();
            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.Received += Watcher_Received;

            while (true)
            {
                Console.WriteLine("Starting BluetoothLEAdvertisementWatcher");
                Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
                watcher.Start();

                // Run until we have found some devices to connect to
                while (s_foundDevices.Count == 0)
                {
                    Thread.Sleep(5000);
                }

                Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
                Console.WriteLine("Stopping BluetoothLEAdvertisementWatcher");

                // We can't connect if watch running so stop it.
                watcher.Stop();

                Console.WriteLine();
                Console.WriteLine($"Devices found = {s_foundDevices.Count}");
                Console.WriteLine();
                Console.WriteLine($"---------------------------------------");
                //Console.WriteLine("Connecting and Reading data");

                foreach (DictionaryEntry entry in s_foundDevices)
                {
                    BluetoothLEDevice device = entry.Value as BluetoothLEDevice;

                    // Connect and receive serial
                    //if (ConnectAndRegister(device))
                    //{
                    //    if (s_dataDevices.Contains(device.BluetoothAddress))
                    //    {
                    //        s_dataDevices.Remove(device.BluetoothAddress);
                    //    }
                    //    s_dataDevices.Add(device.BluetoothAddress, device);
                    //}

                    ConnectAndReceiveSome(device);
                }
                Console.WriteLine($"---------------------------------------");

                s_foundDevices.Clear();
            }
        }

        /// <summary>
        /// Check fir device with correct Service UUID in advert and not already found
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool IsValidDevice(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (args.Advertisement.LocalName.Contains($"NB2"))
            {
                return true;
            }

            return false;
        }

        private static void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Print information about received advertisement
            // You don't receive all information in 1 event and it can be split across 2 events
            // AdvertisementTypes 0 and 4
            //Console.WriteLine($"Received advertisement address:{args.BluetoothAddress:X}/{args.BluetoothAddressType} Name:{args.Advertisement.LocalName}  Advert type:{args.AdvertisementType}  Services:{args.Advertisement.ServiceUuids.Length}");

            // Look for advert with our primary service UUID from Bluetooth Sample 3
            if (IsValidDevice(args))
            {
                //Console.WriteLine($"Found an NB2 eeg amplifyer :{args.BluetoothAddress:X}");

                // Add it to list as a BluetoothLEDevice
                BluetoothLEDevice dev = BluetoothLEDevice.FromBluetoothAddress(args.BluetoothAddress, args.BluetoothAddressType);
                s_foundDevices.Add(args.BluetoothAddress, dev);
            }
        }


        /// <summary>
        /// Connect and set-up Temperature Characteristics for value 
        /// changed notifications.
        /// </summary>
        /// <param name="device">Bluetooth device</param>
        /// <returns>True if device connected</returns>

        private static void ConnectAndReceiveSome(BluetoothLEDevice device)
        {
            GattDeviceServicesResult sr = device.GetGattServices();
            GattCharacteristicsResult cr;

            if (sr.Status == GattCommunicationStatus.Success)
            {
                // Pick up all temperature characteristics
                foreach (GattDeviceService service in sr.Services)
                {
                    Console.WriteLine($"{service.Uuid}");
                    Console.WriteLine($"===========================");

                    cr = service.GetCharacteristics();
                    if (cr.Status == GattCommunicationStatus.Success)
                    {
                        foreach (GattCharacteristic gatt_char in cr.Characteristics)
                        {
                            Console.WriteLine($"{gatt_char.Uuid}");
                        }
                    }
                    Console.WriteLine($"===========================");
                }
            }

            sr = device.GetGattServicesForUuid(GenericAccess);
            cr = sr.Services[0].GetCharacteristicsForUuid(DeviceNameString);
            GattCharacteristic gc = cr.Characteristics[0];
            GattReadResult rr = gc.ReadValue();
            DataReader rdr = DataReader.FromBuffer(rr.Value);
            Console.WriteLine($"Name: {rdr.ReadString(14)}");

            sr = device.GetGattServicesForUuid(DeviceInformation);
            cr = sr.Services[0].GetCharacteristicsForUuid(SerialNumberString);
            gc = cr.Characteristics[0];
            rr = gc.ReadValue();
            rdr = DataReader.FromBuffer(rr.Value);
            Console.WriteLine($"Serial: {rdr.ReadString(4)}");

            sr = device.GetGattServicesForUuid(BatteryService);
            cr = sr.Services[0].GetCharacteristicsForUuid(BatteryPropertiesUuid);
            gc = cr.Characteristics[0];
            rr = gc.ReadValue();
            rdr = DataReader.FromBuffer(rr.Value);
            rdr.ReadBytes(value: PropertyBytes);
            BatteryProperties batteryProperties;
            batteryProperties.Capacity = PropertyBytes[1];
            batteryProperties.Capacity = (UInt16)(batteryProperties.Capacity << 8);
            batteryProperties.Capacity += PropertyBytes[0];
            batteryProperties.Level = PropertyBytes[3];
            batteryProperties.Level = (UInt16)(batteryProperties.Level << 8);
            batteryProperties.Level += PropertyBytes[2];
            batteryProperties.Voltage = PropertyBytes[5];
            batteryProperties.Voltage = (UInt16)(batteryProperties.Voltage << 8);
            batteryProperties.Voltage += PropertyBytes[4];
            batteryProperties.Current = PropertyBytes[7];
            batteryProperties.Current = (Int16)(batteryProperties.Current << 8);
            batteryProperties.Current += PropertyBytes[6];
            batteryProperties.Temperature = PropertyBytes[9];
            batteryProperties.Temperature = (Int16)(batteryProperties.Temperature << 8);
            batteryProperties.Temperature += PropertyBytes[8];
            Console.WriteLine($"capacity: {batteryProperties.Capacity}");
            Console.WriteLine($"level: {batteryProperties.Level}");
            Console.WriteLine($"voltage: {batteryProperties.Voltage}");
            Console.WriteLine($"current: {batteryProperties.Current}");
            Console.WriteLine($"temperature: {batteryProperties.Temperature}");

            sr = device.GetGattServicesForUuid(CurrentDateServUuid);
            cr = sr.Services[0].GetCharacteristicsForUuid(DateUuid);
            gc = cr.Characteristics[0];
            rr = gc.ReadValue();
            rdr = DataReader.FromBuffer(rr.Value);
            rdr.ReadBytes(value: DateBytes);
            CurrentDate currentDate;
            currentDate.Year = DateBytes[1];
            currentDate.Year = (UInt16)(currentDate.Year << 8);
            currentDate.Year += PropertyBytes[0];
            currentDate.Month = PropertyBytes[2];
            currentDate.Day = PropertyBytes[3];
            Console.WriteLine($"Date: {currentDate.Year}:{currentDate.Month}:{currentDate.Day}");

            sr = device.GetGattServicesForUuid(ControlServiceUuid);
            Console.WriteLine($"Control service result status: {sr.Status}");
            GattDeviceService control_service = sr.Services[0];
            Console.WriteLine($"Control service uuid: {control_service.Uuid}");
            cr = control_service.GetCharacteristicsForUuid(CommandUuid);
            Console.WriteLine($"Command characteristic result status: {cr.Status}");
            gc = cr.Characteristics[0];
            Console.WriteLine($"command characteristic uuid: {gc.Uuid}");

            string[] hexValues;
            DataWriter dw;
            byte[] byteArray;
            GattWriteResult wr;

            // start data acquisition
            hexValues = start_acquisition_data_command.Split(' ');
            byteArray = new byte[hexValues.Length];
            for (int i = 0; i < hexValues.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hexValues[i], 16);
            }
            dw = new();
            dw.WriteBytes(byteArray);
            wr = gc.WriteValueWithResult(dw.DetachBuffer(), GattWriteOption.WriteWithResponse);
            Console.WriteLine($"Write result status: {wr.Status}");

            // stop data acquisition
            hexValues = stop_acquisition_data_command.Split(' ');
            byteArray = new byte[hexValues.Length];
            for (int i = 0; i < hexValues.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hexValues[i], 16);
            }
            dw = new();
            dw.WriteBytes(byteArray);
            wr = gc.WriteValueWithResult(dw.DetachBuffer(), GattWriteOption.WriteWithResponse);
            Console.WriteLine($"Write result status: {wr.Status}");

            // turn off command
            hexValues = turn_off_command.Split(' ');
            byteArray = new byte[hexValues.Length];
            for (int i = 0; i < hexValues.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hexValues[i], 16);
            }
            dw = new();
            dw.WriteBytes(byteArray);
            wr = gc.WriteValueWithResult(dw.DetachBuffer(), GattWriteOption.WriteWithResponse);
            Console.WriteLine($"Write result status: {wr.Status}");

            // connection close command
            hexValues = connection_close_command.Split(' ');
            byteArray = new byte[hexValues.Length];
            for (int i = 0; i < hexValues.Length; i++)
            {
                byteArray[i] = Convert.ToByte(hexValues[i], 16);
            }
            dw = new();
            dw.WriteBytes(byteArray);
            wr = gc.WriteValueWithResult(dw.DetachBuffer(), GattWriteOption.WriteWithResponse);
            Console.WriteLine($"Write result status: {wr.Status}");



            device.Close();
        }

        private static void Device_ConnectionStatusChanged(object sender, EventArgs e)
        {
            BluetoothLEDevice dev = (BluetoothLEDevice)sender;
            if (dev.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Console.WriteLine($"Device {dev.BluetoothAddress:X} disconnected");

                // Remove device. We get picked up again once advert seen.
                s_dataDevices.Remove(dev.BluetoothAddress);
                dev.Dispose();
            }
        }

        private static float ReadTempValue(Buffer value)
        {
            DataReader rdr = DataReader.FromBuffer(value);
            return rdr.ReadByte();
        }

        private static void OutputTemp(GattCharacteristic gc, float value)
        {
            Console.WriteLine($"New value => Device:{gc.Service.Device.BluetoothAddress:X} Sensor:{gc.UserDescription,-20}  Current temp:{value}");
        }

        private static void TempValueChanged(GattCharacteristic sender, GattValueChangedEventArgs valueChangedEventArgs)
        {
            OutputTemp(sender,
                ReadTempValue(valueChangedEventArgs.CharacteristicValue));
        }

        static Guid GenericAccess = new("00001800-0000-1000-8000-00805f9b34fb");
        static Guid DeviceNameString = new("00002a00-0000-1000-8000-00805f9b34fb");

        static Guid DeviceInformation = new("0000180a-0000-1000-8000-00805f9b34fb");
        static Guid SerialNumberString = new("00002a25-0000-1000-8000-00805f9b34fb");

        static Guid BatteryService = new("0000180f-0000-1000-8000-00805f9b34fb");
        static Guid BatteryPropertiesUuid = new("5c979c9f-a1ac-5715-9a1b-f81a581179d9");
        const int BatteryPropertiesSize = 10;
        static byte[] PropertyBytes = new byte[BatteryPropertiesSize];

        static Guid CurrentDateServUuid = new("00001805-0000-1000-8000-00805f9b34fb");
        static Guid DateUuid = new("a9b157a1-5827-5553-9ba5-5f5ff8f8e173");
        const int DateSize = 4;
        static byte[] DateBytes = new byte[DateSize];

        static Guid ControlServiceUuid = new("a183c5a7-1e93-8deb-a113-e8d5bb5581db");
        static Guid CommandUuid = new("7395ca15-5997-5a1b-a138-75a7a573b8e5");

        static readonly string start_acquisition_data_command = "01 00 00 01 00 FF FF 00 02 00 00 00 00 00 00 00 02 00 59 6D 3D B3 DC 5C 63 B8 1B 91 D2 76 2D E7 C5 28 DA A8 95 57 18 53 32 55 8B 7E 97 3C A6 A6 0D 99";
        static readonly string start_acquisition_impedance_command = "01 01 00 01 00 FF FF 00 02 00 00 00 00 00 00 00 02 00 3E 73 58 12 CE 8C 90 B1 B0 AB 03 20 35 C2 06 5A 0F C3 AE 3F BB C8 43 E6 24 FC 0F 9C 23 5A 49 70";
        static readonly string stop_acquisition_data_command = "02 8A DC 88 98 EC A3 3B 08 CB BD 40 12 50 FC 6C EA 4E FF 7D 01 C7 87 DC 69 9A 76 52 18 7F FF D5 21";
        static readonly string connection_close_command = "03 67 ED AD EE C5 5D AF 5D A2 FB DC C5 8C 49 62 22 4E 63 64 ED A9 50 AC 9E 58 DF 70 77 CC 08 E2 BC";
        static readonly string turn_off_command = "05 0D DE C2 81 BC 8B 45 00 68 68 47 03 C7 6C B7 DA C0 49 C8 C1 C0 40 82 60 D7 D7 5B EE D9 4B A8 F8 0E";


    }
}
