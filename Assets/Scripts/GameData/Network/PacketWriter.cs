using System;
using System.Text;

namespace GameData.Network
{
    public class PacketWriter
    {
        private byte[] buffer;
        private int position;

        public PacketWriter(int initialSize = 256)
        {
            buffer = new byte[initialSize];
            position = 0;
        }

        public void WriteByte(byte value)
        {
            EnsureCapacity(1);
            buffer[position++] = value;
        }

        public void WriteShort(short value)
        {
            EnsureCapacity(2);
            buffer[position++] = (byte)(value & 0xFF);
            buffer[position++] = (byte)((value >> 8) & 0xFF);
        }

        public void WriteInt(int value)
        {
            EnsureCapacity(4);
            buffer[position++] = (byte)(value & 0xFF);
            buffer[position++] = (byte)((value >> 8) & 0xFF);
            buffer[position++] = (byte)((value >> 16) & 0xFF);
            buffer[position++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteLong(long value)
        {
            EnsureCapacity(8);
            for (int i = 0; i < 8; i++)
            {
                buffer[position++] = (byte)((value >> (i * 8)) & 0xFF);
            }
        }

        public void WriteString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteShort(0);
                return;
            }

            byte[] bytes = Encoding.ASCII.GetBytes(value);
            WriteShort((short)bytes.Length);
            WriteBytes(bytes);
        }

        public void WriteBytes(byte[] data)
        {
            EnsureCapacity(data.Length);
            Array.Copy(data, 0, buffer, position, data.Length);
            position += data.Length;
        }

        public void WritePosition(float x, float y)
        {
            WriteShort((short)x);
            WriteShort((short)y);
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[position];
            Array.Copy(buffer, 0, result, 0, position);
            return result;
        }

        private void EnsureCapacity(int additionalBytes)
        {
            if (position + additionalBytes > buffer.Length)
            {
                int newSize = Math.Max(buffer.Length * 2, position + additionalBytes);
                byte[] newBuffer = new byte[newSize];
                Array.Copy(buffer, 0, newBuffer, 0, position);
                buffer = newBuffer;
            }
        }
    }
}