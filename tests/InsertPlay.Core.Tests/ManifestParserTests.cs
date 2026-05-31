using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsertPlay.Core.Tests;

public class ManifestParserTests
{
    private readonly ManifestParser _parser = new(NullLogger<ManifestParser>.Instance);

    [Fact]
    public async Task TryParseAsync_ReturnsNull_WhenNoManifestFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var result = await _parser.TryParseAsync(tempDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryParseAsync_ReturnsNull_WhenJsonIsInvalid()
    {
        var tempDir = CreateTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "insertplay.json"), "{ not valid json }");
            var result = await _parser.TryParseAsync(tempDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryParseAsync_ReturnsNull_WhenSchemaVersionIsUnsupported()
    {
        var tempDir = CreateTempDir();
        try
        {
            var json = """
                {
                  "schemaVersion": "99.0",
                  "id": "550e8400-e29b-41d4-a716-446655440000",
                  "title": "Test Game",
                  "executable": "game.exe"
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "insertplay.json"), json);
            var result = await _parser.TryParseAsync(tempDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryParseAsync_ReturnsManifest_WhenValidAndExecutableExists()
    {
        var tempDir = CreateTempDir();
        try
        {
            // Create a dummy executable so the validator finds it
            var binDir = Path.Combine(tempDir, "bin");
            Directory.CreateDirectory(binDir);
            await File.WriteAllTextAsync(Path.Combine(binDir, "game.exe"), "dummy");

            var json = """
                {
                  "schemaVersion": "1.0",
                  "id": "550e8400-e29b-41d4-a716-446655440000",
                  "title": "Test Game",
                  "executable": "bin/game.exe",
                  "stopCombination": ["Back", "Start"]
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "insertplay.json"), json);

            var result = await _parser.TryParseAsync(tempDir);

            Assert.NotNull(result);
            Assert.Equal("1.0", result.SchemaVersion);
            Assert.Equal("Test Game", result.Title);
            Assert.Equal("bin/game.exe", result.Executable);
            Assert.Equal(["Back", "Start"], result.StopCombination);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryParseAsync_ReturnsNull_WhenExecutableMissing()
    {
        var tempDir = CreateTempDir();
        try
        {
            var json = """
                {
                  "schemaVersion": "1.0",
                  "id": "550e8400-e29b-41d4-a716-446655440000",
                  "title": "Test Game",
                  "executable": "nonexistent/game.exe"
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "insertplay.json"), json);
            var result = await _parser.TryParseAsync(tempDir);
            Assert.Null(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
