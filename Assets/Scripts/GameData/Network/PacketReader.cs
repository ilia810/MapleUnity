using System;
using System.Text;

namespace GameData.Network
{
    public class PacketReader
    {
        private byte[] data;
        private int position;

        public PacketReader(byte[] data)
        {
            this.data = data;
            this.position = 0;
        }

        public int Available => data.Length - position;

        public byte ReadByte()
        {
            if (position >= data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            return data[position++];
        }

        public short ReadShort()
        {
            if (position + 2 > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            
            short value = (short)(data[position] | (data[position + 1] << 8));
            position += 2;
            return value;
        }

        public int ReadInt()
        {
            if (position + 4 > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            
            int value = data[position] | 
                       (data[position + 1] << 8) | 
                       (data[position + 2] << 16) | 
                       (data[position + 3] << 24);
            position += 4;
            return value;
        }

        public long ReadLong()
        {
            if (position + 8 > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            
            long value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= ((long)data[position + i] << (i * 8));
            }
            position += 8;
            return value;
        }

        public string ReadString()
        {
            short length = ReadShort();
            if (length == 0) return string.Empty;
            
            if (position + length > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            
            string value = Encoding.ASCII.GetString(data, position, length);
            position += length;
            return value;
        }

        public byte[] ReadBytes(int count)
        {
            if (position + count > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
            
            byte[] result = new byte[count];
            Array.Copy(data, position, result, 0, count);
            position += count;
            return result;
        }

        public void Skip(int count)
        {
            position += count;
            if (position > data.Length)
                throw new IndexOutOfRangeException("Packet read past end");
        }

        public (float x, float y) ReadPosition()
        {
            short x = ReadShort();
            short y = ReadShort();
            return (x, y);
        }
    }
}