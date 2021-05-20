using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace DES_Algorithm
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public string ConvertToBinary(string key)
        {
            byte[] byteText = Encoding.UTF8.GetBytes(key);
            Array.Reverse(byteText);

            BitArray bitArray = new BitArray(byteText);

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = bitArray.Length - 1; i >= 0; i--)
            {
                stringBuilder.Append(Convert.ToInt32(bitArray[i]));
            }

            return stringBuilder.ToString();
        }

        public string DES(string input, string key, bool endcodeOrDecode)
        {
            StringBuilder output = new StringBuilder();

            string[] subkeys = prepareDesKey(key); // KEY

            if (!endcodeOrDecode) Array.Reverse(subkeys); // DECRYPT

            //string[] RL = new string[16];
            string[] Rarr = new string[16];
            string[] Larr = new string[16];

            int numberOfblocks = input.Length % 64 == 0 ? (input.Length / 64) : (input.Length / 64) + 1; // jezeli uda sie podzielic na bloki po 64
            for (int k = 0; k < numberOfblocks; k++)
            {
                if(k * 64 + 64 > input.Length)
                {
                    input = AddBits(input, k * 64 + 64 - input.Length);
                }

                string block64 = input.Substring(k * 64, 64); // take first 64 bits from input
                block64 = initialPermatuation(block64); // initial permutation
                string L = block64.Substring(0, 32); // first 32 bits of 64 bits block
                string R = block64.Substring(32, 32); // second 32 bits of 64 bits block
                string initialRvalue = R;

                for (int i = 0; i < subkeys.Length; i++)
                {
                    if (i != 0)
                    {
                        R = Rarr[i - 1];
                        L = Larr[i - 1];
                        initialRvalue = R;
                    }
                    // nowa zmienna czy tymczasowa na trzymanie permutacji rozszerzajacej?
                    R = permutedExpansion(R); // expansion permutation from 32 to 48 bits
                    R = Multiple_XOR(R, subkeys[i]); // XOR with subkey[i]

                    StringBuilder dataFrom8x6Table = new StringBuilder();
                    for (int j = 0; j < 8; j++) // break Right key into 8 x 6 bits
                    {
                        string subBlock = R.Substring(j * 6, 6); // make 8 x 6bits strings
                        dataFrom8x6Table.Append(readTableData(subBlock, j)); // receive 32bits from 8 tables
                    }
                    string table8x6data = permutedSblock(dataFrom8x6Table.ToString());
                    // S block permutation of 32bits data

                    // left side XOR with table data 8x6

                    // right side equals XOR of left side and table data

                    // left side equals right side from the start

                    Rarr[i] = Multiple_XOR(L, table8x6data);
                    Larr[i] = initialRvalue;
                }

                string final = Rarr[15] + Larr[15];
                string pleaseBeCorrect = permutedReverse(final);
                output.Append(pleaseBeCorrect);
            }

            return output.ToString();
        }

        public string AddBits(string block, int count)
        {
            int bytes = count / 8;

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(block);
            for(int i = 0; i < bytes; i++)
            {
                string str = Convert.ToString(0, 2);
                str = str.PadLeft(8, '0');
                stringBuilder.Append(str);
            }

            return stringBuilder.ToString();
        }

        public string readTableData(string bitValue, int s)
        {
            int row = Convert.ToInt32(bitValue[0].ToString() + bitValue[bitValue.Length - 1], 2); // 0 and 5th bit -> int row
            int column = Convert.ToInt32(bitValue.Substring(1, 4), 2); // 1 2 3 4 bits -> int column

            int[,] dataTables =
                {
            {
            14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7,
            0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8,
            4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0,
            15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13,
            },
            {
            15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10,
            3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5,
            0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15,
            13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9,
            },
            {
            10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8,
            13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1,
            13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7,
            1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12,
            },
            {
            7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15,
            13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9,
            10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4,
            3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14,
            },
            {
            2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9,
            14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6,
            4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14,
            11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3,
            },
            {
            12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11,
            10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8,
            9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6,
            4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13,
            },
            {
            4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1,
            13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6,
            1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2,
            6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12,
            },
            {
            13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7,
            1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2,
            7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8,
            2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11}
            };
            // go back to binary
            string result = Convert.ToString(dataTables[s, 16 * row + column], 2);

            return result.Length == 4 ? result : completeSequence(result, 4);
        }

        private string completeSequence(string result, int v)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < v - result.Length; i++)
            {
                sb.Append(0);
            }
            sb.Append(result);
            return sb.ToString();
        }

        public string[] prepareDesKey(string key)
        {

            if(key.Length < 64)
            {
                key = AddBits(key, 64 - key.Length);
            }

            key = permutedChoice_1(key); // initial permutation
            int[] ls = new int[] { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };
            string[] subkeys = new string[ls.Length];
            for (int i = 0; i < ls.Length; i++)
            {
                // break key into 2 parts
                string keyC = key.Substring(0, 28);
                string keyD = key.Substring(28, 28);
                // do shifts & merge key
                key = leftShift(keyC, ls[i]) + leftShift(keyD, ls[i]);
                // do pc2
                subkeys[i] = permutedChoice_2(key);
            }
            return subkeys;
        }

        private string leftShift(string input, int shift)
        {
            string cut = input.Substring(0, shift);
            input = input.Remove(0, shift);
            input += cut;
            return input;
        }

        private string permutedChoice_1(string key) // key permutation, length reduction
        {
            int[] ip = new int[] { 57, 49, 41, 33, 25, 17, 9, 1, 58, 50, 42, 34, 26,
                                   18, 10, 2, 59, 51, 43, 35, 27, 19, 11, 3, 60, 52,
                                   44, 36, 63, 55, 47, 39, 31, 23, 15, 7, 62, 54, 46,
                                   38, 30, 22, 14, 6, 61, 53, 45, 37, 29, 21, 13, 5,
                                   28, 20, 12, 4 };

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++) // reduce key from 64 to 56 bits
            {
                sb.Append(key[ip[i] - 1]);
            }

            return sb.ToString();
        }

        private string permutedChoice_2(string key) // key permutation, length reduction
        {
            int[] ip = new int[] { 14,17,11,24,1,5,3,28,15,6,21,10,
                                   23,19,12,4,26,8,16,7,27,20,13,2,
                                   41,52,31,37,47,55,30,40,51,45,33,48,
                                   44,49,39,56,34,53,46,42,50,36,29,32};

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++) // reduce key from 64 to 56 bits
            {
                sb.Append(key[ip[i] - 1]);
            }
            return sb.ToString();
        }

        public string initialPermatuation(string key)
        {
            int[] ip = new int[] {58,50,42,34,26,18,10,2,60,52,44,36,28,20,12,4,62,54,
                                  46,38,30,22,14,6,64,56,48,40,32,24,16,8,57,49,41,33,
                                  25,17,9,1,59,51,43,35,27,19,11,3,61,53,45,37,29,21,
                                  13,5,63,55,47,39,31,23,15,7};

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++)
            {
                sb.Append(key[ip[i] - 1]);
            }
            return sb.ToString();
        }

        public string permutedExpansion(string key) // key permutation, length reduction
        {
            int[] ip = new int[] { 32,1,2,3,4,5,4,5,6,7,8,9,
                                   8,9,10,11,12,13,12,13,14,15,16,17,
                                   16,17,18,19,20,21,20,21,22,23,24,25,
                                   24,25,26,27,28,29,28,29,30,31,32,1};

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++) // reduce key from 64 to 56 bits
            {
                sb.Append(key[ip[i] - 1]);
            }
            return sb.ToString();
        }

        public string permutedSblock(string key) // key permutation, length reduction
        {
            int[] ip = new int[] { 16, 7, 20, 21, 29, 12, 28, 17, 1, 15, 23,
                                   26, 5, 18, 31, 10, 2, 8, 24, 14, 32, 27, 3,
                                   9, 19, 13, 30, 6, 22, 11, 4, 25 };

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++)
            {
                sb.Append(key[ip[i] - 1]);
            }
            return sb.ToString();
        }

        private string permutedReverse(string key) // key permutation, length reduction
        {
            int[] ip = new int[] {40,8,48,16,56,24,64,32,39,7,47,15,55,23,63,31,
                                  38,6,46,14,54,22,62,30,37,5,45,13,53,21,61,29,
                                  36,4,44,12,52,20,60,28,35,3,43,11,51,19,59,27,
                                  34,2,42,10,50,18,58,26,33,1,41,9,49,17,57,25};

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ip.Length; i++)
            {
                sb.Append(key[ip[i] - 1]);
            }
            return sb.ToString();
        }

        public string Multiple_XOR(string s1, string s2)
        {
            if (s1.Length != s2.Length)
                return "";

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < s1.Length; i++)
            {
                sb.Append(s1[i] ^ s2[i]);
                //sb.Append(Single_XOR(s1[i], s2[i]));
            }
            return sb.ToString();
        }

        //public char Single_XOR(char byte_1, char byte_2)
        //{
        //    return byte_1 != byte_2 ? '1' : '0';
        //}

        public static string BinaryStringToHexString(string binary)
        {
            StringBuilder result = new StringBuilder(binary.Length / 8 + 1);

            int mod4Len = binary.Length % 8;
            if (mod4Len != 0)
            {
                binary = binary.PadLeft(((binary.Length / 8) + 1) * 8, '0');
            }

            for (int i = 0; i < binary.Length; i += 8)
            {
                string eightBits = binary.Substring(i, 8);
                result.AppendFormat("{0:X2}", Convert.ToByte(eightBits, 2));
            }

            int length = result.Length;

            for (int i = 2; i < length; i += 2)
            {
                result.Insert(i, " ");
                length++;
                i++;
            }

            return result.ToString();
        }

        private string HexToBinary(string hexvalue)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach(char c in hexvalue.ToCharArray())
            {
                string str = Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2);
                str = str.PadLeft(4, '0');
                stringBuilder.Append(str);
            }
            return stringBuilder.ToString();
        }

        public static string BinaryToString(string data)
        {
            List<byte> byteList = new List<byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }

        private void Encode_Button_Click(object sender, RoutedEventArgs e)
        {
            string input = ConvertToBinary(InputTextBox.Text);
            string key = ConvertToBinary(KeyTextBox.Text);

            OutputTextBox.Text = BinaryStringToHexString(DES(input, key, true));
        }

        private void Decode_Button_Click(object sender, RoutedEventArgs e)
        {
            string input = HexToBinary(string.Concat(InputTextBox.Text.Where(c => !char.IsWhiteSpace(c))));
            string key = ConvertToBinary(KeyTextBox.Text);

            OutputTextBox.Text = BinaryToString(DES(input, key, false));
        }
    }
}

