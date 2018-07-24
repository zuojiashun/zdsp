﻿//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class SavWav
{

    const int HEADER_SIZE = 44;

    public static bool Save(string filename, AudioClip clip)
    {
        if (!filename.ToLower().EndsWith(".wav"))
        {
            filename += ".wav";
        }

        var filepath = Path.Combine(Application.dataPath, filename);

        Debug.Log(filepath);

        // Make sure directory exists if user is saving to sub dir.
        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        using (var fileStream = CreateEmpty(filepath))
        {

            ConvertAndWrite(fileStream, clip);

            WriteHeader(fileStream, clip);
        }

        return true; // TODO: return false if there's a failure saving the file
    }

    public static AudioClip TrimSilence(AudioClip clip, float min)
    {
        var samples = new float[clip.samples * clip.channels];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
    }

    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
    {
        return TrimSilence(samples, min, channels, hz, true, false);
    }

    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                Debug.Log("trim begin i = " + i);
                break;              
            }
        }
        Debug.Log("trim begin count = " + i);
        samples.RemoveRange(0, i);

        if (samples.Count == 0)
            return null;

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                Debug.Log("trim end i = " + i);
                break;
            }
        }
        Debug.Log("trim end count = " + (samples.Count - i));
        samples.RemoveRange(i, samples.Count - i);

        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }

    static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        byte emptyByte = new byte();

        for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
        {
            fileStream.WriteByte(emptyByte);
        }

        return fileStream;
    }

    static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {

        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

        Byte[] bytesData = new Byte[samples.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        int rescaleFactor = 32767; //to convert float to Int16

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fileStream, AudioClip clip)
    {

        var hz = clip.frequency;
        var channels = clip.channels;
        var samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        Byte[] audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        Byte[] numChannels = BitConverter.GetBytes(channels);
        fileStream.Write(numChannels, 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bps = 16;
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        fileStream.Write(bitsPerSample, 0, 2);

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(datastring, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);

        //		fileStream.Close();
    }

    public static void ConstructClipHeader(Byte[] bytes, int hz, int channels, int samples, UInt16 bps)
    {
        int index = 0;
        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        Array.Copy(riff, 0, bytes, index, 4);
        index += 4;

        Byte[] chunkSize = BitConverter.GetBytes(bytes.Length - 8);
        Array.Copy(chunkSize, 0, bytes, index, 4);
        index += 4;

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        Array.Copy(wave, 0, bytes, index, 4);
        index += 4;

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        Array.Copy(fmt, 0, bytes, index, 4);
        index += 4;

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        Array.Copy(subChunk1, 0, bytes, index, 4);
        index += 4;

        Byte[] audioFormat = BitConverter.GetBytes(1);
        Array.Copy(audioFormat, 0, bytes, index, 2);
        index += 2;

        Byte[] numChannels = BitConverter.GetBytes(channels);
        Array.Copy(numChannels, 0, bytes, index, 2);
        index += 2;

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        Array.Copy(sampleRate, 0, bytes, index, 4);
        index += 4;

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * bps/8); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
        Array.Copy(byteRate, 0, bytes, index, 4);
        index += 4;

        UInt16 blockAlign = (ushort)(channels * bps / 8);
        Array.Copy(BitConverter.GetBytes(blockAlign), 0, bytes, index, 2);
        index += 2;
       
        Byte[] bitsPerSample = BitConverter.GetBytes(bps);
        Array.Copy(bitsPerSample, 0, bytes, index, 2);
        index += 2;

        Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
        Array.Copy(datastring, 0, bytes, index, 4);
        index += 4;

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * bps/8);
        Array.Copy(subChunk2, 0, bytes, index, 4);
        index += 4;
    }

    public static float rescaleFactor = 32767; //to convert float to Int16
    public static byte[] ToByteArray(float[] floatArray, int hz, int channels, int samples)
    {
        int length = floatArray.Length;
        byte[] byteArray = new byte[length * 2 + 44];
        SavWav.ConstructClipHeader(byteArray, hz, channels, samples, 16);
        for (int index = 0; index < length; index++)
        {
            byte[] data = BitConverter.GetBytes((short)(floatArray[index] * rescaleFactor));
            Array.Copy(data, 0, byteArray, 44 + index * 2, 2);
        }
        return byteArray;
    }

    public static float[] ToFloatArray(byte[] byteArray)
    {
        int len = byteArray.Length / 2;
        float[] floatArray = new float[len];
        for (int index = 0; index < floatArray.Length; index++)
        {
            short data = BitConverter.ToInt16(byteArray, index * 2);
            floatArray[index] = data / rescaleFactor;
        }
        return floatArray;
    }

    public static void TrimSilence(List<float> samples, float minBegin, float minEnd, int threshcount)
    {
        int i;
        int count = 0;
        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > minBegin)
            {               
                count++;
                if (count >= threshcount)
                {
                    //Debug.Log("trim begin i = " + i);
                    break;
                }
            }
        }
        //Debug.Log(string.Format("trim begin count = {0}, threshcount = {1}", i, count));
        samples.RemoveRange(0, i);

        if (samples.Count == 0)
            return;
        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > minEnd)
            {
                //Debug.Log("trim end i = " + i);
                break;
            }
        }
        //Debug.Log(string.Format("trim end count = {0}", samples.Count - i));
        samples.RemoveRange(i, samples.Count - i);
    }
}
