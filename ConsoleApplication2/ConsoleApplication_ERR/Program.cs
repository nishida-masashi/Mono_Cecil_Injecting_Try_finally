using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication_ERR
{
    class Program
    {
        static void Main(string[] args)
        {
            throw new Exception("oups");
        }
    }
}
