﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using SkiaSharp;

namespace ZeroPlayer
{
    /// <summary>
    /// Wraps a media file and provides playback associated callbacks
    /// </summary>
    public unsafe class Media : IDisposable
    {
        #region -- field --
        private  AVFormatContext* _avFormatContext;
        private AVPacket* _pPacket;

        private  int _vStreamIndex;
        private  AVCodecContext* _vCodecContext;
        private  AVFrame* _vFrame;
        private  AVFrame* _vHWDecodedFrame;
        private  SwsContext* _vSwsContext;
        private int _vbufferSize;
        private  IntPtr _vBuffer;
        private  byte_ptr4 _vTargetData;
        private  int4 _vTargetLinesize;
        private  int _vTargetDataSize;

        private  int _aStreamIndex;
        private  AVCodecContext* _aCodecContext;
        private  AVFrame* _aFrame;
        private  AVFrame* _aHWDecodedFrame;
        private  int _aBitsPerSample;
        private  SwrContext* _aSwrContext;
        private  IntPtr _aBuffer;
        private  byte* _aBufferPtr;
        private  byte** _aSourceData = null;
        private  byte** _aDestinationData = null;

        private Models.MediaState _mediaState = Models.MediaState.None;
        private long _duration;
        private long _bitrate;
        private string _vCodecName;
        private int _vWidth;
        private int _vHeight;
        private AVPixelFormat _vPixelFormat;
        private AVPixelFormat _vConvertedPixelFormat;
        private AVRational _vTimeBase;
        private AVRational _vFrameRate;
        private bool _hasAudio;
        private AVRational _aTimeBase;
        private string _aCodecName;
        private int _aSampleRate;
        private int _aChannels;
        private ulong _aChannelLayout;
        private AVSampleFormat _aSampleFmt;
        private AVSampleFormat _aTargetSampleFmt;
        private string _errorMessage = string.Empty;
        private DateTime _startPlayingTime = DateTime.MinValue;
        private TimeSpan _audioClock = TimeSpan.Zero;
        private bool _isReadEnd = false;
        private bool _isFirstPlay = true;
        private System.Collections.Concurrent.ConcurrentQueue<Models.VideoFrame> _vFrames = new System.Collections.Concurrent.ConcurrentQueue<Models.VideoFrame>();
        private System.Collections.Concurrent.ConcurrentQueue<Models.AudioFrame> _aFrames = new System.Collections.Concurrent.ConcurrentQueue<Models.AudioFrame>();
        private bool _isDisposed = false;
        private bool isLiveStream = false;


        #endregion

        #region  -- property --
        public Models.MediaState MediaState { get { return _mediaState; } }
        public string Url { get; }
        public AVHWDeviceType HWDeviceType { get; }
        public int PictureQuality { get; }
        public long Duration { get { return _duration; } }
        public TimeSpan CurrentPosition { get { return GetReferenceClock(); } }
        public long Bitrate { get { return _bitrate; } }
        public string V_CodecName { get { return _vCodecName; } }
        public int V_Width { get { return _vWidth; } }
        public int V_Height { get { return _vHeight; } }
        public AVPixelFormat V_PixelFormat { get { return _vPixelFormat; } }
        public AVPixelFormat V_ConvertedPixelFormat { get { return _vConvertedPixelFormat; } }
        public AVRational V_TimeBase { get { return _vTimeBase; } }
        public AVRational V_FrameRate { get { return _vFrameRate; } }
        public bool HasAudio { get { return _hasAudio; } }
        public AVRational A_TimeBase { get { return _aTimeBase; } }
        public string A_CodecName { get { return _aCodecName; } }
        public int A_SampleRate { get { return _aSampleRate; } }
        public int A_Channels { get { return _aChannels; } }
        public ulong A_ChannelLayout { get { return _aChannelLayout; } }
        public AVSampleFormat A_SampleFmt { get { return _aSampleFmt; } }
        public AVSampleFormat A_ConvertedSampleFmt { get { return _aTargetSampleFmt; } }
        public long A_BitsPerSample { get { return _aBitsPerSample; } }
        public string ErrorMessage { get { return _errorMessage; } }
        #endregion

        public event Action<Models.AudioFrame> OnAudioPlay;
        public event Action<Models.VideoFrame> OnVideoPlay;
        public event Action<Models.MediaState> OnStateChange;
        public event Action<TimeSpan> OnTimeUpdate;

