using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.IO
{
    public static class StreamExtensions
    {
        private static emptyArray = new byte[0];
        public static Endianness Endianness = Endianness.Unspecified;

        /// <summary> Reads the specified amount of bytes from the stream </summary>
        /// <exception cref="InvalidOperationException"> if the stream did not contain enough bytes </exception>
        public static byte[] ReadBuffer(this Stream stream, int length)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be negtive");
            if (length == 0) return emptyArray;

            var offset = 0;
            var buffer = new byte[length];
            while (offset < length)
            {
                // Stream.Read() might return less than the requested amount.
                // Guaranteed to read at least one byte unless end of stream.
                var read = stream.Read(buffer, offset, buffer.Length - offset);
                offset += read; // store our progress, migth need another try
                
                if (read != 0) continue; // ...or fail if end of stream
                throw new InvalidOperationException(WrongRead(read, length));
            }
            return buffer;
        }

        /// <summary> Reads an array from the stream; length is the next one byte in the specified endianness </summary>
        public static byte[] ReadShortBytes(this Stream stream, Endianness endianness = Endianness.Unspecified)
        {
            return stream.ReadBuffer(stream.ReadValue<byte>(endianness));
        }

        /// <summary> Reads an array from the stream; length is the next four bytes in the specified endianness </summary>
        public static byte[] ReadBytes(this Stream stream, Endianness endianness = Endianness.Unspecified)
        {
            return stream.ReadBuffer(stream.ReadValue<int>(endianness));
        }

        /// <summary> Reads the given amount of bytes to a string by the specified encoding </summary>
        public static string ReadFixString(this Stream stream, int length, Encoding encoding = null)
        {
            if (encoding == null) encoding = GetDefaultEncoding(Endianness.Unspecified);
            return encoding.GetString(stream.ReadBuffer(length), 0, length);
        }

        /// <summary> Reads a string in the specified encoding from the stream; length is the next byte in the stream </summary>
        public static string ReadShortString(this Stream stream, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            return stream.ReadFixString(ReadValue<byte>(stream, endianness), encoding);
        }

        /// <summary> Reads a string in the specified encoding from the stream; length (4 bytes) read in the given endianness </summary>
        public static string ReadString(this Stream stream, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            return stream.ReadFixString(stream.ReadValue<int>(endianness), encoding);
        }

        /// <summary> Reads the given type from the stream in the specified endianness </summary>
        public static T ReadValue<T>(this Stream stream, Endianness endianness = Endianness.Unspecified) where T : struct
        {
            return stream.ReadBuffer(GetLength<T>()).FixEndianness(endianness).ChangeType<T>();
        }

        /// <summary> Writes the array to the stream </summary>
        public static Stream WriteBuffer(this Stream stream, byte[] buffer)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (buffer != null && buffer.Length > 0)
                stream.Write(buffer, 0, buffer.Length);
            return stream;
        }

        /// <summary> Writes the length and the array into the stream; length should fit into a single byte </summary>
        /// <exception cref="ArgumentOutOfRangeException"> if the buffer's length doesn't fit into a single byte </exception>
        public static Stream WriteShortBytes(this Stream stream, byte[] buffer, Endianness endianness = Endianness.Unspecified)
        {
            if (buffer.Length > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(buffer), "the buffer is too long");
            return stream.WriteValue((byte)buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes the length and the array into the stream; length (4 bytes) is converted to the specified endianness </summary>
        public static Stream WriteBytes(this Stream stream, byte[] buffer, Endianness endianness = Endianness.Unspecified)
        {
            return stream.WriteValue(buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes the given amount of bytes of the string padded with spaces to the stream </summary>
        public static Stream WriteFixString(this Stream stream, string value, int length, Encoding encoding = null)
        {
            if (encoding == null) encoding = GetDefaultEncoding(Endianness.Unspecified);
            var buffer = encoding.GetBytes((value ?? "").PadRight(length, ' '));
            return stream.WriteBuffer(buffer.Take(length).ToArray());
        }

        /// <summary> Writes the length and the string to the stream; length should fit into one byte </summary>
        /// <exception cref="ArgumentOutOfRangeException"> if the string's length (along the encoding) doesn't fit into a single byte </exception>
        public static Stream WriteShortString(this Stream stream, string value, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            var buffer = encoding.GetBytes(value ?? "");
            if (buffer.Length > byte.MaxValue) // verify length fits into a single byte
                throw new ArgumentOutOfRangeException(nameof(value), "the string is too long");
            return stream.WriteValue((byte)buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes the length and the string to the stream; length (4 bytes) is converted to the specified endianness </summary>
        public static Stream WriteString(this Stream stream, string value, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            var buffer = encoding.GetBytes(value ?? "");
            return stream.WriteValue(buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes the given value to the stream in the specified endianness </summary>
        public static Stream WriteValue<T>(this Stream stream, T value, Endianness endianness = Endianness.Unspecified) where T : struct
        {
            return stream.WriteBuffer(GetBytes(value).FixEndianness(endianness));
        }

        private static int GetLength<T>() where T : struct { return GetLength(typeof(T)); }
        private static int GetLength(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            VerifySupported(type); // not a class, interface, or Nullable<T>

            if (typeInfo.IsEnum) type = Enum.GetUnderlyingType(type);

            if (type == typeof(bool)) return 1;
            if (type == typeof(byte)) return 1;
            if (type == typeof(char)) return 2;
            if (type == typeof(DateTime)) return GetLength<long>();
            if (type == typeof(decimal)) return 16;
            if (type == typeof(double)) return 8;
            if (type == typeof(float)) return 4;
            if (type == typeof(Guid)) return 16;
            if (type == typeof(int)) return 4;
            if (type == typeof(long)) return 8;
            if (type == typeof(sbyte)) return 1;
            if (type == typeof(short)) return 2;
            if (type == typeof(uint)) return 4;
            if (type == typeof(ulong)) return 8;
            if (type == typeof(ushort)) return 2;

            throw new NotSupportedException("Not supported type " + type.Name);
        }

        /// <summary> Converts the given array to the specified type; byte order: platform endian </summary>
        public static T ChangeType<T>(this byte[] buffer, int startIndex = 0) where T : struct { return (T)ChangeType(buffer, typeof(T), startIndex); }
        private static object ChangeType(byte[] buffer, Type type, int startIndex)
        {
            if (type == null) throw new ArgumentNullException("type");
            VerifySupported(type); // not a class, interface, or Nullable<T>

            if (typeInfo.IsEnum) type = Enum.GetUnderlyingType(type);

            if (type == typeof(bool)) return BitConverter.ToBoolean(buffer, startIndex);
            if (type == typeof(byte)) return buffer[startIndex];
            if (type == typeof(char)) return BitConverter.ToChar(buffer, startIndex);
            if (type == typeof(DateTime)) return new DateTime(ChangeType<long>(buffer, startIndex));
            if (type == typeof(decimal)) return new decimal(Enumerable.Range(0, 4)
                    .Select(n => BitConverter.ToInt32(buffer, startIndex + n * 4)).ToArray());
            if (type == typeof(double)) return BitConverter.ToDouble(buffer, startIndex);
            if (type == typeof(float)) return BitConverter.ToSingle(buffer, startIndex);
            if (type == typeof(Guid))
            {
                var bytes = buffer.Skip(startIndex).Take(16).ToArray();
                // if platform is little endian, then reverse to big endian
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                var mixedBytes = Enumerable.Empty<byte>()
                    .Concat(bytes.Skip(0).Take(4).Reverse())
                    .Concat(bytes.Skip(4).Take(2).Reverse())
                    .Concat(bytes.Skip(6).Take(2).Reverse())
                    .Concat(bytes.Skip(8).Take(2))
                    .Concat(bytes.Skip(10).Take(6));
                return new Guid(mixedBytes.ToArray());
            }
            if (type == typeof(int)) return BitConverter.ToInt32(buffer, startIndex);
            if (type == typeof(long)) return BitConverter.ToInt64(buffer, startIndex);
            if (type == typeof(sbyte))
            {
                var value = ((sbyte)(buffer[startIndex] & 0x7f));
                var negative = (buffer[startIndex] & 0x80) > 0;
                return (negative) ? ~value : value;
            }
            if (type == typeof(short)) return BitConverter.ToInt16(buffer, startIndex);
            if (type == typeof(uint)) return BitConverter.ToUInt32(buffer, startIndex);
            if (type == typeof(ulong)) return BitConverter.ToUInt64(buffer, startIndex);
            if (type == typeof(ushort)) return BitConverter.ToUInt16(buffer, startIndex);

            throw new InvalidOperationException("Not supported type " + type.Name);
        }

        /// <summary> Converts the given value into a byte array; byte order: platform endian </summary>
        public static byte[] GetBytes<T>(this T value) where T : struct { return GetBytes(value, typeof(T)); }
        private static byte[] GetBytes(object value, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            VerifySupported(type); // not a class, interface, or Nullable<T>

            if (typeInfo.IsEnum) type = Enum.GetUnderlyingType(type);

            if (type == typeof(bool)) return BitConverter.GetBytes((bool)value);
            if (type == typeof(byte)) return new[] { (byte)value };
            if (type == typeof(char)) return BitConverter.GetBytes((char)value);
            if (type == typeof(DateTime)) return BitConverter.GetBytes(((DateTime)value).Ticks);
            if (type == typeof(decimal)) return decimal.GetBits((decimal)value)
                    .SelectMany(b => BitConverter.GetBytes(b)).ToArray();
            if (type == typeof(double)) return BitConverter.GetBytes((double)value);
            if (type == typeof(float)) return BitConverter.GetBytes((float)value);
            if (type == typeof(Guid))
            {
                var mixedBytes = ((Guid)value).ToByteArray();
                var bytes = Enumerable.Empty<byte>()
                    .Concat(mixedBytes.Skip(0).Take(4).Reverse())
                    .Concat(mixedBytes.Skip(4).Take(2).Reverse())
                    .Concat(mixedBytes.Skip(6).Take(2).Reverse())
                    .Concat(mixedBytes.Skip(8).Take(2))
                    .Concat(mixedBytes.Skip(10).Take(6));
                // 'bytes' is big endian, result has to be platform endian
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return bytes;
            }
            if (type == typeof(int)) return BitConverter.GetBytes((int)value);
            if (type == typeof(long)) return BitConverter.GetBytes((long)value);
            if (type == typeof(sbyte))
            {
                var input = (int)((sbyte)value);
                var neg = input < 0 ? 0x80 : 0;
                var val = (input < 0) ? ~input : input;
                return new[] { (byte)(val + neg) };
            }
            if (type == typeof(short)) return BitConverter.GetBytes((short)value);
            if (type == typeof(uint)) return BitConverter.GetBytes((uint)value);
            if (type == typeof(ulong)) return BitConverter.GetBytes((ulong)value);
            if (type == typeof(ushort)) return BitConverter.GetBytes((ushort)value);

            throw new InvalidOperationException("Not supported type " + type.Name);
        }

        private static string WrongRead(int read, int length)
        {
            return string.Format("Read {0} bytes instead of {1}", read, length);
        }

        private static byte[] FixEndianness(this byte[] data, Endianness endianness)
        {
            var end = (endianness == Endianness.Unspecified)
                ? Endianness : endianness;

            if (end == Endianness.Unspecified) return data;
            if (data.Length < 2) return data;

            if (BitConverter.IsLittleEndian)
            {
                // reverse if big endian required
                if (end == Endianness.BigEndian)
                    Array.Reverse(data);
            }
            else // architecture is big endian
            {
                // reverse if little endian required
                if (end == Endianness.LittleEndian)
                    Array.Reverse(data);
            }
            return data;
        }

        private static Encoding GetDefaultEncoding(Endianness endianness)
        {
            var end = (endianness == Endianness.Unspecified)
                ? Endianness : endianness;

            if (BitConverter.IsLittleEndian)
                return (end == Endianness.BigEndian)
                    ? Encoding.BigEndianUnicode
                    : Encoding.Unicode;
            else // architecture is big endian
                return (end != Endianness.LittleEndian)
                    ? Encoding.BigEndianUnicode
                    : Encoding.Unicode;
        }

        private static void VerifySupported(Type type)
        {
#if NETSTANDARD1_0
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            if (typeInfo.IsClass)
                throw new NotSupportedException("Reference types are not supported");
            if (typeInfo.IsInterface)
                throw new NotSupportedException("Interfaces are not supported");
            if (type.Name == typeof(Nullable<>).Name)
                throw new InvalidOperationException("Nullable<> is not supported");
        }
    }
}

namespace System
{
    public enum Endianness : byte
    {
        Unspecified = 0,
        BigEndian = 1,
        LittleEndian = 2,
    }
}
