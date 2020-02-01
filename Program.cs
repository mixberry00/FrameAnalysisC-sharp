using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace KS_lr4
{
    class Program
    {
        public struct Frame
        {
            public byte[] MacDest;
            public byte[] MacSource;
            public byte[] FrType;
            public byte VersHL;
            public byte TypeS;
            public byte[] DLen;
            public byte[] IPfr;
            public byte Lftime;
            public byte Prot;
            public byte[] Csum;
            public byte[] IPdest;
            public byte[] IPsource;
            //переменные для заголовков фреймов форматов Ethernet_802.2 и Ethernet_SNAP
            public byte DSAP;
            public byte SSAP;
            //переменные для заголовка UDP
            public byte[] UDPportsource;
            public byte[] UDPportdest;
            public byte[] UDPlen;
            public byte[] UDPcs;
        }

        static void Main(string[] args)
        {
            string path = Environment.CurrentDirectory;
            using (BinaryReader reader = new BinaryReader(File.Open(path + "\\ethers01.bin", FileMode.Open)))
            {
                int i = 0;
                int cntE2 = 0, chtE802 = 0, cntE802LLC = 0, cntESNAP = 0;
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    Console.WriteLine($"\nFrame number:{i + 1}");
                    var tmp = new Frame
                    {
                        MacDest = new byte[6],
                        MacSource = new byte[6],
                        FrType = new byte[2],
                        VersHL = new byte(),
                        TypeS = new byte(),
                        DLen = new byte[2],
                        IPfr = new byte[4],
                        Lftime = new byte(),
                        Prot = new byte(),
                        Csum = new byte[2],
                        IPdest = new byte[4],
                        IPsource = new byte[4],
                        DSAP = new byte(),
                        SSAP = new byte(),
                        UDPportsource = new byte[2],
                        UDPportdest = new byte[2],
                        UDPlen = new byte[2],
                        UDPcs = new byte[2],

                    };
                    byte[] buf = new byte[6];
                    buf = reader.ReadBytes(6);
                    if (BitConverter.ToString(buf) != "00-00-00-00-00-00")
                        reader.BaseStream.Position -= 6;
                    tmp.MacDest = reader.ReadBytes(6);
                    tmp.MacSource = reader.ReadBytes(6);
                    tmp.FrType = reader.ReadBytes(2);
                    if ((tmp.FrType[0] << 8 | tmp.FrType[1]) > 0x05FE)
                    {
                        //Ethernet_2
                        Process(ref tmp, reader);
                        cntE2++;
                    }
                    else
                    {
                        tmp.DSAP = reader.ReadByte();
                        tmp.SSAP = reader.ReadByte();
                        if ((tmp.DSAP << 8 | tmp.SSAP) == 0xFFFF)
                        {
                            //Ethernet_802.3
                            reader.BaseStream.Position += (tmp.FrType[0] << 8 | tmp.FrType[1]) - 2;
                            chtE802++;
                        }
                        else
                        {
                            if (tmp.DSAP == 0xAA && tmp.SSAP == 0xAA)
                            {
                                //Ethernet_SNAP
                                reader.BaseStream.Position += (tmp.FrType[0] << 8 | tmp.FrType[1]) + 6;
                                cntESNAP++;
                            }
                            else
                            {
                                //Ethernet_802.3/LLC
                                reader.BaseStream.Position += (tmp.FrType[0] << 8 | tmp.FrType[1]) + 1;
                                cntE802LLC++;
                            }
                        }
                    }
                    Print(ref tmp);
                    i++;
                }
                Console.WriteLine($"\nКол-во Ethernet_2: {cntE2}");
                Console.WriteLine($"Кол-во Ethernet_802.3: {chtE802}");
                Console.WriteLine($"Кол-во Ethernet_802.3/LLC: {cntE802LLC}");
                Console.WriteLine($"Кол-во Ethernet_SNAP: {cntESNAP}");
                Console.Read();
            }
        }
        public static void Process(ref Frame tmp, BinaryReader reader)
        {
            tmp.VersHL = reader.ReadByte();
            tmp.TypeS = reader.ReadByte();
            tmp.DLen = reader.ReadBytes(2);
            tmp.IPfr = reader.ReadBytes(4);
            tmp.Lftime = reader.ReadByte();
            tmp.Prot = reader.ReadByte();
            tmp.Csum = reader.ReadBytes(2);
            tmp.IPdest = reader.ReadBytes(4);
            tmp.IPsource = reader.ReadBytes(4);
            tmp.UDPportdest = reader.ReadBytes(2);
            tmp.UDPportsource = reader.ReadBytes(2);
            tmp.UDPlen = reader.ReadBytes(2);
            tmp.UDPcs = reader.ReadBytes(2);
            reader.BaseStream.Position += (tmp.DLen[0] << 8 | tmp.DLen[1]) - 28;
        }
        public static void Print(ref Frame tmp)
        {
            Console.WriteLine("Данные заголовка:");
            Console.WriteLine($"Mac-адрес получателя:{BitConverter.ToString(tmp.MacDest)}");
            Console.WriteLine($"Mac-адрес отправителя:{BitConverter.ToString(tmp.MacSource)}");
            Console.WriteLine($"Тип фрейма/длина:{BitConverter.ToString(tmp.FrType)}");
            if ((tmp.FrType[0] << 8 | tmp.FrType[1]) > 0x05FE)
            {
                Console.WriteLine("\nДанные заголовка ip-дейтаграммы:");
                Console.WriteLine($"Тип службы:{tmp.TypeS}, Длина дейтаграммы: {(tmp.DLen[0] << 8 | tmp.DLen[1])}(в виде целого числа)");
                Console.WriteLine($"Время жизни:{tmp.Lftime}, Протокол: {tmp.Prot}, Контрольная сумма заголовка: {BitConverter.ToString(tmp.Csum)}");
                Console.WriteLine($"IP-адрес отправителя:{tmp.IPsource[0]}.{tmp.IPsource[1]}.{tmp.IPsource[2]}.{tmp.IPsource[3]}");
                Console.WriteLine($"IP-адрес получателя:{tmp.IPdest[0]}.{tmp.IPdest[1]}.{tmp.IPdest[2]}.{tmp.IPdest[3]}");
                Console.WriteLine("\nДанные заголовка UDP-дейтаграммы:");
                Console.WriteLine($"UDP-порт отправителя:{BitConverter.ToString(tmp.UDPportdest)}, UDP-порт получателя:{BitConverter.ToString(tmp.UDPportsource)}");
                Console.WriteLine($"Длина UDP-сообщения: {(tmp.UDPlen[0] << 8 | tmp.UDPlen[1])}, Контрольная сумма:{BitConverter.ToString(tmp.UDPcs)}");
                Console.WriteLine("==================*************==================");
            }
        }
    }
}
