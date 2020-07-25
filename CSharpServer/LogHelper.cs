using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpServer
{
    public static class LogHelper
    {
        public static void Print(string text)
        {
            System.Diagnostics.StackTrace StackTrace = new System.Diagnostics.StackTrace(1, true);
            string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string process = $"{ System.Diagnostics.Process.GetCurrentProcess().ProcessName}[{System.Diagnostics.Process.GetCurrentProcess().Id}]";
            string fileName = $"{System.IO.Path.GetFileName(StackTrace.GetFrame(0).GetFileName())}:{StackTrace.GetFrame(0).GetFileLineNumber()}";
            string str = $"{datetime} {process} {fileName} {text}";
            Console.WriteLine(str);
        }
    }
}
