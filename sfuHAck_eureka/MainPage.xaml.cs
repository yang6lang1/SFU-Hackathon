using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using sfuHAck_eureka.Resources;
// Directives
using Microsoft.Devices;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Media;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using System.Net.Sockets;
using System.Text;
using Microsoft.Phone.Shell;

namespace sfuHAck_eureka
{
    public partial class MainPage : PhoneApplicationPage
    {
        const int ECHO_PORT = 8888;  // The Echo protocol uses port 7 in this sample
        const int QOTD_PORT = 8080; // The Quote of the Day (QOTD) protocol uses port 17 in this sample
        string hostName = "d207-023-206-065.wireless.sfu.ca";

        CameraCaptureTask cameraCaptureTask;
        string result;
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.Button2.IsEnabled = false;
            this.Button3.IsEnabled = false;
            cameraCaptureTask = new CameraCaptureTask();
            cameraCaptureTask.Completed += new EventHandler<PhotoResult>(task_Completed);
        }

        void task_Completed(object sender, PhotoResult e)
        {
            result = string.Empty;
            if (e.Error == null)
            {
                BitmapImage bi = new BitmapImage();
                bi.SetSource(e.ChosenPhoto);
                WriteableBitmap wb = new WriteableBitmap(bi);
                MemoryStream ms = new MemoryStream();
                int scaleRate = 93;
                wb.SaveJpeg(ms, wb.PixelWidth / scaleRate, wb.PixelHeight / scaleRate, 0, 100);
                ms.Seek(0, SeekOrigin.Begin);
                var bn = new BitmapImage();
                bn.SetSource(ms);
                WriteableBitmap nbmp = new WriteableBitmap(bn);
                //MessageBox.Show("width: " + nbmp.PixelWidth + ", height: " + nbmp.PixelHeight);
                int size = nbmp.Pixels.Length;
                int[] grayScaleImg = new int[size];
                for (int i = 0; i < size;i ++)
                {
                    int r = ((nbmp.Pixels[i] >> 16) & 0xff);
                    int g = ((nbmp.Pixels[i] >> 8) & 0xff);
                    int b = ((nbmp.Pixels[i] & 0xff));
                    int grayscale = (int)((0.3 * r) + (0.59 * g) + (0.11 * b));
                    grayscale = 255 - (grayscale % 255);
                    nbmp.Pixels[i] = 0xff << 24;
                    nbmp.Pixels[i] |= grayscale << 16;
                    nbmp.Pixels[i] |= grayscale << 8;
                    nbmp.Pixels[i] |= grayscale << 0;
                    grayScaleImg[i] = grayscale;
                    result += grayScaleImg[i];
                    if (i + 1 < grayScaleImg.Length)
                    {
                        result += ",";
                    }
                }

                //MessageBox.Show(grayScaleImg.Length+"");
                this.Button3.IsEnabled = true;
                //Button.Visibility = Visibility.Collapsed;
                Photo.Source = nbmp;
                Photo.Width = wb.PixelWidth / 8;
                Photo.Height = wb.PixelHeight / 8;
                Photo.Visibility = Visibility.Visible;
            
            }
        }

        private async Task WriteToFile()
        {
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(result.ToCharArray());
            // Get the local folder.
            StorageFolder local = Windows.Storage.ApplicationData.Current.LocalFolder;

            // Create a new folder name DataFolder.
            var dataFolder = await local.CreateFolderAsync("DataFolder",
                CreationCollisionOption.OpenIfExists);

            // Create a new file named DataFile.txt.
            var file = await dataFolder.CreateFileAsync("DataFile.txt",
            CreationCollisionOption.OpenIfExists);

            // Write the data from the textbox.
            using (var s = await file.OpenStreamForWriteAsync())
            {
                s.Write(fileBytes, 0, fileBytes.Length);
            }
        }

        private async void saveButtonClicked(object sender, RoutedEventArgs e)
        {
            await WriteToFile();

            this.Button3.IsEnabled = false;
        }

