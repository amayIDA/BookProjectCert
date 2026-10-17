using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Minimal QR Code encoder (byte mode, error correction level M, versions 1 to 40).
/// Returns a square module matrix where true is a dark module.
/// </summary>
public static class QRCodeEncoder
{
    // For each version: groups of (blockCount, totalCodewordsPerBlock, dataCodewordsPerBlock). Level M.
    static readonly int[][] Blocks =
    {
        new[] { 1,26,16 },
        new[] { 1,44,28 },
        new[] { 1,70,44 },
        new[] { 2,50,32 },
        new[] { 2,67,43 },
        new[] { 4,43,27 },
        new[] { 4,49,31 },
        new[] { 2,60,38, 2,61,39 },
        new[] { 3,58,36, 2,59,37 },
        new[] { 4,69,43, 1,70,44 },
        new[] { 1,80,50, 4,81,51 },
        new[] { 6,58,36, 2,59,37 },
        new[] { 8,59,37, 1,60,38 },
        new[] { 4,64,40, 5,65,41 },
        new[] { 5,65,41, 5,66,42 },
        new[] { 7,73,45, 3,74,46 },
        new[] { 10,74,46, 1,75,47 },
        new[] { 9,69,43, 4,70,44 },
        new[] { 3,70,44, 11,71,45 },
        new[] { 3,67,41, 13,68,42 },
        new[] { 17,68,42 },
        new[] { 17,74,46 },
        new[] { 4,75,47, 14,76,48 },
        new[] { 6,73,45, 14,74,46 },
        new[] { 8,75,47, 13,76,48 },
        new[] { 19,74,46, 4,75,47 },
        new[] { 22,73,45, 3,74,46 },
        new[] { 3,73,45, 23,74,46 },
        new[] { 21,73,45, 7,74,46 },
        new[] { 19,75,47, 10,76,48 },
        new[] { 2,74,46, 29,75,47 },
        new[] { 10,74,46, 23,75,47 },
        new[] { 14,74,46, 21,75,47 },
        new[] { 14,74,46, 23,75,47 },
        new[] { 12,75,47, 26,76,48 },
        new[] { 6,75,47, 34,76,48 },
        new[] { 29,74,46, 14,75,47 },
        new[] { 13,74,46, 32,75,47 },
        new[] { 40,75,47, 7,76,48 },
        new[] { 18,75,47, 31,76,48 }
    };

    static readonly int[][] Align =
    {
        new int[0],
        new[] { 6, 18 },
        new[] { 6, 22 },
        new[] { 6, 26 },
        new[] { 6, 30 },
        new[] { 6, 34 },
        new[] { 6, 22, 38 },
        new[] { 6, 24, 42 },
        new[] { 6, 26, 46 },
        new[] { 6, 28, 50 },
        new[] { 6, 30, 54 },
        new[] { 6, 32, 58 },
        new[] { 6, 34, 62 },
        new[] { 6, 26, 46, 66 },
        new[] { 6, 26, 48, 70 },
        new[] { 6, 26, 50, 74 },
        new[] { 6, 30, 54, 78 },
        new[] { 6, 30, 56, 82 },
        new[] { 6, 30, 58, 86 },
        new[] { 6, 34, 62, 90 },
        new[] { 6, 28, 50, 72, 94 },
        new[] { 6, 26, 50, 74, 98 },
        new[] { 6, 30, 54, 78, 102 },
        new[] { 6, 28, 54, 80, 106 },
        new[] { 6, 32, 58, 84, 110 },
        new[] { 6, 30, 58, 86, 114 },
        new[] { 6, 34, 62, 90, 118 },
        new[] { 6, 26, 50, 74, 98, 122 },
        new[] { 6, 30, 54, 78, 102, 126 },
        new[] { 6, 26, 52, 78, 104, 130 },
        new[] { 6, 30, 56, 82, 108, 134 },
        new[] { 6, 34, 60, 86, 112, 138 },
        new[] { 6, 30, 58, 86, 114, 142 },
        new[] { 6, 34, 62, 90, 118, 146 },
        new[] { 6, 30, 54, 78, 102, 126, 150 },
        new[] { 6, 24, 50, 76, 102, 128, 154 },
        new[] { 6, 28, 54, 80, 106, 132, 158 },
        new[] { 6, 32, 58, 84, 110, 136, 162 },
        new[] { 6, 26, 54, 82, 110, 138, 166 },
        new[] { 6, 30, 58, 86, 114, 142, 170 }
    };

