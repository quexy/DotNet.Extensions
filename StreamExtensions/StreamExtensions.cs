using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System.IO
{
    public static class StreamExtensions
    {
        public static Endianness Endianness = Endianness.Unspecified;

        /// <summary> Reads the specified amount of bytes from the stream </summary>
        /// <exception cref="InvalidOperationException"> if the stream did not contain enough bytes </exception>
        public static byte[] ReadBuffer(this Stream stream, int length)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (length < 0) throw new ArgumentOutOfRangeException("length", "length cannot be negtive");
            if (length == 0) return new byte[0];

            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read == length) return buffer;
            throw new InvalidOperationException(WrongRead(read, length));
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

        /// <summary> Reads a string in the specified encoding from the stream; lenght is the next byte in the stream </summary>
        public static string ReadShortString(this Stream stream, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            return ReadFixString(stream, ReadValue<byte>(stream, endianness), encoding);
        }

        /// <summary> Reads a string in the specified encoding from the stream; lenght (4 bytes) read in the given endianness </summary>
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

        /// <summary> Writes length and the array into the stream; length (4 bytes) is converted to the specified endianness </summary>
        public static Stream WriteBytes(this Stream stream, byte[] buffer, Endianness endianness = Endianness.Unspecified)
        {
            return stream.WriteValue(buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes the string to the stream after padding it to the given length </summary>
        /// <exception cref="ArgumentException"> if the string is longer than specified </exception>
        public static Stream WriteFixString(this Stream stream, string value, int length, Encoding encoding = null)
        {
            if (encoding == null) encoding = GetDefaultEncoding(Endianness.Unspecified);
            value = (value ?? "").PadRight(length, ' ');
            if (value.Length > length) // verify string is exactly 'length' long
                throw new ArgumentException(nameof(value), "the string is too long");
            return stream.WriteBuffer(encoding.GetBytes(value));
        }

        /// <summary> Writes length and the string to the stream; length should fit into a byte </summary>
        public static Stream WriteShortString(this Stream stream, string value, Encoding encoding = null, Endianness endianness = Endianness.Unspecified)
        {
            if (encoding == null) encoding = GetDefaultEncoding(endianness);
            var buffer = encoding.GetBytes(value ?? "");
            if (buffer.Length > byte.MaxValue) //verify length fits into a single byte
                throw new ArgumentOutOfRangeException(nameof(value), "the string is too long");
            return stream.WriteValue((byte)buffer.Length, endianness).WriteBuffer(buffer);
        }

        /// <summary> Writes length and the string to the stream; length is converted to the specified endianness </summary>
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

#if NETSTANDARD1_0
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            if (typeInfo.IsClass)
                throw new NotSupportedException("Reference types are not supported");
            if (type.Name == typeof(Nullable<>).Name)
                throw new InvalidOperationException("Nullable<> is not supported");

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

#if NETSTANDARD1_0
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            if (typeInfo.IsClass)
                throw new NotSupportedException("Reference types are not supported");
            if (type.Name == typeof(Nullable<>).Name)
                throw new InvalidOperationException("Nullable<> is not supported");

            if (typeInfo.IsEnum) type = Enum.GetUnderlyingType(type);

            if (type == typeof(bool)) return BitConverter.ToBoolean(buffer, startIndex);
            if (type == typeof(byte)) return buffer[startIndex];
            if (type == typeof(char)) return BitConverter.ToChar(buffer, startIndex);
            if (type == typeof(DateTime)) return new DateTime(ChangeType<long>(buffer));
            if (type == typeof(decimal)) return new decimal(Enumerable.Range(0, startIndex)
                    .Select(n => BitConverter.ToInt32(buffer, startIndex + n * 4)).ToArray());
            if (type == typeof(double)) return BitConverter.ToDouble(buffer, startIndex);
            if (type == typeof(float)) return BitConverter.ToSingle(buffer, startIndex);
            if (type == typeof(Guid))
            {
                var bytes = buffer.Skip(startIndex).Take(16).ToArray();
                // 'bytes' is platform endian; convert to big endian
                var bigEndBytes = (BitConverter.IsLittleEndian)
                    ? bytes.Reverse().ToArray() : bytes;
                var mixedBytes = Enumerable.Empty<byte>()
                    .Concat(bigEndBytes.Skip(0).Take(4).Reverse())
                    .Concat(bigEndBytes.Skip(4).Take(2).Reverse())
                    .Concat(bigEndBytes.Skip(6).Take(2).Reverse())
                    .Concat(bigEndBytes.Skip(8).Take(2))
                    .Concat(bigEndBytes.Skip(10).Take(6));
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

#if NETSTANDARD1_0
            var typeInfo = type.GetTypeInfo();
#else
            var typeInfo = type;
#endif
            if (typeInfo.IsClass)
                throw new NotSupportedException("Reference types are not supported");
            if (type.Name == typeof(Nullable<>).Name)
                throw new InvalidOperationException("Nullable<> is not supported");

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
                var bigEndBytes = Enumerable.Empty<byte>()
                    .Concat(mixedBytes.Skip(0).Take(4).Reverse())
                    .Concat(mixedBytes.Skip(4).Take(2).Reverse())
                    .Concat(mixedBytes.Skip(6).Take(2).Reverse())
                    .Concat(mixedBytes.Skip(8).Take(2))
                    .Concat(mixedBytes.Skip(10).Take(6));
                // result has to be platform endian
                return (BitConverter.IsLittleEndian)
                    ? bigEndBytes.Reverse().ToArray() : bigEndBytes.ToArray();
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
                if (end != Endianness.BigEndian) return data;
                else /* reverse */ return data.Reverse().ToArray();
            }
            else // architecture is big endian
            {
                if (end != Endianness.LittleEndian) return data;
                else /* reverse */ return data.Reverse().ToArray();
            }
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
