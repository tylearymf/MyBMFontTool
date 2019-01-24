using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyBMFont
{
    class CMD
    {
        static public void ProcessCommand(string pCommand, string pArgument = default(string), bool pUseShellExecute = true)
        {
            var tInfo = new ProcessStartInfo(pCommand);
            tInfo.Arguments = pArgument;
            tInfo.CreateNoWindow = false;
            tInfo.ErrorDialog = true;
            tInfo.UseShellExecute = pUseShellExecute;

            tInfo.RedirectStandardOutput = !pUseShellExecute;
            tInfo.RedirectStandardError = !pUseShellExecute;
            tInfo.RedirectStandardInput = !pUseShellExecute;

            if (!pUseShellExecute)
            {
                tInfo.StandardErrorEncoding = UTF8Encoding.UTF8;
                tInfo.StandardErrorEncoding = UTF8Encoding.UTF8;
            }

            var tProcess = Process.Start(tInfo);
            if (!pUseShellExecute)
            {
                Console.WriteLine(tProcess.StandardOutput);
                Console.WriteLine(tProcess.StandardError);
            }

            tProcess.WaitForExit();
            tProcess.Close();
        }
    }
}
