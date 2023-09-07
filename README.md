# Introduction
On the windows operating system, call FFmpeg to parse video files through C#, and then provide playback callbacks for audio and video.
## Dependencies
.NET-6.0 [FFmpeg-6.0](https://ffmpeg.org/download.html) [FFmpeg.AutoGen-6.0](https://github.com/Ruslan-B/FFmpeg.AutoGen) [SkiaSharp-2.88.5](https://github.com/mono/SkiaSharp)
## Example
```C#
//Frist: RegisterFFmpegBinaries
ZeroPlayer.FFmpegHelper.RegisterFFmpegBinaries();

private ZeroPlayer.Media? media = null;
private void Play(string url)
{
    if(media != null){
        media.Dispose();
        media=null;
    }
    media = new ZeroPlayer.Media(url, AVHWDeviceType.AV_HWDEVICE_TYPE_NONE,100);
    if (media.MediaState != ZeroPlayer.Models.MediaState.Ready)
    {
        _ShowMsg(media.ErrorMessage);
        media.Dispose();
        media = null;
        return;
    }
    media.OnStateChange += Media_OnStateChange;
    media.OnAudioPlay += Media_OnAudioPlay;
    media.OnVideoPlay += Media_OnVideoPlay;
    media.OnTimeUpdate += Media_OnTimeUpdate;

    media.Play();
}
private void Media_OnTimeUpdate(TimeSpan obj)
{
    //string timeStr = obj.ToString(@"hh\:mm\:ss");
}
private void Media_OnStateChange(ZeroPlayer.Models.MediaState obj)
{
    //string stateStr = obj.ToString();
}
private void Media_OnAudioPlay(ZeroPlayer.Models.AudioFrame obj)
{
    //This obj.Data is a 16-bit PCM byte array
    //myAudioOutDevice.Load(obj.Data);
}
private void Media_OnVideoPlay(ZeroPlayer.Models.VideoFrame obj)
{
    //This obj.Data is a byte array of images in jpeg format
    //myPicture.LoadBytes(obj.Data);
}
```
For a more detailed example, please see the [ZeroPlayerTest](https://github.com/13039520/NetFFmpegVideoPlayer/blob/main/ZeroPlayerTest/Form_Main.cs) project.
