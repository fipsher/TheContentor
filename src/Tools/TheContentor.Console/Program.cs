using YoutubeDLSharp;
using YoutubeDLSharp.Options;


var ytdl = new YoutubeDL
{
    YoutubeDLPath = "/opt/homebrew/bin/yt-dlp",
    FFmpegPath = "/opt/homebrew/bin/ffmpeg",
    OutputFolder = AppDomain.CurrentDomain.BaseDirectory,
};

var options = new OptionSet
{
    Format = "bestvideo",
    WriteInfoJson = true,
};

var res = await ytdl.RunVideoDownload("https://www.youtube.com/watch?v=u7kdVe8q5zs", overrideOptions: options);


// this is a path to the file. don't forget to clean it up. webm extension
string path = res.Data;

Console.ReadLine();