    static readonly int[] VersionInfo = { 31892, 34236, 39577, 42195, 48118, 51042, 55367, 58893, 63784, 68472, 70749, 76311, 79154, 84390, 87683, 92361, 96236, 102084, 102881, 110507, 110734, 117786, 119615, 126325, 127568, 133589, 136944, 141498, 145311, 150283, 152622, 158308, 161089, 167017 };   // versions 7 to 40
    static readonly int[] FormatInfoM = { 21522, 20773, 24188, 23371, 17913, 16590, 20375, 19104 };   // mask 0 to 7, level M

    static readonly byte[] ExpTable = new byte[512];
    static readonly byte[] LogTable = new byte[256];

    static QRCodeEncoder()
    {
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            ExpTable[i] = (byte)x;
            LogTable[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
                x ^= 0x11D;
        }

        for (int i = 255; i < 512; i++)
            ExpTable[i] = ExpTable[i - 255];
    }

    public static bool[,] Encode(string text, int forcedMask = -1)
    {
        byte[] data = Encoding.UTF8.GetBytes(text ?? string.Empty);
        int version = PickVersion(data.Length);
        List<byte> codewords = BuildDataCodewords(data, version);
        byte[] finalBytes = AddErrorCorrection(codewords, version);

        int size = 17 + version * 4;
        bool[,] modules = new bool[size, size];
        bool[,] reserved = new bool[size, size];
        DrawFunctionPatterns(modules, reserved, version);
        PlaceData(modules, reserved, finalBytes);

        int bestScore = int.MaxValue;
        bool[,] best = null;
        for (int mask = 0; mask < 8; mask++)
        {
            if (forcedMask >= 0 && mask != forcedMask)
                continue;

            bool[,] candidate = (bool[,])modules.Clone();
            ApplyMask(candidate, reserved, mask);
            DrawFormat(candidate, mask);
            int score = Penalty(candidate);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best ?? modules;
    }

    static int PickVersion(int byteCount)
    {
        for (int version = 1; version <= 40; version++)
        {
            int dataBytes = DataCodewords(version);
            int lengthBits = version <= 9 ? 8 : 16;
            int bits = 4 + lengthBits + byteCount * 8;
            if ((bits + 7) / 8 <= dataBytes)
                return version;
        }

        throw new ArgumentException("Text is too long for a QR code.");
    }

    static int DataCodewords(int version)
    {
        int[] groups = Blocks[version - 1];
        int total = 0;
        for (int i = 0; i < groups.Length; i += 3)
            total += groups[i] * groups[i + 2];
        return total;
    }

    static List<byte> BuildDataCodewords(byte[] data, int version)
    {
        int capacityBits = DataCodewords(version) * 8;
        List<bool> bits = new List<bool>();
        AppendBits(bits, 0x4, 4);
        AppendBits(bits, data.Length, version <= 9 ? 8 : 16);
        for (int i = 0; i < data.Length; i++)
            AppendBits(bits, data[i], 8);

        int terminator = Math.Min(4, capacityBits - bits.Count);
        AppendBits(bits, 0, terminator);
        while (bits.Count % 8 != 0)
            bits.Add(false);

        List<byte> result = new List<byte>();
        for (int i = 0; i < bits.Count; i += 8)
        {
            int value = 0;
            for (int b = 0; b < 8; b++)
                value = (value << 1) | (bits[i + b] ? 1 : 0);
            result.Add((byte)value);
        }

        int pad = 0;
        while (result.Count < capacityBits / 8)
        {
            result.Add((byte)(pad % 2 == 0 ? 0xEC : 0x11));
            pad++;
        }

        return result;
    }

    static void AppendBits(List<bool> bits, int value, int length)
    {
        for (int i = length - 1; i >= 0; i--)
            bits.Add(((value >> i) & 1) != 0);
    }

    static byte[] AddErrorCorrection(List<byte> data, int version)
    {
        int[] groups = Blocks[version - 1];
        List<byte[]> dataBlocks = new List<byte[]>();
        List<byte[]> ecBlocks = new List<byte[]>();
        int offset = 0;
        for (int g = 0; g < groups.Length; g += 3)
        {
            int count = groups[g];
            int total = groups[g + 1];
            int dataLen = groups[g + 2];
            int ecLen = total - dataLen;
            for (int b = 0; b < count; b++)
            {
                byte[] block = new byte[dataLen];
                data.CopyTo(offset, block, 0, dataLen);
                offset += dataLen;
                dataBlocks.Add(block);
                ecBlocks.Add(ReedSolomon(block, ecLen));
            }
        }

        List<byte> result = new List<byte>();
        int maxData = 0;
        for (int i = 0; i < dataBlocks.Count; i++)
            maxData = Math.Max(maxData, dataBlocks[i].Length);
        for (int i = 0; i < maxData; i++)
        {
            for (int b = 0; b < dataBlocks.Count; b++)
            {
                if (i < dataBlocks[b].Length)
                    result.Add(dataBlocks[b][i]);
            }
        }

        int ecCount = ecBlocks[0].Length;
        for (int i = 0; i < ecCount; i++)
        {
            for (int b = 0; b < ecBlocks.Count; b++)
                result.Add(ecBlocks[b][i]);
        }

        return result.ToArray();
    }

    // Generator polynomial coefficients, highest degree first, without the leading 1.
    static byte[] ReedSolomon(byte[] data, int ecLength)
    {
        byte[] generator = new byte[ecLength];
        generator[ecLength - 1] = 1;
        byte root = 1;
        for (int i = 0; i < ecLength; i++)
        {
            for (int j = 0; j < ecLength; j++)
            {
                generator[j] = Multiply(generator[j], root);
                if (j + 1 < ecLength)
                    generator[j] ^= generator[j + 1];
            }

            root = Multiply(root, 2);
        }

        byte[] remainder = new byte[ecLength];
        for (int i = 0; i < data.Length; i++)
        {
            byte factor = (byte)(data[i] ^ remainder[0]);
            Array.Copy(remainder, 1, remainder, 0, ecLength - 1);
            remainder[ecLength - 1] = 0;
            for (int j = 0; j < ecLength; j++)
                remainder[j] ^= Multiply(generator[j], factor);
        }

        return remainder;
    }

    static byte Multiply(byte a, byte b)
    {
        if (a == 0 || b == 0)
            return 0;
        return ExpTable[LogTable[a] + LogTable[b]];
    }

    static void DrawFunctionPatterns(bool[,] m, bool[,] r, int version)
    {
        int size = m.GetLength(0);

        for (int i = 0; i < size; i++)
        {
            Set(m, r, 6, i, i % 2 == 0);
            Set(m, r, i, 6, i % 2 == 0);
        }

        DrawFinder(m, r, 3, 3);
        DrawFinder(m, r, size - 4, 3);
        DrawFinder(m, r, 3, size - 4);

        int[] pos = Align[version - 1];
        for (int i = 0; i < pos.Length; i++)
        {
            for (int j = 0; j < pos.Length; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == pos.Length - 1) || (i == pos.Length - 1 && j == 0))
                    continue;
                DrawAlignment(m, r, pos[i], pos[j]);
            }
        }

