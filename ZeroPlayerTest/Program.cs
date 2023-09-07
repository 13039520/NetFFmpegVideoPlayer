using FFmpeg.AutoGen.Abstractions;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeroPlayerTest
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ZeroPlayer.FFmpegHelper.RegisterFFmpegBinaries();

            ApplicationConfiguration.Initialize();
            Application.Run(new Form_Main());
        }

    }
}