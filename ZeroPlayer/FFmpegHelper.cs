using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace ZeroPlayer
{
    public static class FFmpegHelper
    {
        private static bool _IsRegistered = false;
        public static void RegisterFFmpegBinaries(string? path = null)
        {
            if (_IsRegistered) { return; }
            _IsRegistered = true;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (string.IsNullOrEmpty(path))
                {
                    var current = Environment.CurrentDirectory;
                    var probe = Path.Combine("FFmpeg", Environment.Is64BitProcess ? "x64" : "x86");
                    while (current != null)
                    {
                        var ffmpegBinaryPath = Path.Combine(current, probe);

                        if (Directory.Exists(ffmpegBinaryPath))
                        {
                            DynamicallyLoadedBindings.LibrariesPath = ffmpegBinaryPath;
                            break;
                        }
                        current = Directory.GetParent(current)?.FullName;
                    }
                }
                else
                {
                    if (!System.IO.Directory.Exists(path))
                    {
                        throw new DirectoryNotFoundException(path);
                    }
                    DynamicallyLoadedBindings.LibrariesPath = path;
                }
                DynamicallyLoadedBindings.Initialize();
            }
            else { 
                throw new NotSupportedException(); // fell free add support for platform of your choose
            }
        }
        public static AVPixelFormat GetHWPixelFormat(AVHWDeviceType hWDevice)
        {
            return hWDevice switch
            {
                AVHWDeviceType.AV_HWDEVICE_TYPE_NONE => AVPixelFormat.AV_PIX_FMT_NONE,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU => AVPixelFormat.AV_PIX_FMT_VDPAU,
                AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA => AVPixelFormat.AV_PIX_FMT_CUDA,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI => AVPixelFormat.AV_PIX_FMT_VAAPI,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2 => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_QSV => AVPixelFormat.AV_PIX_FMT_QSV,
                AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX => AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX,
                AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA => AVPixelFormat.AV_PIX_FMT_NV12,
                AVHWDeviceType.AV_HWDEVICE_TYPE_DRM => AVPixelFormat.AV_PIX_FMT_DRM_PRIME,
                AVHWDeviceType.AV_HWDEVICE_TYPE_OPENCL => AVPixelFormat.AV_PIX_FMT_OPENCL,
                AVHWDeviceType.AV_HWDEVICE_TYPE_MEDIACODEC => AVPixelFormat.AV_PIX_FMT_MEDIACODEC,
                _ => AVPixelFormat.AV_PIX_FMT_NONE
            };
        }
        private static Dictionary<int, AVHWDeviceType>? avHWDeviceTypes = null;
        private static object _lock = new object();
        public static Dictionary<int, AVHWDeviceType> GetAVHWDeviceTypes()
        {
            if(avHWDeviceTypes == null)
            {
                lock(_lock)
                {
                    if( avHWDeviceTypes == null)
                    {
                        avHWDeviceTypes = new Dictionary<int, AVHWDeviceType>();
                        var type = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE;
                        var number = 0;
                        avHWDeviceTypes.Add(number, type );
                        while ((type = ffmpeg.av_hwdevice_iterate_types(type)) != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
                        {
                            number++;
                            avHWDeviceTypes.Add(number, type);
                        }
                    }
                }
            }
            return avHWDeviceTypes;
        }
    }
}
