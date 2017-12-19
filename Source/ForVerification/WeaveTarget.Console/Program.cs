using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaveTarget.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var class1 = new Class1();
            System.Console.WriteLine(class1.GetInterceptAttribute());
            //System.Console.WriteLine(class1.Add2(1, 2));
            System.Console.ReadLine();
        }
    }
}
