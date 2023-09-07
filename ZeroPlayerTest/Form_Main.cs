using FFmpeg.AutoGen.Abstractions;
using Microsoft.VisualBasic.Devices;
using NAudio.Gui;
using NAudio.Wave;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ZeroPlayerTest
{
    public partial class Form_Main : Form
    {
        private WaveOut? waveOut;
        private BufferedWaveProvider? bufferedWaveProvider;
        private ZeroPlayer.Media? media = null;
        public Form_Main()
        {
            InitializeComponent();
            this.button_FastBackward.Click += Button_FastBackward_Click;
            this.button_FastForward.Click += Button_FastForward_Click;
            this.button_Stop.Click += Button_Stop_Click;
            this.button_Pause.Click += Button_Pause_Click;
            this.button_Play.Click += Button_Play_Click;
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string[] files = new string[]
                {
                    @"E:\VideoTools\videos\001.mp4",
                    @"E:\VideoTools\videos\002.mp4"
                };
            Play(files[1]);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            this.Hide();
            if (media != null)
            {
                media.Dispose();
            }
            if (waveOut != null)
            {
                waveOut.Dispose();
            }
        }

        private void Play(string url)
        {
            media = new ZeroPlayer.Media(url, AVHWDeviceType.AV_HWDEVICE_TYPE_NONE,100);
            if (media.MediaState != ZeroPlayer.Models.MediaState.Ready)
            {
                _ShowMsg(media.ErrorMessage);
                media.Dispose();
                media = null;
                return;
            }
            double duration = media.Duration * ffmpeg.av_q2d(ffmpeg.av_get_time_base_q());

            StringBuilder s = new StringBuilder("info:");
            var info = media.GetContextInfo();
            info.ToList().ForEach(x => s.Append($"\r\n{x.Key} = {x.Value}"));
            s.Append("\r\n-----------------------");
            s.Append($"\r\nDuration: {TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss")}");
            s.Append($"\r\nV_CodecName: {media.V_CodecName}");
            s.Append($"\r\nV_FPS: {ffmpeg.av_q2d(media.V_FrameRate)}");
            s.Append($"\r\nV_PixelFormat: {media.V_PixelFormat}");
            s.Append($"\r\nV_CovertedPixelFormat: {media.V_ConvertedPixelFormat}");
            s.Append($"\r\nV_FrameSize: {media.V_Width}*{media.V_Height}");
            s.Append($"\r\nV_Time_Base: {media.V_TimeBase.num}/{media.V_TimeBase.den}");
            if (media.HasAudio)
            {
                s.Append("\r\n-----------------------");
                s.Append($"\r\nA_CodecName: {media.A_CodecName}");
                s.Append($"\r\nA_Channels: {media.A_Channels}");
                s.Append($"\r\nA_Sample_Rate: {media.A_SampleRate}");
                s.Append($"\r\nA_Sample_Fmt: {media.A_SampleFmt}");
                s.Append($"\r\nA_ConvertedSampleFmt: {media.A_ConvertedSampleFmt}");
                s.Append($"\r\nA_Time_Base: {media.A_TimeBase.num}/{media.A_TimeBase.den}");
            }
            _ShowMsg(s.ToString());

            media.OnStateChange += Media_OnStateChange;
            media.OnAudioPlay += Media_OnAudioPlay;
            media.OnVideoPlay += Media_OnVideoPlay;
            media.OnTimeUpdate += Media_OnTimeUpdate;

            media.Play();
        }

        #region -- callback --
        private void Media_OnTimeUpdate(TimeSpan obj)
        {
            this.BeginInvoke(new Action(() =>
            {
                this.textBox_PlayTime.Text = obj.ToString(@"hh\:mm\:ss");
            }));
        }
        private void Media_OnStateChange(ZeroPlayer.Models.MediaState obj)
        {
            if (obj == ZeroPlayer.Models.MediaState.Stop || obj == ZeroPlayer.Models.MediaState.End)
            {
                if (bufferedWaveProvider != null)
                {
                    bufferedWaveProvider.ClearBuffer();
                }
            }
            _ShowMsg("media state=>{0}", obj.ToString());
        }
        private void Media_OnAudioPlay(ZeroPlayer.Models.AudioFrame obj)
        {
            if (waveOut == null)
            {
                waveOut = new WaveOut();
                if (bufferedWaveProvider == null)
                {
                    bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat());
                }
                else
                {
                    bufferedWaveProvider.ClearBuffer();
                }
                waveOut.Init(bufferedWaveProvider);
                waveOut.Play();
            }
            if (bufferedWaveProvider.BufferLength <= bufferedWaveProvider.BufferedBytes + obj.Data.Length)
            {
                bufferedWaveProvider.ClearBuffer();
            }
            bufferedWaveProvider.AddSamples(obj.Data, 0, obj.Data.Length);
        }
        private void Media_OnVideoPlay(ZeroPlayer.Models.VideoFrame obj)
        {
            this.BeginInvoke(new Action(() =>
            {
                Image img = this.pictureBox1.Image;
                if (obj.Data == null)
                {
                    return;
                }
                this.pictureBox1.Image = Image.FromStream(new MemoryStream(obj.Data));
                if (img != null)
                {
                    img.Dispose();
                }
            }));
        }
        #endregion

        #region -- play ctrl --
        private void Button_Play_Click(object? sender, EventArgs e)
        {
            if (media != null)
            {
                media.Play();
            }
        }

        private void Button_Pause_Click(object? sender, EventArgs e)
        {
            if (media != null)
            {
                media.Pause();
            }
        }

        private void Button_Stop_Click(object? sender, EventArgs e)
        {
            if (media != null)
            {
                media.Stop();
            }
        }

        private void Button_FastForward_Click(object? sender, EventArgs e)
        {
            if (media != null)
            {
                var sVal = (long)media.CurrentPosition.TotalSeconds;
                media.JumpToSeconds(sVal + 10);
            }
        }

        private void Button_FastBackward_Click(object? sender, EventArgs e)
        {
            if (media != null)
            {
                var sVal = (long)media.CurrentPosition.TotalSeconds;
                if (sVal > 10)
                {
                    media.JumpToSeconds(sVal - 10);
                }
            }
        }
        #endregion


        private void _ShowMsg(string msg)
        {

            this.BeginInvoke(new Action(() =>
            {
                string text = string.Format("{0}\t{1}\r\n{2}", DateTime.Now.ToString("HH:mm:ss.fff"), msg, this.textBox_Msg.Text);
                if (text.Length > 10000)
                {
                    text = text.Substring(0, 10000) + "......";
                }
                this.textBox_Msg.Text = text;
            }));
        }
        private void _ShowMsg(string format, params object[] ps)
        {
            _ShowMsg(string.Format(format, ps));
        }


    }
}