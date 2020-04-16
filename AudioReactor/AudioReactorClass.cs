using System;

using NAudio.Wave;
using System.Threading;
using FFTWSharp;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AudioReactor {
    public partial class AudioReactorClass {

        // MICROPHONE ANALYSIS SETTINGS
        private static int RATE = 44100; // sample rate of the sound card
        private static int BUFFERSIZE = (int)Math.Pow(2, 8); // must be a multiple of 2
        private static double[] printDataArray = new double[(int)Math.Pow(2, 6)];
        //private static double[] printingDataArray= new double[(int)Math.Pow(2, 6)];
        private static bool printDataArrayIsReady = false;
        static int numberOfDraws = 0, printDataArrayLength = printDataArray.Length;
        private static AutoResetEvent waitHandleData = new AutoResetEvent(false);
        private static AutoResetEvent waitHandlePrint = new AutoResetEvent(false);

        private static int[] dataMappingBias = new int[] { 1,1,2,4 };
        private static string[] dataMapCharacter = new string[] { "=","~","-","°" };
        private static int dataMappingSum = 8;
        private static double dataMappingLoop = printDataArrayLength / dataMappingSum;
        private static int fpsCap = 15;
        private static double printRange = 550;
        private static double fftSum = 0;

        // prepare class objects
        private static BufferedWaveProvider bwp;

        public AudioReactorClass(int type,int audioDeviceNumber){
            if (type == -5)
                selectAudioType();
            if (audioDeviceNumber == -5)
                audioDeviceNumber = selectMicrophoneDevice();
            StartListeningToMicrophone(audioDeviceNumber);
        }

        void AudioDataAvailable(object sender, WaveInEventArgs e){
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        int selectAudioType() {
            return 0;
            Console.SetCursorPosition(0, 2);
            Console.WriteLine("Listening to:   0 - Microphone  |  1 - Speaker");
            string s = Console.ReadLine();
            int aty=0;
            if(s == "" || s == null)
                try {
                    aty=int.Parse(s);
                } catch (Exception e) {
                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine("Invalid type!");
                    return selectAudioType();
                }
            return aty;
        }

        int selectMicrophoneDevice() {
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Selecione dispositivo: ");
            Console.SetCursorPosition(0, 3);
            Dictionary<int,string> d = new Dictionary<int,string>();
            for (int n = -1; n < WaveIn.DeviceCount; n++) {
                var caps = WaveIn.GetCapabilities(n);
                Console.WriteLine($"{n}: {caps.ProductName}");
                d.Add(n,caps.ProductName);
            }
            Console.Write("ID: ");
            string id = Console.ReadLine();
            if (id == "") {
                Console.WriteLine("Default");
                return 0;
            }
            if (d.ContainsKey(int.Parse(id))) {
                //Console.WriteLine("Selected: |" + id + "|");
                return int.Parse(id);
            } else {
                Console.SetCursorPosition(0, 1);
                Console.WriteLine("Device not found!");
                return selectMicrophoneDevice();
            }
        }

        int selectAudioDevice() {
            Console.SetCursorPosition(0, 1);
            Console.WriteLine("Selecione dispositivo: ");
            Console.SetCursorPosition(0, 3);
            Dictionary<int, string> d = new Dictionary<int, string>();
            for (int n = -1; n < WaveOut.DeviceCount; n++) {
                var caps = WaveOut.GetCapabilities(n);
                Console.WriteLine($"{n}: {caps.ProductName}");
                d.Add(n, caps.ProductName);
            }
            Console.Write("ID: ");
            string id = Console.ReadLine();
            if (id == "") {
                Console.WriteLine("Default");
                return 0;
            }
            if (d.ContainsKey(int.Parse(id))) {
                //Console.WriteLine("Selected: |" + id + "|");
                return int.Parse(id);
            } else {
                Console.SetCursorPosition(0, 1);
                Console.WriteLine("Device not found!");
                return selectAudioDevice();
            }
        }


        public void StartListeningToMicrophone(int audioDeviceNumber = 0){
            WaveInEvent wi = new WaveInEvent();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * 2;
            bwp.DiscardOnBufferOverflow = true;
            try{
                wi.StartRecording();
            }
            catch (Exception e){
                Console.WriteLine(e);
                string msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                Console.WriteLine(msg);
            }
        }

        public void StartListeningSpeaker(int audioDeviceNumber = 0) {
            WaveInEvent wi = new WaveInEvent();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * 2;
            bwp.DiscardOnBufferOverflow = true;
            try {
                wi.StartRecording();
            } catch (Exception e) {
                Console.WriteLine(e);
                string msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                Console.WriteLine(msg);
            }
        }



        private static Thread getLatestData = new Thread(() => {
            // check the incoming microphone audio
            int frameSize = BUFFERSIZE;
            var audioBytes = new byte[frameSize];
            while (true){
                bwp.Read(audioBytes, 0, frameSize);
                // return if there's nothing new to plot
                if (audioBytes.Length == 0)
                    continue;
                if (audioBytes[frameSize - 2] == 0)
                    continue;
                // incoming data is 16-bit (2 bytes per audio point)
                int BYTES_PER_POINT = 2;
                // create a (32-bit) int array ready to fill with the 16-bit data
                int graphPointCount = audioBytes.Length / BYTES_PER_POINT;
                // create double arrays to hold the data we will graph
                double[] pcm = new double[graphPointCount];
                //double[] fft = new double[graphPointCount];
                double[] fftReal = new double[graphPointCount / 2];

                // populate Xs and Ys with double data
                for (int i = 0; i < graphPointCount; i++) {
                    // read the int16 from the two bytes
                    Int16 val = BitConverter.ToInt16(audioBytes, i * 2);
                    // store the value in Ys as a percent (+/- 100% = 200%)
                    pcm[i] = (double)(val) / Math.Pow(2, 16) * printRange;
                }

                // calculate the full FFT
                fftReal = FFT(pcm);
                if (printDataArray.Length != fftReal.Length) {
                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine("DIFFERENT ARRAY SIZE!!!");}
                printDataArrayIsReady = false;
                //double diff = 0;
                for (int idarr = 0; idarr < printDataArray.Length; idarr++) {
                    if (fftReal[idarr] > printDataArray[idarr])
                        printDataArray[idarr] = ((fftReal[idarr]*0.7 + printDataArray[idarr])*0.3);
                    else
                        printDataArray[idarr] = ((fftReal[idarr]*0.4 + printDataArray[idarr])*0.6);
                    //diff = Math.Abs((fftReal[idarr] - printDataArray[idarr]));
                    //printDataArray[idarr] += (fftReal[idarr] - printDataArray[idarr]);
                }
                if (fftSum > 0) {
                    if (fftSum < 20 && printRange < 10000) {
                        printRange += 50;
                    }
                    if(fftSum > 40 && printRange > 200){
                        printRange -= 50;
                    }
                }
                //if (fftSum > 0) {
                //    printRange = 200 * (80 / Math.Max(fftSum, 4));
                //}
                printDataArrayIsReady = true;
                waitHandlePrint.Set();
                //Thread.Sleep(30);
                waitHandleData.WaitOne(fpsCap);
            }
        });



        private static Thread printConsole = new Thread(() => {
            waitHandlePrint.WaitOne(fpsCap);
            int linelimit = 30;
            Console.SetCursorPosition(0, 1);
            for (var j = 0; j < printDataArrayLength; j++){
                for (var i = 0; i < 80; i++) {
                    Console.Write(" ");
                }Console.WriteLine("");
            }
            string print = "";
            if (dataMappingLoop < 1) {
                Console.WriteLine("Cannot amplify the data range (yet)");
                return;}
            double val = 0;
            int dbv;
            while (true) {
                numberOfDraws++;
                Console.SetCursorPosition(20, 0);
                Console.Write(numberOfDraws);
                if (printDataArrayIsReady) {
                    print = "";
                    Console.SetCursorPosition(0, 1);

                    int daitr = 0;
                    for(int dbi=0;dbi< dataMappingBias.Length; dbi++) {
                        dbv = dataMappingBias[dbi];
                        for(int ii=0;ii < dataMappingLoop; ii++) {
                            val = 0;
                            for(int ig=0;ig < dbv; ig++){
                                val += printDataArray[daitr];
                                daitr++;
                            }
                            print += dbv + " - " + val.ToString("000") + " ";
                            //print += dbv + " - ";
                            while (linelimit > 0) {
                                if (val > 0) {
                                    print += dataMapCharacter[dbi];
                                    val--;
                                } else {
                                    print += " ";
                                }
                                linelimit--;
                            }
                            linelimit = 30;
                            print += "\n";
                        }

                    }
                    print += fftSum.ToString("000")+ " " + printRange.ToString("0000")+"\n";
                    
                    //printDataArrayIsReady = false;
                    Console.WriteLine(print);
                }
                if (numberOfDraws > 9999)
                    numberOfDraws = 0;
                waitHandleData.Set();
                Thread.Sleep(fpsCap);
                waitHandlePrint.WaitOne(fpsCap);
            }
        });

        private Thread thread = new Thread(() => {
            getLatestData.Start();
            printConsole.Start();
        });

        public void start(){
            if (thread.ThreadState == ThreadState.Unstarted)
                thread.Start();
            else
                Console.WriteLine("Already started, close the program to run again");
        }

        public static double[] FFT(double[] data){
            double[] fft = new double[data.Length / 2];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            fftSum = 0;
            for (int i = 0; i < data.Length / 2; i++) {
                fft[i] = fftComplex[i].Magnitude;
                fftSum += fftComplex[i].Magnitude;
            }
            fftSum /= dataMappingLoop;

            return fft;
        }

        public static double[] FFTF(double[] data){
            double[] fft = new double[data.Length];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            IntPtr ptr = fftw.malloc(fftComplex.Length * sizeof(double));
            Marshal.Copy(data, 0, ptr, fftComplex.Length);
            IntPtr plan = fftw.dft_1d(fftComplex.Length / 2, ptr, ptr, fftw_direction.Forward, fftw_flags.Estimate);
            fftw.execute(plan);
            Marshal.Copy(ptr, fft, 0, fftComplex.Length);
            fftw.destroy_plan(plan);
            fftw.free(ptr);
            fftw.cleanup();
            double[] fftReal = new double[data.Length/2];
            for (int i = 0; i < data.Length / 2; i++)
                fftReal[i] = fft[i+i];
            return fftReal;
        }
    }
}

//private void Timer_Tick(object sender, EventArgs e){
//    // turn off the timer, take as long as we need to plot, then turn the timer back on
//    timerReplot.Enabled = false;
//    PlotLatestData();
//    timerReplot.Enabled = true;
//}


//public bool needsAutoScaling = true;
//public void PlotLatestData(){
//    // check the incoming microphone audio
//    int frameSize = BUFFERSIZE;
//    var audioBytes = new byte[frameSize];
//    bwp.Read(audioBytes, 0, frameSize);

//    // return if there's nothing new to plot
//    if (audioBytes.Length == 0)
//        return;
//    if (audioBytes[frameSize - 2] == 0)
//        return;

//    // incoming data is 16-bit (2 bytes per audio point)
//    int BYTES_PER_POINT = 2;

//    // create a (32-bit) int array ready to fill with the 16-bit data
//    int graphPointCount = audioBytes.Length / BYTES_PER_POINT;

//    // create double arrays to hold the data we will graph
//    double[] pcm = new double[graphPointCount];
//    double[] fft = new double[graphPointCount];
//    double[] fftReal = new double[graphPointCount / 2];

//    // populate Xs and Ys with double data
//    for (int i = 0; i < graphPointCount; i++)
//    {
//        // read the int16 from the two bytes
//        Int16 val = BitConverter.ToInt16(audioBytes, i * 2);

//        // store the value in Ys as a percent (+/- 100% = 200%)
//        pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
//    }

//    // calculate the full FFT
//    fft = FFT(pcm);

//    // determine horizontal axis units for graphs
//    double pcmPointSpacingMs = RATE / 1000;
//    double fftMaxFreq = RATE / 2;
//    double fftPointSpacingHz = fftMaxFreq / graphPointCount;

//    // just keep the real half (the other half imaginary)
//    Array.Copy(fft, fftReal, fftReal.Length);

//    // plot the Xs and Ys for both graphs
//    //scottPlotUC1.Clear();
//    //scottPlotUC1.PlotSignal(pcm, pcmPointSpacingMs, Color.Blue);
//    //scottPlotUC2.Clear();
//    //scottPlotUC2.PlotSignal(fftReal, fftPointSpacingHz, Color.Blue);


//    //scottPlotUC1.PlotSignal(Ys, RATE);

//    numberOfDraws += 1;
//    lblStatus.Text = $"Analyzed and graphed PCM and FFT data {numberOfDraws} times";

//    // this reduces flicker and helps keep the program responsive
//    Application.DoEvents();

//}

    //============================
//GETLATESTDATA:
    // determine horizontal axis units for graphs
            //double pcmPointSpacingMs = RATE / 1000;
            //double fftMaxFreq = RATE / 2;
            //double fftPointSpacingHz = fftMaxFreq / graphPointCount;

            // just keep the real half (the other half imaginary)
            //Array.Copy(fft, fftReal, fftReal.Length);