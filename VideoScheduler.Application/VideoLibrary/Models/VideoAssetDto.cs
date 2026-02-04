using System;

namespace VideoScheduler.Application.VideoLibrary.Models;

public sealed record VideoAssetDto(
    string FullPath,
    string FileName,
    long SizeInBytes,
    DateTimeOffset LastWriteTimeUtc
);
