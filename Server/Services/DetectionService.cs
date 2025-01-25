using Grpc.Core;
using System.Diagnostics;

public class DetectionServiceImpl : DetectionService.DetectionServiceBase
{
    private readonly Dictionary<string, Process> ffmpegProcesses = new();
    private readonly Dictionary<string, StreamWriter> ffmpegInputWriters = new();

    public override async Task<DetectionResponse> StreamDetections(IAsyncStreamReader<DetectionRequest> requestStream, ServerCallContext context)
    {
        string? source = null;
        try
        {
            await foreach (var request in requestStream.ReadAllAsync())
            {
                source ??= request.Source;

                if (!ffmpegProcesses.ContainsKey(source))
                {
                    StartFFmpegProcess(source);
                }

                if (ffmpegInputWriters.TryGetValue(source, out var writer))
                {
                    // writer.BaseStream.Write(request.Image);
                    writer.BaseStream.Write(request.Image.ToByteArray());
                    writer.BaseStream.Flush();
                }
            }

            return new DetectionResponse
            {
                Status = $"Stream from {source} processed successfully."
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing stream from {source}: {ex.Message}");
            return new DetectionResponse
            {
                Status = $"Error processing stream from {source}."
            };
        }
    }

    private void StartFFmpegProcess(string source)
    {
        string hlsOutputPath = $"./hls/{source}/stream.m3u8";
        Directory.CreateDirectory($"./hls/{source}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -f mjpeg -i pipe:0 -c:v libx264 -preset ultrafast -hls_time 2 -hls_playlist_type event {hlsOutputPath}",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        ffmpegProcesses[source] = process;
        ffmpegInputWriters[source] = new StreamWriter(process.StandardInput.BaseStream);

        Console.WriteLine($"Started FFmpeg process for source: {source}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var process in ffmpegProcesses.Values)
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        return Task.CompletedTask;
    }
}