        /// <summary>
        /// Wraps a media file and provides playback associated callbacks
        /// </summary>
        /// <param name="url"></param>
        /// <param name="HWDeviceType"></param>
        /// <param name="usePictureQuality">0-100</param>
        public Media(string url, AVHWDeviceType HWDeviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE, int usePictureQuality = 80)
        {
            this.Url = url;
            this.HWDeviceType = HWDeviceType;
            if (usePictureQuality < 0)
            {
                usePictureQuality = 0;
            }
            if (usePictureQuality > 100)
            {
                usePictureQuality = 100;
            }
            this.PictureQuality = usePictureQuality;
            LoadMedia();
        }
        private int LoadMedia()
        {
            _isFirstPlay = true;
            int error = 0;
            _avFormatContext = ffmpeg.avformat_alloc_context();
            var tempFormat = _avFormatContext;
            error = ffmpeg.avformat_open_input(&tempFormat, this.Url, null, null);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "avformat_open_input:" + av_strerror(error);
                return error;
            }
            error = ffmpeg.avformat_find_stream_info(_avFormatContext, null);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "avformat_find_stream_info:" + av_strerror(error);
                return error;
            }

            _duration = _avFormatContext->duration;
            _bitrate = _avFormatContext->bit_rate;

            #region -- video --
            AVCodec* vcodec = null;
            _vStreamIndex = error = ffmpeg.av_find_best_stream(_avFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &vcodec, 0);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "av_find_best_stream:" + av_strerror(error);
                return error;
            }
            _vCodecContext = ffmpeg.avcodec_alloc_context3(vcodec);
            if (HWDeviceType != AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
            {
                error = ffmpeg.av_hwdevice_ctx_create(&_vCodecContext->hw_device_ctx, HWDeviceType, null, null, 0);
                if (error < 0)
                {
                    _mediaState = Models.MediaState.Error;
                    _errorMessage = "av_hwdevice_ctx_create:" + av_strerror(error);
                    return error;
                }
                _vHWDecodedFrame = ffmpeg.av_frame_alloc();
            }
            error = ffmpeg.avcodec_parameters_to_context(_vCodecContext, _avFormatContext->streams[_vStreamIndex]->codecpar);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "avcodec_parameters_to_context:" + av_strerror(error);
                return error;
            }
            error = ffmpeg.avcodec_open2(_vCodecContext, vcodec, null);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "avcodec_open2:" + av_strerror(error);
                return error;
            }
            _pPacket = ffmpeg.av_packet_alloc();
            _aHWDecodedFrame = ffmpeg.av_frame_alloc();
            _vFrame = ffmpeg.av_frame_alloc();
            var vstream = _avFormatContext->streams[_vStreamIndex];
            _vCodecName = ffmpeg.avcodec_get_name(vcodec->id);
            _vWidth = _vCodecContext->width;
            _vHeight = _vCodecContext->height;
            _vPixelFormat = _vCodecContext->pix_fmt;
            _vTimeBase = _avFormatContext->streams[_vStreamIndex]->time_base;
            _vFrameRate = _avFormatContext->streams[_vStreamIndex]->r_frame_rate;
            _vConvertedPixelFormat = AVPixelFormat.AV_PIX_FMT_BGR24;

            _vSwsContext = ffmpeg.sws_getContext(V_Width, V_Height, V_PixelFormat, V_Width, V_Height, V_ConvertedPixelFormat, ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            _vbufferSize = ffmpeg.av_image_get_buffer_size(V_ConvertedPixelFormat, V_Width, V_Height, 1);
            _vBuffer = Marshal.AllocHGlobal(_vbufferSize);
            _vTargetData = new byte_ptr4();
            _vTargetLinesize = new int4();
            _vTargetDataSize = error = ffmpeg.av_image_fill_arrays(ref _vTargetData, ref _vTargetLinesize, (byte*)_vBuffer, V_ConvertedPixelFormat, V_Width, V_Height, 1);
            if (error < 0)
            {
                _mediaState = Models.MediaState.Error;
                _errorMessage = "av_image_fill_arrays:" + av_strerror(error);
                return error;
            }
            #endregion

            AVCodec* codec;
            _aStreamIndex = ffmpeg.av_find_best_stream(_avFormatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &codec, 0);
            _hasAudio = _aStreamIndex > -1;
            if (_hasAudio)
            {
                _aTargetSampleFmt = AVSampleFormat.AV_SAMPLE_FMT_S16;
                var audioStream = _avFormatContext->streams[_aStreamIndex];
                _aCodecContext = ffmpeg.avcodec_alloc_context3(codec);
                error = ffmpeg.avcodec_parameters_to_context(_aCodecContext, audioStream->codecpar);
                if (error < 0)
                {
                    _mediaState = Models.MediaState.Error;
                    _errorMessage = "avcodec_parameters_to_context:" + av_strerror(error);
                    return error;
                }
                error = ffmpeg.avcodec_open2(_aCodecContext, codec, null);
                if (error < 0)
                {
                    _mediaState = Models.MediaState.Error;
                    _errorMessage = "avcodec_open2:" + av_strerror(error);
                    return error;
                }
                _aTimeBase = audioStream->time_base;
                _aCodecName = ffmpeg.avcodec_get_name(codec->id);
                _aChannels = _aCodecContext->ch_layout.nb_channels;
                _aChannelLayout = _aCodecContext->channel_layout;
                _aSampleRate = _aCodecContext->sample_rate;
                _aSampleFmt = _aCodecContext->sample_fmt;
                _aBitsPerSample = error = ffmpeg.av_samples_get_buffer_size(null, 2, _aCodecContext->frame_size, A_ConvertedSampleFmt, 1);
                if (error < 0)
                {
                    _mediaState = Models.MediaState.Error;
                    _errorMessage = "av_samples_get_buffer_size:" + av_strerror(error);
                    return error;
                }
                _aBuffer = Marshal.AllocHGlobal((int)_aBitsPerSample);
                _aBufferPtr = (byte*)_aBuffer;

                _aSwrContext = ffmpeg.swr_alloc();
                ResamplingAudioConvertOpt(_aSwrContext, (int)A_ChannelLayout, A_ConvertedSampleFmt, (int)A_SampleRate, (int)A_ChannelLayout, A_SampleFmt, (int)A_SampleRate);
                ffmpeg.swr_init(_aSwrContext);
                _aFrame = ffmpeg.av_frame_alloc();
            }

            _mediaState = Models.MediaState.Ready;
            return 0;
        }
        private string av_strerror(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }
        private void ResamplingAudioConvertOpt(SwrContext* _context, long out_channel_layout, AVSampleFormat out_sample_fmt, int out_sample_rate, long in_channel_layout, AVSampleFormat in_sample_fmt, int in_sample_rate)
        {
            ffmpeg.av_opt_set_int(_context, "in_channel_layout", in_channel_layout, 0);
            ffmpeg.av_opt_set_int(_context, "in_sample_rate", in_sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(_context, "in_sample_fmt", in_sample_fmt, 0);

            ffmpeg.av_opt_set_int(_context, "out_channel_layout", out_channel_layout, 0);
            ffmpeg.av_opt_set_int(_context, "out_sample_rate", out_sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(_context, "out_sample_fmt", out_sample_fmt, 0);
        }
        private byte[] VideoFrameToJpegBytes(AVFrame* vFrame)
        {
            ffmpeg.sws_scale(_vSwsContext, vFrame->data, vFrame->linesize, 0, vFrame->height, _vTargetData, _vTargetLinesize);
            byte[] reval = null;
            using (SkiaSharp.SKBitmap bitmap = new SkiaSharp.SKBitmap(vFrame->width, vFrame->height))
            {
                SKColor[]? pixels = new SKColor[vFrame->width * vFrame->height];
                int bIndex = 0;
                int pIndex = 0;
                for (int row = 0; row < vFrame->height; row++)
                {
                    for (int col = 0; col < vFrame->width; col++)
                    {
                        //layout:BGR
                        byte b = Marshal.ReadByte(_vBuffer, bIndex + 2);
                        byte g = Marshal.ReadByte(_vBuffer, bIndex + 1);
                        byte r = Marshal.ReadByte(_vBuffer, bIndex);
                        pixels[pIndex] = new SKColor(b, g, r);
                        bIndex += 3;
                        pIndex++;
                    }
                }
                bitmap.Pixels = pixels;
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Encode(ms, SKEncodedImageFormat.Jpeg, PictureQuality);
                    reval = ms.ToArray();
                }
                pixels = null;
            }
            return reval;
        }
        private byte[] AudioFrameToBytes(AVFrame* audioFrame)
        {
            var tempBufferPtr = _aBufferPtr;
            var outputSamplesPerChannel = ffmpeg.swr_convert(_aSwrContext, &tempBufferPtr, _aFrame->nb_samples, audioFrame->extended_data, audioFrame->nb_samples);
            var outPutBufferLength = ffmpeg.av_samples_get_buffer_size(null, 2, outputSamplesPerChannel, A_ConvertedSampleFmt, 0);
            if (outputSamplesPerChannel < 0)
            {
                return null;
            }
            byte[] bytes = new byte[outPutBufferLength];
            Marshal.Copy(_aBuffer, bytes, 0, bytes.Length);
            return bytes;
        }
        private void StartReadFrame()
        {
            Task.Run(() =>
            {
                while (_mediaState == Models.MediaState.Playing)
                {
                    if (_aFrames.Count > 1 && _vFrames.Count > 1)
                    {
                        Thread.Sleep(6);
                    }
                    int error;
                    AVFrame frame;

                    error = ffmpeg.av_read_frame(_avFormatContext, _pPacket);
                    if (error == ffmpeg.AVERROR_EOF)
                    {
                        _isReadEnd = true;
                        break;
                    }
                    int psi = _pPacket->stream_index;
                    long packetDuration = 0;
                    try
                    {
                        if (psi == _vStreamIndex)
                        {
                            ffmpeg.av_frame_unref(_vFrame);
                            ffmpeg.avcodec_send_packet(_vCodecContext, _pPacket);
                        }
                        else if (psi == _aStreamIndex)
                        {
                            ffmpeg.av_frame_unref(_aFrame);
                            ffmpeg.avcodec_send_packet(_aCodecContext, _pPacket);
                        }
                        //Wrong avframe.duration value on 32-bit apps,
                        //use and publish 64-bit apps if not absolutely necessary
                        packetDuration = _pPacket->duration;
                    }
                    finally
                    {
                        ffmpeg.av_packet_unref(_pPacket);
                    }
                    if (psi == _vStreamIndex)
                    {
                        error = ffmpeg.avcodec_receive_frame(_vCodecContext, _vFrame);
                        if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        {
                            continue;
                        }
                        if (_vCodecContext->hw_device_ctx != null)
                        {
                            ffmpeg.av_frame_unref(_vHWDecodedFrame);
                            ffmpeg.av_hwframe_transfer_data(_aHWDecodedFrame, _vFrame, 0);
                            frame = *_vHWDecodedFrame;
                        }
                        else
                        {
                            frame = *_vFrame;
                        }
                        var bytes = VideoFrameToJpegBytes(&frame);
                        _vFrames.Enqueue(new Models.VideoFrame(V_TimeBase, packetDuration, frame.pts, frame.pkt_dts, bytes, frame.width, frame.height, V_ConvertedPixelFormat));

                    }
                    else if (psi == _aStreamIndex)
                    {
                        error = ffmpeg.avcodec_receive_frame(_aCodecContext, _aFrame);
                        if (error == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                        {
                            continue;
                        }
                        if (_aCodecContext->hw_device_ctx != null)
                        {
                            ffmpeg.av_frame_unref(_aHWDecodedFrame);
                            ffmpeg.av_hwframe_transfer_data(_aHWDecodedFrame, _aFrame, 0);
                            frame = *_aHWDecodedFrame;
                        }
                        else
                        {
                            frame = *_aFrame;
                        }
                        byte[] bytes = AudioFrameToBytes(&frame);
                        if (bytes != null)
                        {
                            _aFrames.Enqueue(new(A_TimeBase, packetDuration, frame.pts, frame.pkt_dts, bytes, frame.ch_layout.nb_channels, frame.ch_layout, frame.sample_rate, A_ConvertedSampleFmt));
                        }
                    }
                }
            });
        }

        #region -- Triggering event --
        private TimeSpan OnStateChangeTrigger()
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (OnStateChange != null)
            {
                try
                {
                    OnStateChange(_mediaState);
                }
                catch { }
            }
            sw.Stop();
            return sw.Elapsed;
        }
        private TimeSpan OnAudioPlayTrigger(Models.AudioFrame audio)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (OnAudioPlay != null)
            {
                try
                {
                    OnAudioPlay(audio);
                }
                catch { }
            }
            sw.Stop();
            return sw.Elapsed;
        }
        private TimeSpan OnVideoPlayTrigger(Models.VideoFrame video)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (OnVideoPlay != null)
            {
                try
                {
                    OnVideoPlay(video);
                }
                catch { }
            }
            sw.Stop();
            return sw.Elapsed;
        }
        private TimeSpan OnTimeUpdateTrigger(TimeSpan ts)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (OnTimeUpdate != null)
            {
                try
                {
                    OnTimeUpdate(ts);
                }
                catch { }
            }
            sw.Stop();
            return sw.Elapsed;
        }
        #endregion

        private TimeSpan GetReferenceClock()
        {
            if (HasAudio)
            {
                return _audioClock;
            }
            return (DateTime.Now - _startPlayingTime);
        }
        private void StartPlaying()
        {
            if (_mediaState == Models.MediaState.Error)
            {
                OnStateChangeTrigger();
                return;
            }
            _startPlayingTime = DateTime.Now;
            Task.Run(() =>
            {
                if (!HasAudio)
                {
                    OnStateChangeTrigger();
                }
                while (_mediaState == Models.MediaState.Playing)
                {
                    #region -- loop --
                    Models.VideoFrame frame;
                    if (!_vFrames.TryDequeue(out frame))
                    {
                        if (_isReadEnd)
                        {
                            if (!HasAudio)
                            {
                                _mediaState = Models.MediaState.End;
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (_isFirstPlay)
                    {
                        if (!HasAudio)
                        {
                            _isFirstPlay = false;
                            OnStateChangeTrigger();
                            _startPlayingTime = DateTime.Now;
                        }
                    }

                    var frameDuration = TimeSpan.FromSeconds(frame.Duration * ffmpeg.av_q2d(frame.TimeBase));
                    var delay = TimeSpan.Zero;
                    var refClock = GetReferenceClock();
                    if (frame.Time < refClock)
                    {
                        if ((refClock - frame.Time).TotalMilliseconds > 500)
                        {
                            continue;//ignore
                        }
                    }
                    else if (frame.Time > refClock)
                    {
                        delay = frame.Time - refClock;
                        if (delay.TotalMilliseconds > 500)
                        {
                            continue;//ignore
                        }
                    }
                    if (delay > TimeSpan.Zero)
                    {
                        Thread.Sleep(delay);//delay show
                    }
                    if (!HasAudio)
                    {
                        _audioClock = frame.Time;
                    }
                    var playTimeCost = OnVideoPlayTrigger(frame);
                    if (!HasAudio)
                    {
                        playTimeCost += OnTimeUpdateTrigger(_audioClock);
                    }
                    if (frameDuration > playTimeCost)
                    {
                        delay = frameDuration - playTimeCost;
                    }
                    else
                    {
                        delay = TimeSpan.Zero;
                    }
                    if (delay > TimeSpan.Zero)
                    {
                        Thread.Sleep(delay);//delay read next
                    }
                    #endregion
                }
                if (!HasAudio)
                {
                    OnStateChangeTrigger();
                }
            });
            if (HasAudio)
            {
                Task.Run(() => {

                    OnStateChangeTrigger();
                    //Audio: no any time reference
                    while (_mediaState == Models.MediaState.Playing)
                    {
                        #region -- loop --
                        Models.AudioFrame frame;
                        DateTime dateTime = DateTime.Now;
                        if (!_aFrames.TryDequeue(out frame))
                        {
                            if (_isReadEnd)
                            {
                                _mediaState = Models.MediaState.End;
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        _audioClock = frame.Time;
                        //Add 10 milliseconds advance value
                        var readTimeCost = (DateTime.Now.AddMilliseconds(10) - dateTime);
                        if (_isFirstPlay)
                        {
                            _isFirstPlay = false;
                            _startPlayingTime = DateTime.Now;
                        }
                        var frameDuration = TimeSpan.FromSeconds(frame.Duration * ffmpeg.av_q2d(frame.TimeBase));
                        var delay = TimeSpan.Zero;
                        //callback
                        var playTimeCost = OnAudioPlayTrigger(frame);
                        playTimeCost += OnTimeUpdateTrigger(_audioClock);
                        if (playTimeCost > TimeSpan.Zero)
                        {
                            if (playTimeCost < frameDuration)
                            {
                                delay = frameDuration - playTimeCost;
                            }
                        }
                        if (delay > TimeSpan.Zero)
                        {
                            if (delay > readTimeCost)
                            {
                                delay = delay - readTimeCost;
                            }
                            Thread.Sleep(delay);//delay: next
                        }
                        #endregion
                    }
                    OnStateChangeTrigger();
                });
            }
        }
        private bool CheckErrorState()
        {
            bool reval = _mediaState == Models.MediaState.Error || _mediaState == Models.MediaState.None;
            if (reval)
            {
                OnStateChangeTrigger();
            }
            return reval;
        }
        private void FreeResources()
        {
            try
            {
                var frame = _vFrame;
                ffmpeg.av_frame_free(&frame);
                if (_vHWDecodedFrame != null)
                {
                    frame = _vHWDecodedFrame;
                    ffmpeg.av_frame_free(&frame);
                }
                var packet = _pPacket;
                ffmpeg.av_packet_free(&packet);
                ffmpeg.avcodec_close(_vCodecContext);
                Marshal.FreeHGlobal(_vBuffer);
                ffmpeg.sws_freeContext(_vSwsContext);

                if (HasAudio)
                {
                    frame = _aFrame;
                    ffmpeg.av_frame_free(&frame);
                    ffmpeg.avcodec_close(_aCodecContext);
                    if (_aHWDecodedFrame != null)
                    {
                        frame = _aHWDecodedFrame;
                        ffmpeg.av_frame_free(&frame);
                    }
                    Marshal.FreeHGlobal(_aBuffer);
                }

                var avFormatContext = _avFormatContext;
                ffmpeg.avformat_close_input(&avFormatContext);
            }
            catch { }
        }

        #region -- play ctrl --
        public void Play()
        {
            if (CheckErrorState()) { return; }
            if (_mediaState == Models.MediaState.Playing) { return; }
            bool flag = (_mediaState == Models.MediaState.End || _mediaState == Models.MediaState.Stop);
            _mediaState = Models.MediaState.Playing;
            if (flag)
            {
                FreeResources();
                LoadMedia();//MediaState.Ready
                _mediaState = Models.MediaState.Playing;
            }
            StartReadFrame();
            StartPlaying();
        }
        public void Stop()
        {
            if (CheckErrorState()) { return; }
            if (_mediaState == Models.MediaState.Stop) { return; }
            _mediaState = Models.MediaState.Stop;
            _aFrames.Clear();
            _vFrames.Clear();
        }
        public void Pause()
        {
            if (CheckErrorState()) { return; }
            if (_mediaState == Models.MediaState.Pause) { return; }
            if(_mediaState != Models.MediaState.Playing) { return; }
            _mediaState = Models.MediaState.Pause;
        }
        public void JumpToSeconds(long seconds)
        {
            if (CheckErrorState()) { return; }
            var pts = seconds * ffmpeg.AV_TIME_BASE;
            if (isLiveStream || pts < 0 || pts > Duration) { return; }
            int ret = ffmpeg.av_seek_frame(_avFormatContext, -1, pts, ffmpeg.AVSEEK_FLAG_BACKWARD);
            if (ret != 0) { return; }
            _aFrames.Clear();
            _vFrames.Clear();
            _startPlayingTime = DateTime.Now;
            _isFirstPlay = true;
            _isReadEnd = false;
        }
        #endregion

        public IReadOnlyDictionary<string, string> GetContextInfo()
        {
            AVDictionaryEntry* tag = null;
            var result = new Dictionary<string, string>();

            while ((tag = ffmpeg.av_dict_get(_avFormatContext->metadata, "", tag, ffmpeg.AV_DICT_IGNORE_SUFFIX)) != null)
            {
                var key = Marshal.PtrToStringAnsi((IntPtr)tag->key);
                var value = Marshal.PtrToStringAnsi((IntPtr)tag->value);
                if(!string.IsNullOrEmpty(key))
                {
                    if(string.IsNullOrEmpty(value))
                    {
                        value = string.Empty;
                    }
                    result.Add(key, value);
                }
            }

            return result;
        }
        public void Dispose() {

            if (_isDisposed) { return; }
            _isDisposed = true;
            _mediaState = Models.MediaState.None;
            FreeResources();
        }

    }
}
