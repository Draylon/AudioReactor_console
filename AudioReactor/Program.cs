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
            Console.WriteLine("Hello World!");

            audioReactor = new AudioReactorClass();
            audioReactor.start();
        }
    }
}
