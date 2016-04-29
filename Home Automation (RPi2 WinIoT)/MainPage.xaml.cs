using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Home_Automation__RPi2_WinIoT_
{
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Provides access to user credential
        /// </summary>
        public static class UserCredential
        {
            public static readonly string UserName = "admin";
            public static readonly string Password = "admin";
            public static bool LoddegIn = true;
        }

        /// <summary>
        /// Holds global static SensorData. Update I2C_Slave_Address property to retrieve the latest sensor data of that slave.
        /// </summary>
        public static class SensorData
        {
            private static bool AlreadyRunning = false;
            public static int I2C_Slave_Address { get; set; }

            public struct SensorSturct
            {
                public Library.Core.Sensors.AmbientLight AmbientLight;
                public Library.Core.Sensors.PassiveIR PassiveIR;
                public Library.Core.Sensors.Temperature Temperature;
            }

            public static SensorSturct Sensors;

            /// <summary>
            /// Collects sensor data at interval of 1 second. To get data of specific room, do not call this function instead update value to property I2C_Slave_Address and this function will collect data from that slave.
            /// </summary>
            public static void CollectData()
            {
                if (AlreadyRunning == false)
                {
                    AlreadyRunning = true;

                    Sensors = new SensorSturct();
                    Sensors.AmbientLight = new Library.Core.Sensors.AmbientLight();
                    Sensors.PassiveIR = new Library.Core.Sensors.PassiveIR();
                    Sensors.Temperature = new Library.Core.Sensors.Temperature();

                    Task Task_CollectSensorData = new Task(async () =>
                    {
                        while (true)
                        {
                            var Response = await Library.Communication.I2C_Helper.WriteRead(I2C_Slave_Address, Library.Communication.I2C_Helper.Mode.Mode0);

                            // Update Ambient Light
                            Sensors.AmbientLight.RawData = (short)Response[0];

                            //Update PIR State
                            Sensors.PassiveIR.HumanDetected = (((byte)Response[1]) == 1) ? true : false;

                            // Update Temperature
                            Sensors.Temperature.Celsius = (byte)Response[3];
                            Sensors.Temperature.Celsius *= (((byte)Response[2])==0)?-1:1; // Update Temperature Sign. Refer mode 0 for details.

                            Debug.WriteLine(Sensors.AmbientLight.RawData);
                            Debug.WriteLine(Sensors.PassiveIR.HumanDetected.ToString());
                            Debug.WriteLine(Sensors.Temperature.Celsius);
                            Debug.WriteLine("=======================");

                            await Task.Delay(1000);
                        }
                    });

                    Task_CollectSensorData.Start();
                    Task_CollectSensorData.Wait();
                }
            }
        }

        public static Library.Core.Home MyHome;
        public static Frame SharedFrame;

        public MainPage()
        {
            this.InitializeComponent();

            // Start Task that updates time
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () =>
                    {
                        Lbl_Time.Text = DateTime.Now.ToString("hh:mm tt");
                        Lbl_Date.Text = DateTime.Now.ToString("MMMM dd, yyyy");
                    });
                    await Task.Delay(1000);
                }
            });

            // Load last saved Home object
            MyHome = Library.Core.Home.LoadHome().Result;

            // Turn Off all devices
            foreach (var Room in MyHome.Rooms)
            {
                foreach (var Device in Room.Devices)
                {
                    Task.Factory.StartNew(() => { Device.TurnOff(); });
                }
            }

            if(MyHome!=null && MyHome.Rooms != null &&  MyHome.Rooms.Count>0)
            {
                SensorData.I2C_Slave_Address = MyHome.Rooms[0].I2C_Slave_Address;
                SensorData.CollectData();
            }

            SharedFrame = Frame_Main;
        }

        private void Btn_ShowRooms_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame_Main.Visibility = Visibility.Visible;
            Lbl_Time.Visibility = Visibility.Collapsed;
            Lbl_Date.Visibility = Visibility.Collapsed;

            Pages.Page_Room ShowRoom = new Pages.Page_Room();
            Frame_Main.Navigate(ShowRoom.GetType());
        }

        private void Btn_ShowConfiguration_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame_Main.Visibility = Visibility.Visible;
            Lbl_Time.Visibility = Visibility.Collapsed;
            Lbl_Date.Visibility = Visibility.Collapsed;

            Pages.Page_Configuration _PC = new Pages.Page_Configuration();
            Frame_Main.Navigate(_PC.GetType());
        }

        private void Frame_Main_Navigated(object sender, NavigationEventArgs e)
        {
            
        }

        private void Frame_Main_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if(Frame_Main.SourcePageType!=null && Frame_Main.SourcePageType.Name=="Page_Devices")
            {
                //MethodInfo UpdateSensorData = Frame_Main.SourcePageType.GetMethod("ReleaseSensorThread");
                //if (UpdateSensorData != null)
                //{
                //    UpdateSensorData.Invoke(Frame_Main.SourcePageType, null);
                //}
                ((Pages.Page_Devices)Frame_Main.Content).ReleaseSensorThread();
            }
        }

        private void Btn_Home_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame_Main.Visibility = Visibility.Collapsed;
            Lbl_Time.Visibility = Visibility.Visible;
            Lbl_Date.Visibility = Visibility.Visible;
        }
    }
}

//Task.Factory.StartNew(async() =>
//            {
//                while (true)
//                {
//                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
//                                        () =>
//                                        {
//                                            //txt_Time.Text = DateTime.Now.Hour + "/" + DateTime.Now.Minute + "/" + DateTime.Now.Second;
//                                        });
//                    await Task.Delay(1000);
//                }
//            });