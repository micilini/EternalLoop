using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EternalLoop.App.Services;
using FluentAssertions;

namespace EternalLoop.App.Tests.Services;

public sealed class TrackArtworkServiceTests
{
    [Fact]
    public async Task TryLoadArtwork_ReturnsImage_WhenMp3ContainsId3V23ApicFrontCover()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempFile(CreateMp3File(CreateId3V23TagWithApic(CreatePngBytes(Colors.Red))));
            try
            {
                var service = new TrackArtworkService();

                ImageSource? image = service.TryLoadArtwork(path);

                image.Should().NotBeNull();
                ((Freezable)image!).IsFrozen.Should().BeTrue();
                AssertPixelColor(image, Colors.Red);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsImage_WhenMp3ContainsId3V24ApicFrontCover()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempFile(CreateMp3File(CreateId3V24TagWithApic(CreatePngBytes(Colors.Blue))));
            try
            {
                var service = new TrackArtworkService();

                ImageSource? image = service.TryLoadArtwork(path);

                image.Should().NotBeNull();
                ((Freezable)image!).IsFrozen.Should().BeTrue();
                AssertPixelColor(image, Colors.Blue);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_PrefersFrontCover_WhenMultipleApicFramesExist()
    {
        await RunStaAsync(() =>
        {
            byte[] first = CreatePngBytes(Colors.Green);
            byte[] frontCover = CreatePngBytes(Colors.Yellow);
            string path = WriteTempFile(CreateMp3File(
                CreateId3V23Tag(
                    CreateApicFrameV23(first, pictureType: 1, mime: "image/png"),
                    CreateApicFrameV23(frontCover, pictureType: 3, mime: "image/png"))));
            try
            {
                var service = new TrackArtworkService();

                ImageSource? image = service.TryLoadArtwork(path);

                image.Should().NotBeNull();
                AssertPixelColor(image!, Colors.Yellow);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public void TryLoadArtwork_ReturnsNull_WhenFileDoesNotExist()
    {
        var service = new TrackArtworkService();

        service.TryLoadArtwork(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp3"))
            .Should()
            .BeNull();
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsNull_WhenFileHasNoId3Tag()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempFile(Encoding.ASCII.GetBytes("NO TAG"));
            try
            {
                var service = new TrackArtworkService();

                service.TryLoadArtwork(path).Should().BeNull();
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsNull_WhenApicImageBytesAreInvalid()
    {
        await RunStaAsync(() =>
        {
            byte[] validPng = CreatePngBytes(Colors.Purple);
            byte[] invalidPng = validPng[..Math.Min(validPng.Length, 12)];
            string path = WriteTempFile(CreateMp3File(CreateId3V23TagWithApic(invalidPng)));
            try
            {
                var service = new TrackArtworkService();

                service.TryLoadArtwork(path).Should().BeNull();
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_DoesNotLockAudioFile_AfterLoadingArtwork()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempFile(CreateMp3File(CreateId3V23TagWithApic(CreatePngBytes(Colors.Orange))));
            try
            {
                var service = new TrackArtworkService();

                service.TryLoadArtwork(path).Should().NotBeNull();

                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                }

                File.Delete(path);
                File.Exists(path).Should().BeFalse();
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsImage_WhenM4aContainsCovrJpeg()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempM4aFile(CreateM4aFileWithCovr(CreatePngBytes(Colors.Red), 13));
            try
            {
                var service = new TrackArtworkService();
                ImageSource? image = service.TryLoadArtwork(path);
                image.Should().NotBeNull();
                AssertPixelColor(image!, Colors.Red);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsImage_WhenM4aContainsCovrPng()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempM4aFile(CreateM4aFileWithCovr(CreatePngBytes(Colors.Blue), 14));
            try
            {
                var service = new TrackArtworkService();
                ImageSource? image = service.TryLoadArtwork(path);
                image.Should().NotBeNull();
                AssertPixelColor(image!, Colors.Blue);
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsNull_WhenM4aHasNoCovr()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempM4aFile(CreateM4aFile());
            try
            {
                var service = new TrackArtworkService();
                service.TryLoadArtwork(path).Should().BeNull();
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_ReturnsNull_WhenM4aCovrImageBytesAreInvalid()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempM4aFile(CreateM4aFileWithCovr(new byte[] { 0x01, 0x02 }, 13));
            try
            {
                var service = new TrackArtworkService();
                service.TryLoadArtwork(path).Should().BeNull();
            }
            finally
            {
                File.Delete(path);
            }
        });
    }

    [Fact]
    public async Task TryLoadArtwork_DoesNotLockM4aFile_AfterLoadingArtwork()
    {
        await RunStaAsync(() =>
        {
            string path = WriteTempM4aFile(CreateM4aFileWithCovr(CreatePngBytes(Colors.Orange), 14));
            try
            {
                var service = new TrackArtworkService();
                service.TryLoadArtwork(path).Should().NotBeNull();
                
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                }
                
                File.Delete(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        });
    }

    // Helper methods for M4A tests

    private static byte[] CreateM4aFile()
    {
        return new byte[] { 0x00, 0x00, 0x00, 0x08, (byte)'f', (byte)'t', (byte)'y', (byte)'p' }; // Minimal file
    }

    private static byte[] CreateM4aFileWithCovr(byte[] imageBytes, int dataType)
    {
        // 1. Create data atom
        byte[] dataPayload = new byte[8 + imageBytes.Length];
        WriteBigEndian(dataType, dataPayload.AsSpan(0, 4));
        // dataPayload[4..8] is 0 (locale/reserved)
        Buffer.BlockCopy(imageBytes, 0, dataPayload, 8, imageBytes.Length);
        byte[] dataAtom = CreateAtom("data", dataPayload);

        // 2. Create covr atom
        byte[] covrAtom = CreateAtom("covr", dataAtom);

        // 3. Create ilst atom
        byte[] ilstAtom = CreateAtom("ilst", covrAtom);

        // 4. Create meta atom
        byte[] metaPayload = new byte[4 + ilstAtom.Length];
        // metaPayload[0..4] is 0 (version/flags)
        Buffer.BlockCopy(ilstAtom, 0, metaPayload, 4, ilstAtom.Length);
        byte[] metaAtom = CreateAtom("meta", metaPayload);

        // 5. Create udta atom
        byte[] udtaAtom = CreateAtom("udta", metaAtom);

        // 6. Create moov atom
        byte[] moovAtom = CreateAtom("moov", udtaAtom);

        return moovAtom;
    }

    private static byte[] CreateAtom(string type, byte[] content)
    {
        byte[] atom = new byte[8 + content.Length];
        WriteBigEndian(content.Length + 8, atom.AsSpan(0, 4));
        Encoding.ASCII.GetBytes(type).CopyTo(atom.AsSpan(4, 4));
        Buffer.BlockCopy(content, 0, atom, 8, content.Length);
        return atom;
    }

    private static string WriteTempM4aFile(byte[] content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"EternalLoop.Artwork.{Guid.NewGuid():N}.m4a");
        File.WriteAllBytes(path, content);
        return path;
    }

    private static byte[] CreateMp3File(byte[] tagBytes)
    {
        byte[] audioBytes = [0x11, 0x22, 0x33, 0x44];
        byte[] fileBytes = new byte[tagBytes.Length + audioBytes.Length];
        Buffer.BlockCopy(tagBytes, 0, fileBytes, 0, tagBytes.Length);
        Buffer.BlockCopy(audioBytes, 0, fileBytes, tagBytes.Length, audioBytes.Length);
        return fileBytes;
    }

    private static byte[] CreateId3V23TagWithApic(byte[] imageBytes, byte pictureType = 3, string mime = "image/png")
    {
        return CreateId3Tag(3, CreateApicFrameV23(imageBytes, pictureType, mime));
    }

    private static byte[] CreateId3V24TagWithApic(byte[] imageBytes, byte pictureType = 3, string mime = "image/png")
    {
        return CreateId3Tag(4, CreateApicFrameV24(imageBytes, pictureType, mime));
    }

    private static byte[] CreateId3V23Tag(params byte[][] frames)
    {
        return CreateId3Tag(3, frames);
    }

    private static byte[] CreateId3Tag(byte versionMajor, params byte[][] frames)
    {
        byte[] payload = Combine(frames);
        byte[] header = new byte[10];
        header[0] = (byte)'I';
        header[1] = (byte)'D';
        header[2] = (byte)'3';
        header[3] = versionMajor;
        header[4] = 0;
        header[5] = 0;
        WriteSynchsafe(payload.Length, header.AsSpan(6, 4));

        byte[] tag = new byte[header.Length + payload.Length];
        Buffer.BlockCopy(header, 0, tag, 0, header.Length);
        Buffer.BlockCopy(payload, 0, tag, header.Length, payload.Length);
        return tag;
    }

    private static byte[] CreateApicFrameV23(byte[] imageBytes, byte pictureType, string mime)
    {
        return CreateApicFrame(imageBytes, pictureType, mime, versionMajor: 3);
    }

    private static byte[] CreateApicFrameV24(byte[] imageBytes, byte pictureType, string mime)
    {
        return CreateApicFrame(imageBytes, pictureType, mime, versionMajor: 4);
    }

    private static byte[] CreateApicFrame(byte[] imageBytes, byte pictureType, string mime, byte versionMajor)
    {
        byte[] mimeBytes = Encoding.ASCII.GetBytes(mime);
        byte[] payload = new byte[1 + mimeBytes.Length + 1 + 1 + 1 + imageBytes.Length];
        int offset = 0;
        payload[offset++] = 0;
        Buffer.BlockCopy(mimeBytes, 0, payload, offset, mimeBytes.Length);
        offset += mimeBytes.Length;
        payload[offset++] = 0;
        payload[offset++] = pictureType;
        payload[offset++] = 0;
        Buffer.BlockCopy(imageBytes, 0, payload, offset, imageBytes.Length);

        byte[] frame = new byte[10 + payload.Length];
        frame[0] = (byte)'A';
        frame[1] = (byte)'P';
        frame[2] = (byte)'I';
        frame[3] = (byte)'C';
        if (versionMajor == 3)
        {
            WriteBigEndian(payload.Length, frame.AsSpan(4, 4));
        }
        else
        {
            WriteSynchsafe(payload.Length, frame.AsSpan(4, 4));
        }

        Buffer.BlockCopy(payload, 0, frame, 10, payload.Length);
        return frame;
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] combined = new byte[length];
        int offset = 0;

        foreach (byte[] array in arrays)
        {
            Buffer.BlockCopy(array, 0, combined, offset, array.Length);
            offset += array.Length;
        }

        return combined;
    }

    private static byte[] CreatePngBytes(Color color)
    {
        byte[] pixels = [color.B, color.G, color.R, color.A];
        BitmapSource source = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static void AssertPixelColor(ImageSource imageSource, Color expected)
    {
        BitmapSource bitmap = (BitmapSource)imageSource;
        byte[] pixels = new byte[4];
        bitmap.CopyPixels(pixels, 4, 0);

        pixels[0].Should().Be(expected.B);
        pixels[1].Should().Be(expected.G);
        pixels[2].Should().Be(expected.R);
        pixels[3].Should().Be(expected.A);
    }

    private static string WriteTempFile(byte[] content)
    {
        string path = Path.Combine(Path.GetTempPath(), $"EternalLoop.Artwork.{Guid.NewGuid():N}.mp3");
        File.WriteAllBytes(path, content);
        return path;
    }

    private static void WriteSynchsafe(int value, Span<byte> destination)
    {
        destination[0] = (byte)((value >> 21) & 0x7F);
        destination[1] = (byte)((value >> 14) & 0x7F);
        destination[2] = (byte)((value >> 7) & 0x7F);
        destination[3] = (byte)(value & 0x7F);
    }

    private static void WriteBigEndian(int value, Span<byte> destination)
    {
        destination[0] = (byte)((value >> 24) & 0xFF);
        destination[1] = (byte)((value >> 16) & 0xFF);
        destination[2] = (byte)((value >> 8) & 0xFF);
        destination[3] = (byte)(value & 0xFF);
    }

    private static Task RunStaAsync(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
