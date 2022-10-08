using Microsoft.JSInterop;
namespace MediaFilesAPI;

public class MediaFiles : JSModule
{
    public MediaFiles(IJSRuntime js)
        : base(js, "./_content/MediaFilesAPI/mediaFiles.js")
    {
    }

    public async ValueTask<JSDirectory> ShowDirectoryPicker()
        => await InvokeAsync<JSDirectory>("showDirectoryPicker");

    public async ValueTask<JSDirectory> ReopenLastDirectory()
        => await InvokeAsync<JSDirectory>("reopenLastDirectory");

    public async ValueTask<JSFile[]> GetFilesAsync(JSDirectory directory)
        => await InvokeAsync<JSFile[]>("getFiles", directory.Instance);

    public async ValueTask<byte[]> DecodeAudioFileAsync(JSFile file)
        => await InvokeAsync<byte[]>("decodeAudioFile", file.Name);

    public async ValueTask<IJSObjectReference> PlayAudioFileAsync(JSFile file)
        => await InvokeAsync<IJSObjectReference>("playAudioFile", file.Name);

    public async ValueTask<IJSObjectReference> PlayAudioDataAsync(byte[] data)
        => await InvokeAsync<IJSObjectReference>("playAudioData", data);

    public record JSDirectory(string Name, IJSObjectReference Instance) : IAsyncDisposable
    {
        // When .NET is done with this JSDirectory, also release the underlying JS object
        public ValueTask DisposeAsync() => Instance.DisposeAsync();
    }
    public record JSFile(string Name, long Size, DateTime LastModified);
}
