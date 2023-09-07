using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FFmpeg.AutoGen.Abstractions;

namespace ZeroPlayer.Models
{
    public class AudioFrame
    {
        public TimeSpan Time { get; }
        private byte[] _Data = null;
        public byte[] Data { get { return _Data; } }
        public int Channels { get; }
        public AVChannelLayout ChannelLayout { get; }
        public AVSampleFormat AVSampleFormat { get; }
        public int SampleRate { get; }
        public long Duration { get; }
        public long PTS { get; }
        public long DTS { get; }
        public AVRational TimeBase { get; }
        public AudioFrame(AVRational timeBase, long duration, long pts, long dts, byte[] data, int channels, AVChannelLayout channelLayout, int sampleRate, AVSampleFormat aVSampleFormat)
        {
            TimeBase = timeBase;
            Duration = duration;
            PTS = pts;
            DTS = dts;
            Time = TimeSpan.FromSeconds(pts * ffmpeg.av_q2d(timeBase));
            _Data = data;
            Channels = channels;
            ChannelLayout = channelLayout;
            SampleRate = sampleRate;
            AVSampleFormat = aVSampleFormat;
        }
    }
}
