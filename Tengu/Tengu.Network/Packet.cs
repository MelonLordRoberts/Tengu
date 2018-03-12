﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Tengu.Network
{
    // A class used to encapsulate network data
    public class Packet
    {
        public short BaseID { get; set; }
        public short SubID { get; set; }
        public short Length = 0;
        public byte[] Body { get; set; }
        public int BufferLength = 1024;
        public int ReadIndex;
        public List<byte[]> BodyBuilder { get; set; }
        public Socket Source { get; set; }
        private enum DataType
        {
            Int16 = 0, Int32 = 1, Int64 = 2, Double = 3,
            Float = 4, Bool = 5, UTF8 = 6,
        };

        public Packet()
        {
            // Arbitrary value to accommodate incoming data
            Body = new byte[BufferLength];
            BodyBuilder = new List<byte[]>();
            ReadIndex = 0;
        }
        public byte[] Compose()
        {
            // Get the bytes of the packet IDs 
            byte[] baseBytes = BitConverter.GetBytes(BaseID);
            byte[] subBytes = BitConverter.GetBytes(SubID);

            // Get byte[] of all data added to packet
            Body = ConcatenateBody();

            // Get total length of packet
            short totalLength = (short)(6 + Body.Length);

            // Allocate byte[] for the packet data
            byte[] packetBytes = new byte[totalLength];
            // Get bytes of Packet Length short
            byte[] lengthBytes = BitConverter.GetBytes(totalLength);

            // Copy data into outgoing packetByte
            Buffer.BlockCopy(baseBytes, 0, packetBytes, 0, 2);
            Buffer.BlockCopy(subBytes, 0, packetBytes, 2, 2);
            Buffer.BlockCopy(lengthBytes, 0, packetBytes, 4, 2);
            Buffer.BlockCopy(Body, 0, packetBytes, 6, Body.Length);

            return packetBytes;
        }
        public byte[] ConcatenateBody()
        {
            // Allocate byte[] for the actual length of the data
            int len = 0;
            foreach (var byteArray in BodyBuilder)
            {
                len += byteArray.Length;
            }
            byte[] body = new byte[len];

            // Copy each byte[] to body
            int index = 0;
            foreach (var byteArray in BodyBuilder)
            {
                Buffer.BlockCopy(byteArray, 0, body, index, byteArray.Length);
                index += byteArray.Length;
            }

            return body;
        }

        public void AddStringUTF8(string data)
        {
            byte[] header = new byte[1];
            header[0] = (byte)DataType.UTF8;

            byte[] stringData = Encoding.UTF8.GetBytes(data);
            byte[] len = BitConverter.GetBytes((short)stringData.Length);

            BodyBuilder.Add(header);
            BodyBuilder.Add(len);
            BodyBuilder.Add(stringData);
        }
        public void AddValue(object data)
        {
            byte[] numBytes;
            byte[] header;
            switch (data)
            {
                case Int16 a:
                    header = new byte[] { (int)DataType.Int16 }; 
                    numBytes = BitConverter.GetBytes(a);
                    break;
                case Int32 b:
                    header = new byte[] { (int)DataType.Int32 };
                    numBytes = BitConverter.GetBytes(b);
                    break;
                case Int64 c:
                    header = new byte[] { (int)DataType.Int64 };
                    numBytes = BitConverter.GetBytes(c);
                    break;
                case float d:
                    header = new byte[] { (int)DataType.Float };
                    numBytes = BitConverter.GetBytes(d);
                    break;
                case Double e:
                    header = new byte[] { (int)DataType.Double };
                    numBytes = BitConverter.GetBytes(e);
                    break;
                default:
                    throw new NotSupportedException("This Numeric is not supported");
            }
            BodyBuilder.Add(header);
            BodyBuilder.Add(numBytes);
        }
        public void ReadHeader()
        {
            BaseID = ReadShort();
            SubID = ReadShort();
            Length = ReadShort();
        }
        public List<object> Read()
        {
            List<object> values = new List<object>();

            while (ReadIndex <= Length - 1)
            {
                values.Add(ReadValue());
            }

            return values;
        }

        private object ReadValue()
        {
            DataType valueType = (DataType)Body[ReadIndex];
            ReadIndex += 1;
            object value;
            switch (valueType)
            {
                case DataType.Int16:
                    value = ReadShort();
                    break;
                case DataType.Int32:
                    value = ReadInt();
                    break;
                case DataType.Int64:
                    value = ReadLong();
                    break;
                case DataType.Float:
                    value = ReadFloat();
                    break;
                case DataType.Double:
                    value = ReadDouble();
                    break;
                case DataType.Bool:
                    value = ReadBool();
                    break;
                case DataType.UTF8:
                    value = ReadStringUTF8();
                    break;

                default:
                    value = null;
                    break;
            }

            return value;
        }
        private string ReadStringUTF8()
        {
            short stringLength = ReadShort();
            byte[] stringBytes = new byte[stringLength];

            Buffer.BlockCopy(Body, ReadIndex, stringBytes, 0, stringLength);
            ReadIndex += stringLength;
            string data = Encoding.UTF8.GetString(stringBytes);

            return data;
        }
        private short ReadShort()
        {
            short number = BitConverter.ToInt16(Body, ReadIndex);
            ReadIndex += 2;
            return number;
        }
        private int ReadInt()
        {
            int number = BitConverter.ToInt32(Body, ReadIndex);
            ReadIndex += 4;
            return number;
        }
        private double ReadDouble()
        {
            double number = BitConverter.ToInt64(Body, ReadIndex);
            ReadIndex += 8;
            return number;
        }
        private float ReadFloat()
        {
            float number = BitConverter.ToSingle(Body, ReadIndex);
            ReadIndex += 4;
            return number;
        }
        private bool ReadBool()
        {
            bool value = BitConverter.ToBoolean(Body, ReadIndex);
            ReadIndex += 1;

            return value;
        }
        private long ReadLong()
        {
            long value = BitConverter.ToInt64(Body, ReadIndex);
            ReadIndex += 8;

            return value;
        }
    }
}