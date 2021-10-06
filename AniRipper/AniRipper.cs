using System;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace LEGOAniRipper
{
    class AniRipper
    {
        static bool SeekUntil(FileStream stream, string str, long limit = -1)
        {
            byte[] b = new byte[str.Length];

            int read_byte;

            // Read until 
            while ((read_byte = stream.ReadByte()) != -1 && (limit == -1 || stream.Position < limit))
            {
                // Push bytes along
                for (int i = 1; i < b.Length; i++)
                {
                    b[i - 1] = b[i];
                }

                // Set last byte to the newest byte
                b[b.Length - 1] = (byte)read_byte;

                // See if byte array matches string
                if (System.Text.Encoding.ASCII.GetString(b) == str)
                {
                    return true;
                }
            }

            return false;
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No SI file was provided");
                return;
            }
                var interleafFile = args[0];

                string path = Directory.GetCurrentDirectory();

            if (File.Exists(interleafFile))
            {
                using (FileStream si = new FileStream(interleafFile, FileMode.Open, FileAccess.Read))
                {
                    byte[] b = new byte[4];

                    if (si.ReadByte() == 0x52) // Hacky check for if input file is valid (First byte of "RIFF" header identifier)
                    {
                        Console.WriteLine("Current Interleaf file: {0}", interleafFile);
                        {
                            var latestPosition = si.Position;
                            while (SeekUntil(si, "LegoAnimPresenter")) // Find LegoAnimPresenter instances in the LIST
                            {
                                latestPosition = si.Position;
                                Console.Write("\nFound LegoAnimPresenter at {0}, ", (si.Position - 17).ToString("X")); // Print the offset of the found entries minus the length of the LegoAnimPresenter string

                                si.Position += 5; // From the end of the LegoAnimPresenter string, move forward 5 bytes to get to the beginning of the second string

                                SeekUntil(si, "\x00"); // At the start of the second string, seek until finding the null terminator
                                var objectID = si.ReadByte(); // Get the Object ID
                                Console.Write("Object ID: {0}, ", objectID);

                                string aniFN = "";
                                SeekUntil(si, ".ani"); // Find the .ani extension so we can get to the end of the filename

                                // Find the filename for the ani by backtracking
                                long current_pos = si.Position;
                                do
                                {
                                    si.Position -= 2;
                                }
                                while ((char)si.ReadByte() != '\\');

                                int aniFN_char;
                                while ((aniFN_char = si.ReadByte()) != 0)
                                {
                                    aniFN += (char)aniFN_char;
                                }
                                si.Position = current_pos;

                                Console.Write(aniFN + '\n');

                                using (FileStream aniOut = new FileStream(path + "/" + aniFN, FileMode.Create, FileAccess.Write))
                                {
                                    // Find chunks and then check their IDs for a match
                                    while (SeekUntil(si, "MxCh"))
                                    {
                                        si.Position += 6;
                                        if (si.ReadByte() == objectID)
                                        {
                                            si.Position -= 7; // Get back to the start of the chunk now
                                            si.Read(b, 0, b.Length);

                                            Int32 mxch_size = BitConverter.ToInt32(b, 0);

                                            if (mxch_size > 14) // Make sure we're skipping filler chunks
                                            {
                                                Console.Write("Extracting {0} now...\n", aniFN);
                                                byte[] chunk_data = new byte[mxch_size];
                                                si.Read(chunk_data, 0, mxch_size);
                                                aniOut.Write(chunk_data, 14, mxch_size - 14);
                                            }  
                                        }
                                        else
                                        {
                                            si.Position -= 7; // If ObjectID does not match, make sure we are not offset by 6 bytes
                                        }
                                    }
                                    si.Position = latestPosition; // After the MxCh loop, set the stream position back to the offset we had after finding an instance of LegoAnimPresenter
                                }
                            }
                            Console.WriteLine("\nFinished extraction!");
                        }
                    }

                    else
                    {
                        Console.WriteLine("The provided Interleaf file is not valid");
                        return;
                    }
                  }
                }
                else
                {
                    Console.WriteLine("Could not find the provided Interleaf file");
                    return;
                }
            }
        }
    }

