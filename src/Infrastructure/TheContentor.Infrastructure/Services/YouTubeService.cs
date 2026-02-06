using TheContentor.Infrastructure.Interfaces;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json; // For parsing yt-dlp's JSON output

namespace TheContentor.Infrastructure.Services;

public class YouTubeService : IYouTubeService
{
    private readonly string _ytDlpPath = "yt-dlp"; // Assumes yt-dlp is in PATH

    public async Task<bool> IsValidYouTubeUrlAsync(string url)
    {
        // Basic regex validation first for quick checks
        if (!Regex.IsMatch(url, @"^(https?://)?(www\.)?(youtube\.com|youtu\.be)/.+$"))
        {
            return false;
        }

        // Use yt-dlp to verify if the URL points to an actual video
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"--flat-playlist --dump-single-json --skip-download --geo-bypass \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) return false;

                // Read output to prevent deadlock
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                // If yt-dlp exits with 0, it means it successfully recognized the video
                return process.ExitCode == 0;
            }
        }
        catch (Exception ex)
        {
            // Log the exception (e.g., yt-dlp not found, execution error)
            Console.WriteLine($"Error during YouTube URL validation with yt-dlp: {ex.Message}");
            return false;
        }
    }

    public async Task<(TimeSpan Duration, int Width, int Height, DateTime UploadDate, string OriginalUrl, string Title)?> GetVideoMetadataAsync(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"--dump-json --no-warnings --geo-bypass \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null) return null;

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync(); // Read error stream to prevent deadlock
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine($"yt-dlp metadata extraction failed: {error}");
                    return null;
                }

                // Parse JSON output from yt-dlp
                dynamic? json = JsonConvert.DeserializeObject(output);

                if (json == null) return null;

                // Extract relevant metadata
                TimeSpan duration = TimeSpan.FromSeconds((double)(json.duration ?? 0));
                var width = (int?)(json.width ?? 0);
                var height = (int?)(json.height ?? 0);
                // yt-dlp's upload_date is YYYYMMDD
                DateTime uploadDate = DateTime.ParseExact((string)json.upload_date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                string originalUrl = (string)(json.webpage_url ?? url);
                string title = (string)(json.title ?? "Untitled Video");

                return (duration, width ?? 0, height ?? 0, uploadDate, originalUrl, title);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during YouTube metadata extraction with yt-dlp: {ex.Message}");
            return null;
        }
    }

    public async Task<Stream?> DownloadVideoStreamAsync(string url)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ytDlpPath,
                Arguments = $"-f bestvideo[ext=mp4] --no-warnings --geo-bypass -o - \"{url}\"", // -o - means output to stdout
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var process = Process.Start(startInfo);
            if (process == null) return null;

            // Immediately start reading the error stream to prevent deadlocks
            var errorReadingTask = process.StandardError.ReadToEndAsync();

            // Return the StandardOutput stream directly
            // Note: Caller is responsible for disposing the process and its streams
            // Consider using a wrapper that manages process lifetime for robustness
            var outputStream = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(outputStream);
            outputStream.Position = 0; // Reset position for reading
            
            await process.WaitForExitAsync();
            var errorOutput = await errorReadingTask;

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"yt-dlp video download failed: {errorOutput}");
                return null;
            }

            return outputStream;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during YouTube video download with yt-dlp: {ex.Message}");
            return null;
        }
    }
}
