/*

A Windows Service to detect if it has been shutdown and hibernate if multiple instances are. 

Released as open source by NCC Group Plc - http://research.nccgroup.com/

Developed by Ollie Whitehouse, ollie dot whitehouse at nccgroup dot com

https://github.com/nccgroup/KilledProcessCanary

Released under AGPL see LICENSE for more information

thanks to Harry for the idea

 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Net;

namespace SWOLLENRIVER
{

    /// <summary>
    /// Class used for conversion between byte array and Base32 notation
    /// source: https://olegignat.com/base32/
    /// </summary>
    internal sealed class Base32
    {
        /// <summary>
        /// Size of the regular byte in bits
        /// </summary>
        private const int InByteSize = 8;

        /// <summary>
        /// Size of converted byte in bits
        /// </summary>
        private const int OutByteSize = 5;

        /// <summary>
        /// Alphabet
        /// </summary>
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>
        /// Convert byte array to Base32 format
        /// </summary>
        /// <param name="bytes">An array of bytes to convert to Base32 format</param>
        /// <returns>Returns a string representing byte array</returns>
        internal static string ToBase32String(byte[] bytes)
        {
            // Check if byte array is null
            if (bytes == null)
            {
                return null;
            }
            // Check if empty
            else if (bytes.Length == 0)
            {
                return string.Empty;
            }

            // Prepare container for the final value
            StringBuilder builder = new StringBuilder(bytes.Length * InByteSize / OutByteSize);

            // Position in the input buffer
            int bytesPosition = 0;

            // Offset inside a single byte that <bytesPosition> points to (from left to right)
            // 0 - highest bit, 7 - lowest bit
            int bytesSubPosition = 0;

            // Byte to look up in the dictionary
            byte outputBase32Byte = 0;

            // The number of bits filled in the current output byte
            int outputBase32BytePosition = 0;

            // Iterate through input buffer until we reach past the end of it
            while (bytesPosition < bytes.Length)
            {
                // Calculate the number of bits we can extract out of current input byte to fill missing bits in the output byte
                int bitsAvailableInByte = Math.Min(InByteSize - bytesSubPosition, OutByteSize - outputBase32BytePosition);

                // Make space in the output byte
                outputBase32Byte <<= bitsAvailableInByte;

                // Extract the part of the input byte and move it to the output byte
                outputBase32Byte |= (byte)(bytes[bytesPosition] >> (InByteSize - (bytesSubPosition + bitsAvailableInByte)));

                // Update current sub-byte position
                bytesSubPosition += bitsAvailableInByte;

                // Check overflow
                if (bytesSubPosition >= InByteSize)
                {
                    // Move to the next byte
                    bytesPosition++;
                    bytesSubPosition = 0;
                }

                // Update current base32 byte completion
                outputBase32BytePosition += bitsAvailableInByte;

                // Check overflow or end of input array
                if (outputBase32BytePosition >= OutByteSize)
                {
                    // Drop the overflow bits
                    outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                    // Add current Base32 byte and convert it to character
                    builder.Append(Base32Alphabet[outputBase32Byte]);

                    // Move to the next byte
                    outputBase32BytePosition = 0;
                }
            }

            // Check if we have a remainder
            if (outputBase32BytePosition > 0)
            {
                // Move to the right bits
                outputBase32Byte <<= (OutByteSize - outputBase32BytePosition);

                // Drop the overflow bits
                outputBase32Byte &= 0x1F;  // 0x1F = 00011111 in binary

                // Add current Base32 byte and convert it to character
                builder.Append(Base32Alphabet[outputBase32Byte]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Convert base32 string to array of bytes
        /// </summary>
        /// <param name="base32String">Base32 string to convert</param>
        /// <returns>Returns a byte array converted from the string</returns>
        internal static byte[] FromBase32String(string base32String)
        {
            // Check if string is null
            if (base32String == null)
            {
                return null;
            }
            // Check if empty
            else if (base32String == string.Empty)
            {
                return new byte[0];
            }

            // Convert to upper-case
            string base32StringUpperCase = base32String.ToUpperInvariant();

            // Prepare output byte array
            byte[] outputBytes = new byte[base32StringUpperCase.Length * OutByteSize / InByteSize];

            // Check the size
            if (outputBytes.Length == 0)
            {
                throw new ArgumentException("Specified string is not valid Base32 format because it doesn''t have enough data to construct a complete byte array");
            }

            // Position in the string
            int base32Position = 0;

            // Offset inside the character in the string
            int base32SubPosition = 0;

            // Position within outputBytes array
            int outputBytePosition = 0;

            // The number of bits filled in the current output byte
            int outputByteSubPosition = 0;

            // Normally we would iterate on the input array but in this case we actually iterate on the output array
            // We do it because output array doesn''t have overflow bits, while input does and it will cause output array overflow if we don''t stop in time
            while (outputBytePosition < outputBytes.Length)
            {
                // Look up current character in the dictionary to convert it to byte
                int currentBase32Byte = Base32Alphabet.IndexOf(base32StringUpperCase[base32Position]);

                // Check if found
                if (currentBase32Byte < 0)
                {
                    throw new ArgumentException(string.Format("Specified string is not valid Base32 format because character \"{0}\" does not exist in Base32 alphabet", base32String[base32Position]));
                }

                // Calculate the number of bits we can extract out of current input character to fill missing bits in the output byte
                int bitsAvailableInByte = Math.Min(OutByteSize - base32SubPosition, InByteSize - outputByteSubPosition);

                // Make space in the output byte
                outputBytes[outputBytePosition] <<= bitsAvailableInByte;

                // Extract the part of the input character and move it to the output byte
                outputBytes[outputBytePosition] |= (byte)(currentBase32Byte >> (OutByteSize - (base32SubPosition + bitsAvailableInByte)));

                // Update current sub-byte position
                outputByteSubPosition += bitsAvailableInByte;

                // Check overflow
                if (outputByteSubPosition >= InByteSize)
                {
                    // Move to the next byte
                    outputBytePosition++;
                    outputByteSubPosition = 0;
                }

                // Update current base32 byte completion
                base32SubPosition += bitsAvailableInByte;

                // Check overflow or end of input array
                if (base32SubPosition >= OutByteSize)
                {
                    // Move to the next character
                    base32Position++;
                    base32SubPosition = 0;
                }
            }

            return outputBytes;
        }
    }

    class FireCanary
    {
        public static void Fire()
        {
            // https://twitter.com/marcoslaviero/status/1266320937416298498?s=21
            // https://docs.canarytokens.org/guide/dns-token.html#encoding-information-in-your-token

            // REPLACE ME with the base hostname generated for a Canary token here - https://canarytokens.org/generate#
            string canaryHostname = "REPLACEME.canarytokens.com";

            string machineName= Base32.ToBase32String(System.Text.Encoding.UTF8.GetBytes(Environment.MachineName + " hibernating"));
            string thisMachineName = machineName;
            thisMachineName = thisMachineName.Replace("=", "");

            Random ran = new Random();
            int getRanNum = ran.Next(10,99);
            
            string Final = thisMachineName + "." + "G" + getRanNum.ToString() + "." + canaryHostname;
            Dns.GetHostEntry(Final);
            Console.WriteLine(Final);
        }

    }

    class Power
    {
        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]

        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
    }

    static class Program
    {
        static Mutex mut = new Mutex(false, "SWOLLENRIVER");

        

        public static void IncCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            Cur++;
            mmva.Write(0, Cur);
            mut.ReleaseMutex();
        }

        public static void DecCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            Cur--;
            mmva.Write(0, Cur);
            mut.ReleaseMutex();
        }

        public static void CheckCount()
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("mmf1", 2, MemoryMappedFileAccess.ReadWrite);
            MemoryMappedViewAccessor mmva = mmf.CreateViewAccessor(0, sizeof(int));

            mut.WaitOne();
            int Cur = mmva.ReadInt32(0);
            mut.ReleaseMutex();

            // Number of instances threshold
            if( Cur < 2)
            {
                // Fire our canary
                FireCanary.Fire();

                // Hibernate the host
                Power.SetSuspendState(true, true, true);
            }

        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //
            // run from the command line
            // for testing
            //
            if (System.Environment.UserInteractive)
            {

                //
                // TIP
                // Run two instances
                //

                // Log our run
                Console.WriteLine("[SWOLLENRIVER] Interactive");
                IncCount();
                
                // Snooze
                Console.WriteLine("[SWOLLENRIVER] Sleeping for 10 seconds");
                Thread.Sleep(10000);

                // Now check and fire if neede
                Console.WriteLine("[SWOLLENRIVER] Checking how many of me are running");
                CheckCount();    
                DecCount();
            }
            //
            // run as Windows service
            // 
            else
            {
                Console.WriteLine("[SWOLLENRIVER] Service");
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new SWOLLENRIVERSVC()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
