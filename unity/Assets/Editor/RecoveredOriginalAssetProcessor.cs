using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RecoveredOriginalAssetProcessor
{
    private const int ExpectedAnimalComponents = 10;
    private const int MinimumAnimalPixels = 2500;
    private const int Padding = 6;

    private sealed class ComponentBounds
    {
        public int MinX = int.MaxValue;
        public int MinY = int.MaxValue;
        public int MaxX = int.MinValue;
        public int MaxY = int.MinValue;
        public int PixelCount;

        public int Width { get { return MaxX - MinX + 1; } }
        public int Height { get { return MaxY - MinY + 1; } }
        public float CenterX { get { return (MinX + MaxX) * 0.5f; } }
        public float CenterY { get { return (MinY + MaxY) * 0.5f; } }
    }

    public static void ProcessAnimals(string[] sourcePaths, string[] runPaths,
        string[] idlePaths)
    {
        if (sourcePaths == null || runPaths == null || idlePaths == null ||
            sourcePaths.Length != AnimalCatalog.Count ||
            runPaths.Length != AnimalCatalog.Count ||
            idlePaths.Length != AnimalCatalog.Count)
        {
            throw new ArgumentException("Recovered animal path arrays must contain six entries.");
        }

        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            ProcessAnimal(sourcePaths[index], runPaths[index], idlePaths[index]);
        }
    }

    public static void PrepareTransparentCopy(string sourcePath, string outputPath)
    {
        ConfigureImporter(sourcePath, true, false);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Recovered artwork not found: " + sourcePath);
        }

        Color32[] pixels = source.GetPixels32();
        RemoveConnectedBlackBackground(pixels, source.width, source.height);
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
        ConfigureImporter(outputPath, false, true);
    }

    public static void PrepareAllBlackTransparentCopy(string sourcePath, string outputPath)
    {
        ConfigureImporter(sourcePath, true, false);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Recovered artwork not found: " + sourcePath);
        }

        Color32[] pixels = source.GetPixels32();
        for (int index = 0; index < pixels.Length; index++)
        {
            if (!IsBlackBackground(pixels[index])) continue;
            Color32 color = pixels[index];
            color.a = 0;
            pixels[index] = color;
        }

        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
        ConfigureImporter(outputPath, false, true);
    }

    public static void ProcessObstaclePoses(string sourcePath, string outputPath)
    {
        ConfigureImporter(sourcePath, true, false);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        Color32[] pixels = source.GetPixels32();
        RemoveConnectedBlackBackground(pixels, source.width, source.height);
        RemoveEnclosedBlackRegions(pixels, source.width, source.height);
        // Ground shadows are not part of an airborne pose or a ball-riding pose.
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 c = pixels[i];
            if (Math.Abs(c.r - 116) <= 1 && Math.Abs(c.g - 110) <= 1 && Math.Abs(c.b - 96) <= 1)
            {
                c.a = 0;
                pixels[i] = c;
            }
        }
        var frames = new List<ComponentBounds>();
        var anchors = new List<int>();
        int cellWidth = 1, cellHeight = 1;
        for (int number = 5; number <= 10; number++)
        {
            int column = (number - 1) / 5;
            int row = (number - 1) % 5;
            int left = column * source.width / 2;
            int right = (column + 1) * source.width / 2;
            int bottom = source.height - (row + 1) * source.height / 5;
            int top = source.height - row * source.height / 5;
            var bounds = new ComponentBounds();
            for (int y = bottom; y < top; y++)
                for (int x = left; x < right; x++)
                    if (pixels[y * source.width + x].a != 0)
                    {
                        bounds.MinX = Math.Min(bounds.MinX, x);
                        bounds.MaxX = Math.Max(bounds.MaxX, x);
                        bounds.MinY = Math.Min(bounds.MinY, y);
                        bounds.MaxY = Math.Max(bounds.MaxY, y);
                        bounds.PixelCount++;
                    }
            if (bounds.PixelCount == 0) throw new InvalidOperationException("Missing action pose " + number);
            int anchor = (bounds.MinX + bounds.MaxX) / 2;
            if (number >= 9)
            {
                int sum = 0, count = 0;
                for (int y = bounds.MinY; y <= Math.Min(bounds.MinY + 3, bounds.MaxY); y++)
                    for (int x = bounds.MinX; x <= bounds.MaxX; x++)
                        if (pixels[y * source.width + x].a != 0) { sum += x; count++; }
                if (count > 0) anchor = Mathf.RoundToInt(sum / (float)count);
            }
            frames.Add(bounds);
            anchors.Add(anchor);
            cellWidth = Math.Max(cellWidth, 2 * Math.Max(anchor - bounds.MinX, bounds.MaxX - anchor) + Padding * 2 + 2);
            cellHeight = Math.Max(cellHeight, bounds.Height + Padding * 2);
        }
        var output = new Texture2D(cellWidth * 2, cellHeight * 3, TextureFormat.RGBA32, false);
        var outputPixels = new Color32[output.width * output.height];
        for (int i = 0; i < frames.Count; i++)
        {
            ComponentBounds bounds = frames[i];
            int x = (i % 2) * cellWidth + cellWidth / 2 - (anchors[i] - bounds.MinX);
            int y = (2 - i / 2) * cellHeight + Padding;
            CopyBounds(pixels, source.width, bounds, outputPixels, output.width, output.height, x, y);
        }
        output.SetPixels32(outputPixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
        ConfigureImporter(outputPath, false, true);
    }

    public static void PrepareFinishRankCopy(string sourcePath, string outputPath)
    {
        ConfigureImporter(sourcePath, true, false);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Recovered finish ranks not found: " + sourcePath);
        }

        Color32[] pixels = source.GetPixels32();
        RemoveConnectedBlackBackground(pixels, source.width, source.height);
        ClearDarkRegionFromSeed(pixels, source.width, source.height, 75, 18);
        ClearDarkRegionFromSeed(pixels, source.width, source.height, 75, 89);
        ClearSixInnerArcArtifact(pixels, source.width, source.height);
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        output.SetPixels32(pixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
        ConfigureImporter(outputPath, false, true);
    }

    private static void ClearDarkRegionFromSeed(
        Color32[] pixels, int width, int height, int seedX, int seedY)
    {
        if (seedX < 0 || seedX >= width || seedY < 0 || seedY >= height)
        {
            return;
        }

        int seed = seedY * width + seedX;
        if (!IsDarkRankHole(pixels[seed])) return;

        var queue = new Queue<int>();
        var visited = new bool[pixels.Length];
        var region = new List<int>();
        visited[seed] = true;
        queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            region.Add(index);
            VisitDarkRankHole(index - 1, x > 0, pixels, visited, queue);
            VisitDarkRankHole(index + 1, x + 1 < width, pixels, visited, queue);
            VisitDarkRankHole(index - width, y > 0, pixels, visited, queue);
            VisitDarkRankHole(index + width, y + 1 < height, pixels, visited, queue);
        }

        if (region.Count == 0 || region.Count > 400) return;
        for (int index = 0; index < region.Count; index++)
        {
            Color32 color = pixels[region[index]];
            color.a = 0;
            pixels[region[index]] = color;
        }
    }

    private static void VisitDarkRankHole(
        int index, bool valid, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        if (!valid || visited[index] || !IsDarkRankHole(pixels[index])) return;
        visited[index] = true;
        queue.Enqueue(index);
    }

    private static bool IsDarkRankHole(Color32 color)
    {
        return color.a > 0 && color.r <= 80 && color.g <= 60 && color.b <= 30;
    }

    private static void ClearSixInnerArcArtifact(Color32[] pixels, int width, int height)
    {
        // This arc is connected to the outer outline in the recovered source, so
        // flood fill cannot remove it without damaging the outside of the six.
        const int minX = 60;
        const int maxX = 68;
        const int minY = 14;
        const int maxY = 28;
        for (int y = minY; y <= maxY && y < height; y++)
        {
            for (int x = minX; x <= maxX && x < width; x++)
            {
                int index = y * width + x;
                Color32 color = pixels[index];
                // Only remove the genuinely black capture background. The old
                // broad threshold also erased the brown antialiased outline
                // that joins the curl to the body of the green six.
                if (color.a == 0 || color.r > 24 || color.g > 24 || color.b > 20)
                {
                    continue;
                }

                color.a = 0;
                pixels[index] = color;
            }
        }
    }

    private static void ProcessAnimal(string sourcePath, string runPath, string idlePath)
    {
        ConfigureImporter(sourcePath, true, false);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Recovered animal sheet not found: " + sourcePath);
        }

        Color32[] pixels = source.GetPixels32();
        RemoveConnectedBlackBackground(pixels, source.width, source.height);
        RemoveEnclosedBlackRegions(pixels, source.width, source.height);
        List<ComponentBounds> components = FindComponents(pixels, source.width, source.height);
        if (components.Count != ExpectedAnimalComponents)
        {
            throw new InvalidOperationException(sourcePath + " must contain 10 complete animal poses, found " +
                                                components.Count + ".");
        }

        components.Sort(delegate(ComponentBounds left, ComponentBounds right)
        {
            return right.CenterY.CompareTo(left.CenterY);
        });

        var rows = new List<List<ComponentBounds>>(5);
        for (int row = 0; row < 5; row++)
        {
            var pair = new List<ComponentBounds>
            {
                components[row * 2],
                components[row * 2 + 1]
            };
            pair.Sort(delegate(ComponentBounds left, ComponentBounds right)
            {
                return left.CenterX.CompareTo(right.CenterX);
            });
            rows.Add(pair);
        }

        var runFrames = new List<ComponentBounds>(4);
        for (int row = 1; row < 5; row++)
        {
            runFrames.Add(rows[row][0]);
        }

        WriteStrip(pixels, source.width, source.height, runFrames, runPath);
        WriteStrip(pixels, source.width, source.height,
            new List<ComponentBounds> { rows[0][0] }, idlePath);
        ConfigureImporter(runPath, false, true);
        ConfigureImporter(idlePath, false, true);
    }

    private static void RemoveConnectedBlackBackground(Color32[] pixels, int width, int height)
    {
        bool[] background = new bool[pixels.Length];
        var queue = new Queue<int>(width * 2 + height * 2);
        for (int x = 0; x < width; x++)
        {
            QueueBackgroundPixel(x, pixels, background, queue);
            QueueBackgroundPixel((height - 1) * width + x, pixels, background, queue);
        }
        for (int y = 0; y < height; y++)
        {
            QueueBackgroundPixel(y * width, pixels, background, queue);
            QueueBackgroundPixel(y * width + width - 1, pixels, background, queue);
        }

        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            if (x > 0) QueueBackgroundPixel(index - 1, pixels, background, queue);
            if (x + 1 < width) QueueBackgroundPixel(index + 1, pixels, background, queue);
            if (y > 0) QueueBackgroundPixel(index - width, pixels, background, queue);
            if (y + 1 < height) QueueBackgroundPixel(index + width, pixels, background, queue);
        }

        for (int index = 0; index < pixels.Length; index++)
        {
            if (!background[index]) continue;
            Color32 color = pixels[index];
            color.a = 0;
            pixels[index] = color;
        }
    }

    private static void QueueBackgroundPixel(int index, Color32[] pixels,
        bool[] background, Queue<int> queue)
    {
        if (index < 0 || index >= pixels.Length || background[index] ||
            !IsBlackBackground(pixels[index]))
        {
            return;
        }

        background[index] = true;
        queue.Enqueue(index);
    }

    private static bool IsBlackBackground(Color32 color)
    {
        return color.r <= 8 && color.g <= 8 && color.b <= 8;
    }

    public static bool ShouldRemoveEnclosedBlackRegion(int pixelCount)
    {
        return pixelCount >= 20;
    }

    private static void RemoveEnclosedBlackRegions(Color32[] pixels, int width, int height)
    {
        bool[] visited = new bool[pixels.Length];
        var queue = new Queue<int>();
        var region = new List<int>();
        for (int start = 0; start < pixels.Length; start++)
        {
            if (visited[start] || pixels[start].a == 0 || !IsBlackBackground(pixels[start]))
            {
                continue;
            }

            bool touchesEdge = false;
            visited[start] = true;
            queue.Enqueue(start);
            region.Clear();
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                region.Add(index);
                touchesEdge |= x == 0 || y == 0 || x == width - 1 || y == height - 1;
                VisitBlack(index - 1, x > 0, pixels, visited, queue);
                VisitBlack(index + 1, x + 1 < width, pixels, visited, queue);
                VisitBlack(index - width, y > 0, pixels, visited, queue);
                VisitBlack(index + width, y + 1 < height, pixels, visited, queue);
            }

            if (touchesEdge || !ShouldRemoveEnclosedBlackRegion(region.Count)) continue;
            for (int index = 0; index < region.Count; index++)
            {
                Color32 color = pixels[region[index]];
                color.a = 0;
                pixels[region[index]] = color;
            }
        }
    }

    private static void VisitBlack(int index, bool valid, Color32[] pixels,
        bool[] visited, Queue<int> queue)
    {
        if (!valid || visited[index] || pixels[index].a == 0 ||
            !IsBlackBackground(pixels[index]))
        {
            return;
        }
        visited[index] = true;
        queue.Enqueue(index);
    }

    private static List<ComponentBounds> FindComponents(Color32[] pixels, int width, int height)
    {
        var components = new List<ComponentBounds>();
        bool[] visited = new bool[pixels.Length];
        var queue = new Queue<int>();
        for (int start = 0; start < pixels.Length; start++)
        {
            if (visited[start] || pixels[start].a == 0) continue;
            var bounds = new ComponentBounds();
            visited[start] = true;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                bounds.MinX = Math.Min(bounds.MinX, x);
                bounds.MaxX = Math.Max(bounds.MaxX, x);
                bounds.MinY = Math.Min(bounds.MinY, y);
                bounds.MaxY = Math.Max(bounds.MaxY, y);
                bounds.PixelCount++;
                Visit(index - 1, x > 0, pixels, visited, queue);
                Visit(index + 1, x + 1 < width, pixels, visited, queue);
                Visit(index - width, y > 0, pixels, visited, queue);
                Visit(index + width, y + 1 < height, pixels, visited, queue);
            }
            if (bounds.PixelCount >= MinimumAnimalPixels) components.Add(bounds);
        }
        return components;
    }

    private static void Visit(int index, bool valid, Color32[] pixels,
        bool[] visited, Queue<int> queue)
    {
        if (!valid || visited[index] || pixels[index].a == 0) return;
        visited[index] = true;
        queue.Enqueue(index);
    }

    private static void WriteStrip(Color32[] source, int sourceWidth, int sourceHeight,
        List<ComponentBounds> frames, string outputPath)
    {
        int cellWidth = 1;
        int cellHeight = 1;
        for (int index = 0; index < frames.Count; index++)
        {
            cellWidth = Math.Max(cellWidth, frames[index].Width + Padding * 2);
            cellHeight = Math.Max(cellHeight, frames[index].Height + Padding * 2);
        }

        int outputWidth = cellWidth * frames.Count;
        var output = new Texture2D(outputWidth, cellHeight, TextureFormat.RGBA32, false);
        var outputPixels = new Color32[outputWidth * cellHeight];
        for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
        {
            ComponentBounds frame = frames[frameIndex];
            int offsetX = frameIndex * cellWidth + (cellWidth - frame.Width) / 2;
            int offsetY = Padding;
            CopyBounds(source, sourceWidth, frame, outputPixels, outputWidth, cellHeight,
                offsetX, offsetY);
        }

        output.SetPixels32(outputPixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static void CopyBounds(Color32[] source, int sourceWidth,
        ComponentBounds bounds, Color32[] output, int outputWidth, int outputHeight,
        int offsetX, int offsetY)
    {
        for (int y = bounds.MinY; y <= bounds.MaxY; y++)
        {
            for (int x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                int destinationX = offsetX + x - bounds.MinX;
                int destinationY = offsetY + y - bounds.MinY;
                if (destinationX < 0 || destinationX >= outputWidth ||
                    destinationY < 0 || destinationY >= outputHeight) continue;
                output[destinationY * outputWidth + destinationX] =
                    source[y * sourceWidth + x];
            }
        }
    }

    private static void WritePng(Texture2D texture, string assetPath)
    {
        string absolutePath = Path.GetFullPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static void ConfigureImporter(string assetPath, bool readable, bool transparent)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Texture importer not found: " + assetPath);
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = transparent;
        importer.isReadable = readable;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }
}
