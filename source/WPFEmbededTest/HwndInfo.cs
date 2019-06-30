using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFEmbededTest
{
    public class HwndInfo
    {
        public IntPtr Hwnd { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; } = "";



        public override string ToString()
        {
            return Hwnd.ToString()+" "+ Title+" "+ClassName;
        }
    }
}