        for (int i = 0; i < 9; i++)
        {
            r[8, i] = true;
            r[i, 8] = true;
        }

        for (int i = 0; i < 8; i++)
        {
            r[size - 1 - i, 8] = true;
            r[8, size - 1 - i] = true;
        }

        Set(m, r, 8, size - 8, true);

        if (version >= 7)
        {
            int bits = VersionInfo[version - 7];
            for (int i = 0; i < 18; i++)
            {
                bool bit = ((bits >> i) & 1) != 0;
                int a = size - 11 + i % 3;
                int b = i / 3;
                Set(m, r, a, b, bit);
                Set(m, r, b, a, bit);
            }
        }
    }

    static void DrawFinder(bool[,] m, bool[,] r, int cx, int cy)
    {
        int size = m.GetLength(0);
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x < 0 || y < 0 || x >= size || y >= size)
                    continue;
                int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                Set(m, r, x, y, d != 2 && d != 4);
            }
        }
    }

    static void DrawAlignment(bool[,] m, bool[,] r, int cx, int cy)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
                Set(m, r, cx + dx, cy + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
        }
    }

    static void Set(bool[,] m, bool[,] r, int x, int y, bool dark)
    {
        m[x, y] = dark;
        r[x, y] = true;
    }

    static void PlaceData(bool[,] m, bool[,] reserved, byte[] data)
    {
        int size = m.GetLength(0);
        int bitIndex = 0;
        int totalBits = data.Length * 8;
        bool upward = true;
        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
                right = 5;

            for (int step = 0; step < size; step++)
            {
                int y = upward ? size - 1 - step : step;
                for (int c = 0; c < 2; c++)
                {
                    int x = right - c;
                    if (reserved[x, y])
                        continue;

                    bool dark = false;
                    if (bitIndex < totalBits)
                    {
                        dark = ((data[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        bitIndex++;
                    }

                    m[x, y] = dark;
                }
            }

            upward = !upward;
        }
    }

    static bool MaskBit(int mask, int x, int y)
    {
        switch (mask)
        {
            case 0: return (x + y) % 2 == 0;
            case 1: return y % 2 == 0;
            case 2: return x % 3 == 0;
            case 3: return (x + y) % 3 == 0;
            case 4: return (y / 2 + x / 3) % 2 == 0;
            case 5: return (x * y) % 2 + (x * y) % 3 == 0;
            case 6: return ((x * y) % 2 + (x * y) % 3) % 2 == 0;
            default: return ((x + y) % 2 + (x * y) % 3) % 2 == 0;
        }
    }

    static void ApplyMask(bool[,] m, bool[,] reserved, int mask)
    {
        int size = m.GetLength(0);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (!reserved[x, y] && MaskBit(mask, x, y))
                    m[x, y] = !m[x, y];
            }
        }
    }

    static void DrawFormat(bool[,] m, int mask)
    {
        int size = m.GetLength(0);
        int bits = FormatInfoM[mask];

        // bit 0 is the least significant bit of the 15 bit word
        for (int i = 0; i < 15; i++)
        {
            bool bit = ((bits >> i) & 1) != 0;

            int x1;
            int y1;
            if (i < 6) { x1 = 8; y1 = i; }
            else if (i == 6) { x1 = 8; y1 = 7; }
            else if (i == 7) { x1 = 8; y1 = 8; }
            else if (i == 8) { x1 = 7; y1 = 8; }
            else { x1 = 14 - i; y1 = 8; }
            m[x1, y1] = bit;

            int x2;
            int y2;
            if (i < 8) { x2 = size - 1 - i; y2 = 8; }
            else { x2 = 8; y2 = size - 15 + i; }
            m[x2, y2] = bit;
        }

        m[8, size - 8] = true;
    }

    static int Penalty(bool[,] m)
    {
        int size = m.GetLength(0);
        int score = 0;

        for (int pass = 0; pass < 2; pass++)
        {
            for (int a = 0; a < size; a++)
            {
                int run = 1;
                for (int b = 1; b < size; b++)
                {
                    bool prev = pass == 0 ? m[b - 1, a] : m[a, b - 1];
                    bool cur = pass == 0 ? m[b, a] : m[a, b];
                    if (cur == prev)
                    {
                        run++;
                        if (run == 5)
                            score += 3;
                        else if (run > 5)
                            score++;
                    }
                    else
                        run = 1;
                }
            }
        }

        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                bool c = m[x, y];
                if (c == m[x + 1, y] && c == m[x, y + 1] && c == m[x + 1, y + 1])
                    score += 3;
            }
        }

        for (int pass = 0; pass < 2; pass++)
        {
            for (int a = 0; a < size; a++)
            {
                for (int b = 0; b <= size - 11; b++)
                {
                    if (FinderLike(m, pass, a, b, true) || FinderLike(m, pass, a, b, false))
                        score += 40;
                }
            }
        }

        int dark = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (m[x, y])
                    dark++;
            }
        }

        int percent = dark * 100 / (size * size);
        score += Math.Abs(percent - 50) / 5 * 10;
        return score;
    }

    static readonly bool[] PatternA = { true, false, true, true, true, false, true, false, false, false, false };
    static readonly bool[] PatternB = { false, false, false, false, true, false, true, true, true, false, true };

    static bool FinderLike(bool[,] m, int pass, int a, int start, bool first)
    {
        bool[] pattern = first ? PatternA : PatternB;
        for (int i = 0; i < 11; i++)
        {
            bool value = pass == 0 ? m[start + i, a] : m[a, start + i];
            if (value != pattern[i])
                return false;
        }

        return true;
    }
}
