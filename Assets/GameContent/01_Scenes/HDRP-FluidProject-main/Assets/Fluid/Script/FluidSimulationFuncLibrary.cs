using System;
using System.Collections.Generic;
using UnityEngine;

public static class FluidSimulationFuncLibrary
{
    public static int size;
    public static float diff;
    public static float dt;
    public static float disturbance;

    public static float[] GetGrayscale(
        Texture2D texture,
        int targetSize,
        string channel,
        float correction,
        float multiplier)
    {
        return SampleTexture(
            texture,
            targetSize,
            color => ReadChannel(color, channel) - correction,
            multiplier);
    }

    public static float[] GetGrayscale(
        Texture2D texture,
        int targetSize,
        string channel,
        string negativeChannel,
        float multiplier)
    {
        return SampleTexture(
            texture,
            targetSize,
            color => ReadChannel(color, channel) - ReadChannel(color, negativeChannel),
            multiplier);
    }

    public static void CalculateShader(
        ComputeShader shader,
        string kernelName,
        string[] bufferBindings,
        ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kernel = shader.FindKernel(kernelName);
        foreach (string binding in bufferBindings)
        {
            string[] parts = binding.Split('_');
            shader.SetBuffer(kernel, parts[parts.Length - 1], computeBufferMap[parts[0]]);
        }

        Dispatch(shader, kernel);
    }

    public static ComputeBuffer GetComputeBuffer(float[] values)
    {
        ComputeBuffer buffer = new ComputeBuffer(values.Length, sizeof(float));
        buffer.SetData(values);
        return buffer;
    }

    public static void AddDynamicSource(
        ComputeShader shader,
        ref RenderTexture renderTexture,
        ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kernel = shader.FindKernel("AddDynamicSource");
        shader.SetTexture(kernel, "dynamicSourceTexture", renderTexture);
        shader.SetBuffer(kernel, "SBuffer", computeBufferMap["X0"]);
        Dispatch(shader, kernel);
    }

    public static void CalculateCollision(
        ComputeShader shader,
        ref RenderTexture inputTexture,
        ref RenderTexture outputTexture,
        ref Dictionary<string, ComputeBuffer> computeBufferMap)
    {
        int kernel = shader.FindKernel("Collision");
        shader.SetTexture(kernel, "inputTexture", inputTexture);
        shader.SetTexture(kernel, "outputTexture", outputTexture);
        shader.SetBuffer(kernel, "XBuffer", computeBufferMap["X"]);
        shader.SetBuffer(kernel, "X0Buffer", computeBufferMap["X0"]);
        shader.SetBuffer(kernel, "UBuffer", computeBufferMap["U"]);
        shader.SetBuffer(kernel, "VBuffer", computeBufferMap["V"]);
        shader.SetBuffer(kernel, "CBuffer", computeBufferMap["C"]);
        Dispatch(shader, kernel);
    }

    public static void CalculateBrush(
        ComputeShader shader,
        ref RenderTexture inputTexture,
        Vector2Int position,
        int brushSize)
    {
        int kernel = shader.FindKernel("BrushRT");
        shader.SetInt("brushPosX", position.x);
        shader.SetInt("brushPosY", position.y);
        shader.SetInt("brushSize", brushSize);
        shader.SetTexture(kernel, "inputTexture", inputTexture);
        Dispatch(shader, kernel);
    }

    // Kept for compatibility with the original HDRP component.
    public static void CalculateBrush(
        ComputeShader shader,
        Vector2 position,
        int brushSize)
    {
        shader.SetInt("brushPosX", Mathf.RoundToInt(position.x));
        shader.SetInt("brushPosY", Mathf.RoundToInt(position.y));
        shader.SetInt("brushSize", brushSize);
    }

    private static float[] SampleTexture(
        Texture2D texture,
        int targetSize,
        Func<Color32, float> sample,
        float multiplier)
    {
        if (targetSize <= 0)
        {
            return Array.Empty<float>();
        }

        float[] result = new float[targetSize * targetSize];
        if (texture == null || texture.width != texture.height || texture.width < targetSize)
        {
            return result;
        }

        Color32[] pixels = texture.GetPixels32();
        int blockSize = texture.width / targetSize;
        float normalization = multiplier / (blockSize * blockSize);

        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                float value = 0f;
                int sourceX = x * blockSize;
                int sourceY = y * blockSize;

                for (int blockY = 0; blockY < blockSize; blockY++)
                {
                    int row = (sourceY + blockY) * texture.width;
                    for (int blockX = 0; blockX < blockSize; blockX++)
                    {
                        value += sample(pixels[row + sourceX + blockX]);
                    }
                }

                result[x + y * targetSize] = value * normalization;
            }
        }

        return result;
    }

    private static float ReadChannel(Color32 color, string channel)
    {
        const float ByteToFloat = 1f / 255f;
        switch (channel)
        {
            case "R":
                return color.r * ByteToFloat;
            case "G":
                return color.g * ByteToFloat;
            case "B":
                return color.b * ByteToFloat;
            case "A":
                return color.a * ByteToFloat;
            case "RGB":
                return (color.r + color.g + color.b) * (ByteToFloat / 3f);
            default:
                return 0f;
        }
    }

    private static void Dispatch(ComputeShader shader, int kernel)
    {
        shader.GetKernelThreadGroupSizes(kernel, out uint threadX, out uint threadY, out _);
        int groupsX = Mathf.CeilToInt(size / (float)Mathf.Max(1, (int)threadX));
        int groupsY = Mathf.CeilToInt(size / (float)Mathf.Max(1, (int)threadY));
        shader.Dispatch(kernel, groupsX, groupsY, 1);
    }
}
