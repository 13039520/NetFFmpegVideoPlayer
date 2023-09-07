using FFmpeg.AutoGen.Abstractions;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ZeroPlayer.Models
{
    public class VideoFrame
    {
        public TimeSpan Time { get; }
        private byte[] _Data = null;
        public byte[] Data { get { return _Data; } }
        public int Width { get; }
        public int Height { get; }
        public AVPixelFormat AVPixelFormat { get; }
        public long Duration { get; }
        public long PTS { get; }
        public long DTS { get; }
        public AVRational TimeBase { get; }
        public VideoFrame(AVRational timeBase, long duration, long pts, long dts, byte[] data, int width, int height, AVPixelFormat aVPixelFormat)
        {
            TimeBase = timeBase;
            Duration = duration;
            PTS = pts;
            DTS = dts;
            Time = TimeSpan.FromSeconds((double)pts * ffmpeg.av_q2d(timeBase));
            _Data = data;
            Width = width;
            Height = height;
            AVPixelFormat = aVPixelFormat;
        }
    }
}
