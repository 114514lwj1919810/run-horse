using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AnimalSpriteSheetProcessor
{
    private const int ExpectedComponentCount = 9;
    private const int Padding = 8;
    private const int MinimumComponentPixels = 2500;

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

    public static void ProcessAll(string[] sourcePaths, string[] runPaths,
        string[] idlePaths, string[] portraitPaths)
    {
        if (sourcePaths == null || runPaths == null || idlePaths == null || portraitPaths == null ||
            sourcePaths.Length != AnimalCatalog.Count || runPaths.Length != AnimalCatalog.Count ||
            idlePaths.Length != AnimalCatalog.Count || portraitPaths.Length != AnimalCatalog.Count)
        {
            throw new ArgumentException("Animal sprite path arrays must contain exactly six entries.");
        }

        for (int index = 0; index < AnimalCatalog.Count; index++)
        {
            ProcessOne(sourcePaths[index], runPaths[index], idlePaths[index], portraitPaths[index]);
        }
    }

    private static void ProcessOne(string sourcePath, string runPath, string idlePath, string portraitPath)
    {
        ConfigureImporter(sourcePath, true);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        if (source == null)
        {
            throw new InvalidOperationException("Animal source image not found: " + sourcePath);
        }

        int width = source.width;
        int height = source.height;
        Color32[] pixels = source.GetPixels32();
        RemoveConnectedWhiteBackground(pixels, width, height);

        List<ComponentBounds> components = FindComponents(pixels, width, height);
        if (components.Count != ExpectedComponentCount)
        {
            throw new InvalidOperationException(sourcePath + " must contain 9 isolated assets, found " +
                                                components.Count + ".");
        }

        components.Sort(delegate(ComponentBounds left, ComponentBounds right)
        {
            return right.CenterY.CompareTo(left.CenterY);
        });

        var top = components.GetRange(0, 3);
        var bottom = components.GetRange(3, 6);
        top.Sort(delegate(ComponentBounds left, ComponentBounds right)
        {
            return left.CenterX.CompareTo(right.CenterX);
        });
        bottom.Sort(delegate(ComponentBounds left, ComponentBounds right)
        {
            return left.CenterX.CompareTo(right.CenterX);
        });

        WriteStrip(pixels, width, height, bottom, runPath);
        WriteStrip(pixels, width, height, top.GetRange(1, 2), idlePath);
        WritePortrait(pixels, width, height, top[0], portraitPath);

        ConfigureImporter(runPath, false);
        ConfigureImporter(idlePath, false);
        ConfigureImporter(portraitPath, false);
    }

    private static void RemoveConnectedWhiteBackground(Color32[] pixels, int width, int height)
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
            if (background[index])
            {
                Color32 color = pixels[index];
                color.a = 0;
                pixels[index] = color;
            }
        }
    }

    private static void QueueBackgroundPixel(int index, Color32[] pixels, bool[] background, Queue<int> queue)
    {
        if (index < 0 || index >= pixels.Length || background[index] || !IsBackground(pixels[index]))
        {
            return;
        }

        background[index] = true;
        queue.Enqueue(index);
    }

    private static bool IsBackground(Color32 color)
    {
        int minimum = Math.Min(color.r, Math.Min(color.g, color.b));
        int maximum = Math.Max(color.r, Math.Max(color.g, color.b));
        return minimum >= 214 && maximum - minimum <= 32;
    }

    private static List<ComponentBounds> FindComponents(Color32[] pixels, int width, int height)
    {
        var components = new List<ComponentBounds>();
        bool[] visited = new bool[pixels.Length];
        var queue = new Queue<int>();

        for (int start = 0; start < pixels.Length; start++)
        {
            if (visited[start] || pixels[start].a == 0)
            {
                continue;
            }

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

                VisitForeground(index - 1, x > 0, pixels, visited, queue);
                VisitForeground(index + 1, x + 1 < width, pixels, visited, queue);
                VisitForeground(index - width, y > 0, pixels, visited, queue);
                VisitForeground(index + width, y + 1 < height, pixels, visited, queue);
            }

            if (bounds.PixelCount >= MinimumComponentPixels)
            {
                components.Add(bounds);
            }
        }

        return components;
    }

    private static void VisitForeground(int index, bool valid, Color32[] pixels, bool[] visited, Queue<int> queue)
    {
        if (!valid || visited[index] || pixels[index].a == 0)
        {
            return;
        }

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
            CopyBounds(source, sourceWidth, sourceHeight, frame, outputPixels, outputWidth, cellHeight,
                offsetX, offsetY);
        }

        output.SetPixels32(outputPixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static void WritePortrait(Color32[] source, int sourceWidth, int sourceHeight,
        ComponentBounds portrait, string outputPath)
    {
        int size = Math.Max(portrait.Width, portrait.Height) + Padding * 2;
        var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var outputPixels = new Color32[size * size];
        int offsetX = (size - portrait.Width) / 2;
        int offsetY = (size - portrait.Height) / 2;
        CopyBounds(source, sourceWidth, sourceHeight, portrait, outputPixels, size, size, offsetX, offsetY);
        output.SetPixels32(outputPixels);
        output.Apply(false, false);
        WritePng(output, outputPath);
        UnityEngine.Object.DestroyImmediate(output);
    }

    private static void CopyBounds(Color32[] source, int sourceWidth, int sourceHeight,
        ComponentBounds bounds, Color32[] output, int outputWidth, int outputHeight,
        int offsetX, int offsetY)
    {
        for (int y = bounds.MinY; y <= bounds.MaxY; y++)
        {
            for (int x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                int destinationX = offsetX + x - bounds.MinX;
                int destinationY = offsetY + y - bounds.MinY;
                if (destinationX >= 0 && destinationX < outputWidth &&
                    destinationY >= 0 && destinationY < outputHeight)
                {
                    output[destinationY * outputWidth + destinationX] = source[y * sourceWidth + x];
                }
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

    private static void ConfigureImporter(string assetPath, bool readable)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Texture importer not found: " + assetPath);
        }

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
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
