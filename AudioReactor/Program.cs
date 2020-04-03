using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace AudioReactor{
    class Program{

        private static AudioReactorClass audioReactor;

        [STAThread]
        static void Main(string[] args){
            Console.WriteLine("AudioReactor");

            audioReactor = new AudioReactorClass();
            audioReactor.start();
        }
    }
}
