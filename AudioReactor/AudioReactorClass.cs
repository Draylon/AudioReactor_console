using System;

using NAudio.Wave;
using System.Threading;
using FFTWSharp;
using System.Runtime.InteropServices;

namespace AudioReactor
{
    public partial class AudioReactorClass{

        // MICROPHONE ANALYSIS SETTINGS
        private static int RATE = 44100; // sample rate of the sound card
        private static int BUFFERSIZE = (int)Math.Pow(2, 8); // must be a multiple of 2
        private static double[] printDataArray = new double[(int)Math.Pow(2, 6)];
        private static bool printDataArrayIsReady=true;

        // prepare class objects
        private static BufferedWaveProvider bwp;

        public AudioReactorClass(){
            StartListeningToMicrophone();
        }

        void AudioDataAvailable(object sender, WaveInEventArgs e){
            bwp.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }


        public void StartListeningToMicrophone(int audioDeviceNumber = 0)
        {
            WaveInEvent wi = new WaveInEvent();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
            bwp = new BufferedWaveProvider(wi.WaveFormat);
            bwp.BufferLength = BUFFERSIZE * 2;
            bwp.DiscardOnBufferOverflow = true;
            try
            {
                wi.StartRecording();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                string msg = "Could not record from audio device!\n\n";
                msg += "Is your microphone plugged in?\n";
                msg += "Is it set as your default recording device?";
                Console.WriteLine(msg);
            }
        }

        //private void Timer_Tick(object sender, EventArgs e){
        //    // turn off the timer, take as long as we need to plot, then turn the timer back on
        //    timerReplot.Enabled = false;
        //    PlotLatestData();
        //    timerReplot.Enabled = true;
        //}


        public bool needsAutoScaling = true;
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

        public static void getLatestData(int slots = 2){
            
            // check the incoming microphone audio
            int frameSize = BUFFERSIZE;
            var audioBytes = new byte[frameSize];
            bwp.Read(audioBytes, 0, frameSize);

            // return if there's nothing new to plot
            if (audioBytes.Length == 0)
                return;
            if (audioBytes[frameSize - 2] == 0)
                return;

            // incoming data is 16-bit (2 bytes per audio point)
            int BYTES_PER_POINT = 2;
            // create a (32-bit) int array ready to fill with the 16-bit data
            int graphPointCount = audioBytes.Length / BYTES_PER_POINT;
            // create double arrays to hold the data we will graph
            double[] pcm = new double[graphPointCount];
            //double[] fft = new double[graphPointCount];
            double[] fftReal = new double[graphPointCount / 2];

            // populate Xs and Ys with double data
            for (int i = 0; i < graphPointCount; i++){
                // read the int16 from the two bytes
                Int16 val = BitConverter.ToInt16(audioBytes, i * 2);
                // store the value in Ys as a percent (+/- 100% = 200%)
                pcm[i] = (double)(val) / Math.Pow(2, 16) * 200.0;
            }

            // calculate the full FFT
            fftReal = FFT(pcm);
            if (printDataArray.Length != fftReal.Length)
                Console.WriteLine("DIFFERENT ARRAY SIZE!!!");
            for (int idarr = 0; idarr < printDataArray.Length; idarr++){
                if (fftReal[idarr] > printDataArray[idarr])
                    printDataArray[idarr] =  fftReal[idarr];
                else
                    printDataArray[idarr] /= (fftReal[idarr] + 1.5);
            }
            
            // determine horizontal axis units for graphs
            //double pcmPointSpacingMs = RATE / 1000;
            //double fftMaxFreq = RATE / 2;
            //double fftPointSpacingHz = fftMaxFreq / graphPointCount;

            // just keep the real half (the other half imaginary)
            //Array.Copy(fft, fftReal, fftReal.Length);
        }
        static int numberOfDraws = 0;
        private static Thread printConsole = new Thread(() =>{
            string print = "";
            while (true){
                numberOfDraws++;
                Console.SetCursorPosition(20, 0);
                Console.Write(numberOfDraws);
                if (printDataArrayIsReady){
                    print = "";
                    Console.SetCursorPosition(0, 1);
                    for (int isl = 0; isl < printDataArray.Length; isl+=2){
                        double val=0;

                        val = printDataArray[isl] + printDataArray[isl + 1];
                        val *= 1.5;
                        while(val>0){
                            print += "*";
                            val--;
                        }
                        print += "                                                                     \n";
                    }
                    //printDataArrayIsReady = false;
                    Console.WriteLine(print);
                }
                if (numberOfDraws > 9999)
                    numberOfDraws = 0;
                Thread.Sleep(40);
            }
        });

        private Thread thread = new Thread(() =>{
            //AudioReactorClass audioReactor = new AudioReactorClass();
            printConsole.Start();
            int intv = 20;
            while (true){
                Thread.Sleep(intv);
                //printDataArrayIsReady = false;
                getLatestData();
                //printDataArrayIsReady = true;
                Thread.Sleep(intv);
            }
        });

        public void start(){
            thread.Start();
        }

        public static double[] FFT(double[] data)
        {
            double[] fft = new double[data.Length / 2];
            System.Numerics.Complex[] fftComplex = new System.Numerics.Complex[data.Length];
            for (int i = 0; i < data.Length; i++)
                fftComplex[i] = new System.Numerics.Complex(data[i], 0.0);
            Accord.Math.FourierTransform.FFT(fftComplex, Accord.Math.FourierTransform.Direction.Forward);
            for (int i = 0; i < data.Length/2; i++)
                fft[i] = fftComplex[i].Magnitude;
            return fft;
        }

        public double[] FFTF(double[] data){
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
                fftReal[i] = fft[2 * i];
            return fftReal;
        }
    }
}