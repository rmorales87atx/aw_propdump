/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace AwPropdump;

using System.Globalization;
using System.Text;

/// <summary>
/// Parse object data from an ActiveWorlds property dump ("propdump")
/// </summary>
public class PropdumpParser : IDisposable {
    private int? _fileVersion;
    private readonly Encoding _fileEncoding = Encoding.GetEncoding(1252);
    private readonly BinaryReader _streamReader;

    public PropdumpParser(Stream source)
    {
        _streamReader = new BinaryReader(source);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _streamReader.Dispose();
    }

    public IEnumerable<PropdumpObject> ReadObjects()
    {
        long sequence = 1;

        _fileVersion ??= GetFileVersion();

        var buffer = new byte[65536];
        int nbytes = 0;
        do {
            nbytes = GetFileLine(buffer);
            if (nbytes > 0) {
                yield return ParseObject(sequence, buffer);
                sequence++;
            }
        } while (nbytes != 0);
    }

    private byte? ReadByte()
    {
        try {
            return _streamReader.ReadByte();
        } catch (EndOfStreamException) {
            return null;
        }
    }

    private int GetFileLine(Span<byte> buffer)
    {
        var idx = 0;
        buffer.Clear();
        while (_streamReader.BaseStream.CanRead) {
            if (idx >= buffer.Length) {
                return idx;
            }

            var currentByte = ReadByte();
            if (currentByte == null || currentByte == '\n') {
                break;
            }

            byte? nextByte = ReadByte();
            if (currentByte == 128 && nextByte == 127) {
                // propdump tools convert "embedded" newline sequences (\r\n) to {0x80,0x7f}
                // these need to be converted as we read from the file
                buffer[idx++] = (byte)'\r';
                buffer[idx++] = (byte)'\n';
            } else {
                if (currentByte != '\r' && currentByte != '\n') {
                    buffer[idx++] = currentByte.Value;
                }

                if (nextByte != null && nextByte != '\r' && nextByte != '\n') {
                    buffer[idx++] = nextByte.Value;
                }
            }

            if (nextByte == '\n') {
                break;
            }
        }

        return idx;
    }

    private int GetFileVersion()
    {
        var buffer = new byte[255];
        GetFileLine(buffer);

        var tokens = _fileEncoding.GetString(buffer).Split(' ');
        if (tokens == null || tokens.Length == 0) {
            throw new PropdumpException("Expected first line of propdump to be non-empty");
        }

        if (tokens is not ["propdump", "version", _]) {
            throw new PropdumpException("Unknown or invalid propdump version");
        }

        if (!int.TryParse(tokens[2], NumberStyles.None, null, out var result)) {
            throw new PropdumpException("Unknown or invalid propdump version");
        }

        return result;

    }

    private PropdumpObject ParseObject(long sequence, ReadOnlySpan<byte> buffer)
    {
        var line = _fileEncoding.GetString(buffer);
        var tokens =  _fileVersion switch {
            2 => line.Split(' ', 10),
            3 => line.Split(' ', 12),
            4 or 5 => line.Split(' ', 14),
            _ => throw new PropdumpException($"Error parsing line {sequence}")
        };

        Span<int> lengthIndicators = stackalloc int[4];

        // length of "model" string
        lengthIndicators[0] = _fileVersion switch {
            2 => int.Parse(tokens[6]),
            3 => int.Parse(tokens[8]),
            4 or 5 => int.Parse(tokens[9]),
            _ => 0
        };

        // length of "description" string
        lengthIndicators[1] = _fileVersion switch {
            2 => int.Parse(tokens[7]),
            3 => int.Parse(tokens[9]),
            4 or 5 => int.Parse(tokens[10]),
            _ => 0
        };

        // length of "action" string
        lengthIndicators[2] = _fileVersion switch {
            2 => int.Parse(tokens[8]),
            3 => int.Parse(tokens[10]),
            4 or 5 => int.Parse(tokens[11]),
            _ => 0
        };

        // length of V4 object data string
        lengthIndicators[3] = _fileVersion switch {
            4 or 5 => int.Parse(tokens[12]),
            _ => 0
        };

        // the "length indicators" (model, description, action, & data length)
        // seem to assume specific byte positions that are otherwise "incompatible" with Unicode/UTF8

        using var stringData = new MemoryStream(_fileEncoding.GetBytes(tokens.Last()));

        // construct object information
        var obj = new PropdumpObject {
            OwnerCitizenNumber = int.Parse(tokens[0]),
            BuildTimestamp = long.Parse(tokens[1]),
            XCoord = int.Parse(tokens[2]),
            YCoord = int.Parse(tokens[3]),
            ZCoord = int.Parse(tokens[4]),
            YOrient = int.Parse(tokens[5]),
            XOrient = _fileVersion switch {
                >= 3 and <= 5 => int.Parse(tokens[6]),
                _ => null
            },
            ZOrient = _fileVersion switch {
                >= 3 and <= 5 => int.Parse(tokens[7]),
                _ => null
            },
            ObjectType = _fileVersion switch {
                4 or 5 => int.Parse(tokens[8]),
                _ => 0
            },
        };

        Span<byte> tempBytes = stackalloc byte[131072];

        if (lengthIndicators[0] > 0) {
            tempBytes.Clear();
            stringData.ReadExactly(tempBytes.Slice(0, lengthIndicators[0]));
            obj.Model = _fileEncoding.GetString(tempBytes.Slice(0, lengthIndicators[0]));
        }

        if (lengthIndicators[1] > 0) {
            tempBytes.Clear();
            stringData.ReadExactly(tempBytes.Slice(0, lengthIndicators[1]));
            obj.Description = _fileEncoding.GetString(tempBytes.Slice(0, lengthIndicators[1]));
        }

        if (lengthIndicators[2] > 0) {
            tempBytes.Clear();
            stringData.ReadExactly(tempBytes.Slice(0, lengthIndicators[2]));
            obj.Action = _fileEncoding.GetString(tempBytes.Slice(0, lengthIndicators[2]));
        }

        if (lengthIndicators[3] > 0) {
            tempBytes.Clear();
            stringData.ReadExactly(tempBytes.Slice(0, lengthIndicators[3]));
            obj.V4Data = Convert.ToBase64String(tempBytes.Slice(0, lengthIndicators[3]));
        }

        return obj;
    }
}
