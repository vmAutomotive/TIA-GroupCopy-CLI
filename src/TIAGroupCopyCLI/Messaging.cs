using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TIAGroupCopyCLI.MessagingFct
{
    static class Messaging
    {

        public static void Progress(string message)
        {
            //MessageBox.Show(message);
            //GenerateText = notInProgressText;
            //ProgressMessage = "";
            Console.WriteLine(message);
        }


        public static void FaultMessage(string message, Exception ex = null, string functionName = "")
        {
            //MessageBox.Show(message);
            //GenerateText = notInProgressText;
            //ProgressMessage = "";
            Console.WriteLine("");
            Console.WriteLine(message);
            if (functionName != "")
            {
                Console.WriteLine("Exception in " + functionName + " : ");
            }
            if (ex != null)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("");
        }

        public static void FaultMessage(string message)
        {
            Console.WriteLine("");
            Console.WriteLine("Fault!  " + message);
        }

    }
}
