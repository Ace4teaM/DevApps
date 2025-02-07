using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevApps.PythonExtends
{
    public class Console
    {
        public void write(string text)
        {
            System.Console.WriteLine(text);
//            Program.Dispatcher.Invoke(new Action(() => { System.Console.WriteLine(text); }));
        }
    }
}
