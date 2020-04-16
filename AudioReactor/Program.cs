using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading;

namespace AudioReactor{
    class Program{

        private static AudioReactorClass audioReactor;

        private static string getParam(string[] args,string param) {
            int ic = 0;
            foreach(string i in args){
                if (i == param && args.Length > ic+1)
                    return args[ic + 1];
            }
            return "-5";
        }

        //private static void getAudioDevice() {
        //    ManagementObjectSearcher mo = new ManagementObjectSearcher("select * from Win32_SoundDevice");

        //    foreach (ManagementObject soundDevice in mo.Get()) {
        //        foreach (PropertyData property in soundDevice.Properties) {
        //            Console.Out.WriteLine(String.Format("{0}:{1}", property.Name, property.Value));
        //        }
        //        //Console.WriteLine("Caption: " + soundDevice.GetPropertyValue("Caption"));
        //        //Console.WriteLine("Description: " + soundDevice.GetPropertyValue("Description"));
        //        //Console.WriteLine("Manufacturer: " + soundDevice.GetPropertyValue("Manufacturer"));
        //        //Console.WriteLine("Status: " + soundDevice.GetPropertyValue("Status"));
        //        Console.WriteLine("");
        //        // etc                       
        //    }

        //}

        [STAThread]
        static void Main(string[] args){
            Console.WriteLine("AudioReactor");
            Console.SetWindowSize(40, 37);
            int iad = int.Parse(getParam(args, "-d"));
            int type = int.Parse(getParam(args, "-t"));
            audioReactor = new AudioReactorClass(type,iad);
            audioReactor.start();
        }
    }
}
