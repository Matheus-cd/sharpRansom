using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Dns
{
    public static class DNSHelper
    {
        private const ushort DnsTypeTXT = 16;
        private const ushort DnsClassINET = 1;
        private const uint DefaultTTL = 300;
        private const ushort DnsOpcodeUpdate = 5;
        private const ushort RcodeSuccess = 0;

        /// <summary>
        /// Sends the encryption key and IV via DNS TXT record update.
        /// </summary>
        public static void SendKeyAndIV(string subdomain, string base64Key, string base64IV, string domain, string dnsServer)
        {
            var message = BuildDnsUpdateMessage(subdomain, base64Key, base64IV, domain);
            var response = SendDnsMessage(message, dnsServer);

            if (response == null)
            {
                throw new Exception("Falha ao enviar atualizacao DNS: resposta nula");
            }

            var rcode = (ushort)((response[2] & 0x0F));
            if (rcode != RcodeSuccess)
            {
                throw new Exception($"Atualizacao DNS falhou, codigo de resposta: {RcodeToString(rcode)}");
            }

            Console.WriteLine($"Chave e IV enviados com sucesso para subdominio: {subdomain}");
        }

        /// <summary>
        /// Builds a DNS UPDATE message with TXT records for key and IV.
        /// </summary>
        private static byte[] BuildDnsUpdateMessage(string subdomain, string base64Key, string base64IV, string domain)
        {
            var message = new List<byte>();

            // Transaction ID (random)
            var random = new Random();
            ushort transactionId = (ushort)random.Next(0, 65535);
            message.AddRange(ToBytes(transactionId));

            // Flags: Opcode = UPDATE (5), standard query
            ushort flags = (ushort)(DnsOpcodeUpdate << 11);
            message.AddRange(ToBytes(flags));

            // ZOCOUNT (Zone count) = 1
            message.AddRange(ToBytes((ushort)1));

            // PRCOUNT (Prerequisite count) = 0
            message.AddRange(ToBytes((ushort)0));

            // UPCOUNT (Update count) = 2 (key and IV records)
            message.AddRange(ToBytes((ushort)2));

            // ADCOUNT (Additional count) = 0
            message.AddRange(ToBytes((ushort)0));

            // Zone section
            message.AddRange(EncodeDomainName(domain));
            message.AddRange(ToBytes((ushort)6)); // SOA type
            message.AddRange(ToBytes(DnsClassINET));

            // Update section - Key TXT record
            string keyName = $"key.{subdomain}.{domain}";
            message.AddRange(EncodeDomainName(keyName));
            message.AddRange(ToBytes(DnsTypeTXT));
            message.AddRange(ToBytes(DnsClassINET));
            message.AddRange(ToBytes(DefaultTTL));
            var keyData = EncodeTxtData(base64Key);
            message.AddRange(ToBytes((ushort)keyData.Length));
            message.AddRange(keyData);

            // Update section - IV TXT record
            string ivName = $"iv.{subdomain}.{domain}";
            message.AddRange(EncodeDomainName(ivName));
            message.AddRange(ToBytes(DnsTypeTXT));
            message.AddRange(ToBytes(DnsClassINET));
            message.AddRange(ToBytes(DefaultTTL));
            var ivData = EncodeTxtData(base64IV);
            message.AddRange(ToBytes((ushort)ivData.Length));
            message.AddRange(ivData);

            return message.ToArray();
        }

        /// <summary>
        /// Sends a DNS message to the specified server and returns the response.
        /// </summary>
        private static byte[] SendDnsMessage(byte[] message, string dnsServer)
        {
            string host = dnsServer;
            int port = 53;

            if (dnsServer.Contains(":"))
            {
                var parts = dnsServer.Split(':');
                host = parts[0];
                port = int.Parse(parts[1]);
            }

            using (var udpClient = new UdpClient())
            {
                udpClient.Connect(host, port);
                udpClient.Send(message, message.Length);

                udpClient.Client.ReceiveTimeout = 5000;

                IPEndPoint remoteEndPoint = null;
                var response = udpClient.Receive(ref remoteEndPoint);

                return response;
            }
        }

        /// <summary>
        /// Encodes a domain name in DNS wire format.
        /// </summary>
        private static byte[] EncodeDomainName(string domain)
        {
            var result = new List<byte>();
            var labels = domain.TrimEnd('.').Split('.');

            foreach (var label in labels)
            {
                result.Add((byte)label.Length);
                result.AddRange(System.Text.Encoding.ASCII.GetBytes(label));
            }

            result.Add(0); // Root label

            return result.ToArray();
        }

        /// <summary>
        /// Encodes TXT record data.
        /// </summary>
        private static byte[] EncodeTxtData(string text)
        {
            var result = new List<byte>();
            var bytes = System.Text.Encoding.ASCII.GetBytes(text);

            // TXT records have a length byte prefix for each string (max 255)
            int offset = 0;
            while (offset < bytes.Length)
            {
                int length = Math.Min(255, bytes.Length - offset);
                result.Add((byte)length);
                for (int i = 0; i < length; i++)
                {
                    result.Add(bytes[offset + i]);
                }
                offset += length;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Converts a ushort to big-endian bytes.
        /// </summary>
        private static byte[] ToBytes(ushort value)
        {
            return new byte[] { (byte)(value >> 8), (byte)(value & 0xFF) };
        }

        /// <summary>
        /// Converts a uint to big-endian bytes.
        /// </summary>
        private static byte[] ToBytes(uint value)
        {
            return new byte[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value & 0xFF)
            };
        }

        /// <summary>
        /// Converts DNS response code to string.
        /// </summary>
        private static string RcodeToString(ushort rcode)
        {
            return rcode switch
            {
                0 => "NOERROR",
                1 => "FORMERR",
                2 => "SERVFAIL",
                3 => "NXDOMAIN",
                4 => "NOTIMP",
                5 => "REFUSED",
                6 => "YXDOMAIN",
                7 => "YXRRSET",
                8 => "NXRRSET",
                9 => "NOTAUTH",
                10 => "NOTZONE",
                _ => $"UNKNOWN({rcode})"
            };
        }
    }
}