         //<summary>
         //Handle the btnEcho_Click event by receiving text from the Quote of 
         //the Day (QOTD) server and outputting the response 
         //</summary>
        private void btnGetQuote_Click(object sender, RoutedEventArgs e)
        {

            // Make sure we can perform this action with valid data
            if (ValidateRemoteHost())
            {
                // Instantiate the SocketClient object
                SocketClient client = new SocketClient();

                // Attempt connection to the Quote of the Day (QOTD) server
                //Log(String.Format("Connecting to server '{0}' over port {1} (Quote of the Day) ...", txtRemoteHost.Text, QOTD_PORT), true);
                string returnResult = client.Connect(txtRemoteHost.Text, ECHO_PORT);
                //Log(result, false);

                // Note: The QOTD protocol is not expecting data to be sent to it.
                // So we omit a send call in this example.

                // Receive response from the QOTD server
                //Log("Requesting Receive ...", true);
                returnResult = client.Receive();

                // Close the socket conenction explicitly
                client.Close();
            }
        }

        #region UI Validation
        /// <summary>
        /// Validates the txtInput TextBox
        /// </summary>
        /// <returns>True if the txtInput TextBox contains valid data, otherwise 
        /// False.
        ///</returns>
        private bool ValidateInput()
        {
            // txtInput must contain some text
            if (String.IsNullOrWhiteSpace(this.txtRemoteHost.Text))
            {
                MessageBox.Show("Please enter the host name.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates the txtRemoteHost TextBox
        /// </summary>
        /// <returns>True if the txtRemoteHost contains valid data,
        /// otherwise False
        /// </returns>
        private bool ValidateRemoteHost()
        {
            // The txtRemoteHost must contain some text
            if (String.IsNullOrWhiteSpace(txtRemoteHost.Text))
            {
                MessageBox.Show("Please enter a host name");
                return false;
            }

            return true;
        }
        #endregion

        private void CameraButtonPressed(object sender, RoutedEventArgs e)
        {
            cameraCaptureTask.Show();
        }

        private void inquireButtonPressed(object sender, RoutedEventArgs e)
        {
            // Make sure we can perform this action with valid data
            if (ValidateRemoteHost() && ValidateInput())
            {
                // Instantiate the SocketClient
                SocketClient client = new SocketClient();

                // Attempt to connect to the echo server
                string sendResult = client.Connect(hostName, ECHO_PORT);
                
                sendResult = client.Send(result);
                sendResult = client.Receive();
                
                MessageBox.Show(sendResult);
                client.Close();
            }
            this.Button2.IsEnabled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Make sure we can perform this action with valid data
            if (ValidateRemoteHost())
            {
                // Instantiate the SocketClient object
                SocketClient client = new SocketClient();

                // Attempt connection to the Quote of the Day (QOTD) server
                string receiveMsg = client.Connect(txtRemoteHost.Text, QOTD_PORT);

                // Note: The QOTD protocol is not expecting data to be sent to it.
                // So we omit a send call in this example.

                // Receive response from the QOTD server
                receiveMsg = client.Receive();

                // Close the socket conenction explicitly
                MessageBox.Show(receiveMsg);
                client.Close();
            }

        }

        #region Logging
        /// <summary>
        /// Log text to the txtOutput TextBox
        /// </summary>
        /// <param name="message">The message to write to the txtOutput TextBox</param>
        /// <param name="isOutgoing">True if the message is an outgoing (client to server)
        /// message, False otherwise.
        /// </param>
        /// <remarks>We differentiate between a message from the client and server 
        /// by prepending each line  with ">>" and "<<" respectively.</remarks>
        //private void Log(string message, bool isOutgoing)
        //{
        //    string direction = (isOutgoing) ? ">> " : "<< ";
        //    txtOutput.Text += Environment.NewLine + direction + message;
        //}

        /// <summary>
        /// Clears the txtOutput TextBox
        /// </summary>
        //private void ClearLog()
        //{
        //    txtOutput.Text = String.Empty;
        //}
        #endregion

    }
}