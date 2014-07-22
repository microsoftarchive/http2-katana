// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved       
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0

// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.

// See the Apache 2 License for the specific language governing permissions and limitations under the License.

using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    using Map = Dictionary<bool[], byte>;

    internal partial class HuffmanCodesTable
    {
        // see spec 08 - > 4.1.2.  String Literal Representation
        // String literals which use Huffman encoding are encoded with the
        // Huffman codes defined in Appendix C

        #region Huffman Codes Table

        private Map _symbolBitsMap = new Map
            {
                {new[] {F, T, F, T, F, F}, (byte) ' '},                                // ' ' ( 32) |010100
                {new[] {T, T, T, T, T, T, T, F, F, F}, (byte) '!'},                    // '!' ( 33) |11111110|00
                {new[] {T, T, T, T, T, T, T, F, F, T}, (byte) '"'},                    // '"' ( 34) |11111110|01
                {new[] {T, T, T, T, T, T, T, T, T, F, T, F}, (byte) '#'},              // '#' ( 35) |11111111|1010
                {new[] {T, T, T, T, T, T, T, T, T, T, F, F, T}, (byte) '$'},           // '$' ( 36) |11111111|11001
                {new[] {F, T, F, T, F, T}, (byte) '%'},                                // '%' ( 37) |010101
                {new[] {T, T, T, T, T, F, F, F}, (byte) '&'},                          // '&' ( 38) |11111000
                {new[] {T, T, T, T, T, T, T, T, F, T, F}, (byte) '\''},                // ''' ( 39) |11111111|010
                {new[] {T, T, T, T, T, T, T, F, T, F}, (byte) '('},                    // '(' ( 40) |11111110|10
                {new[] {T, T, T, T, T, T, T, F, T, T}, (byte) ')'},                    // ')' ( 41) |11111110|11
                {new[] {T, T, T, T, T, F, F, T}, (byte) '*'},                          // '*' ( 42) |11111001
                {new[] {T, T, T, T, T, T, T, T, F, T, T}, (byte) '+'},                 // '+' ( 43) |11111111|011
                {new[] {T, T, T, T, T, F, T, F}, (byte) ','},                          // ',' ( 44) |11111010
                {new[] {F, T, F, T, T, F}, (byte) '-'},                                // '-' ( 45) |010110
                {new[] {F, T, F, T, T, T}, (byte) '.'},                                // '.' ( 46) |010111
                {new[] {F, T, T, F, F, F}, (byte) '/'},                                // '/' ( 47) |011000
                {new[] {F, F, F, F, F}, (byte) '0'},                                   // '0' ( 48) |00000
                {new[] {F, F, F, F, T}, (byte) '1'},                                   // '1' ( 49) |00001
                {new[] {F, F, F, T, F}, (byte) '2'},                                   // '2' ( 50) |00010
                {new[] {F, T, T, F, F, T}, (byte) '3'},                                // '3' ( 51) |011001
                {new[] {F, T, T, F, T, F}, (byte) '4'},                                // '4' ( 52) |011010
                {new[] {F, T, T, F, T, T}, (byte) '5'},                                // '5' ( 53) |011011
                {new[] {F, T, T, T, F, F}, (byte) '6'},                                // '6' ( 54) |011100
                {new[] {F, T, T, T, F, T}, (byte) '7'},                                // '7' ( 55) |011101
                {new[] {F, T, T, T, T, F}, (byte) '8'},                                // '8' ( 56) |011110
                {new[] {F, T, T, T, T, T}, (byte) '9'},                                // '9' ( 57) |011111
                {new[] {T, F, T, T, T, F, F}, (byte) ':'},                             // ':' ( 58) |1011100
                {new[] {T, T, T, T, T, F, T, T}, (byte) ';'},                          // ';' ( 59) |11111011
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, T, F, F}, (byte) '<'},     // '<' ( 60) |11111111|1111100
                {new[] {T, F, F, F, F, F}, (byte) '='},                                // '=' ( 61) |100000
                {new[] {T, T, T, T, T, T, T, T, T, F, T, T}, (byte) '>'},              // '>' ( 62) |11111111|1011
                {new[] {T, T, T, T, T, T, T, T, F, F}, (byte) '?'},                    // '?' ( 63) |11111111|00
                {new[] {T, T, T, T, T, T, T, T, T, T, F, T, F}, (byte) '@'},           // '@' ( 64) |11111111|11010
                {new[] {T, F, F, F, F, T}, (byte) 'A'},                                // 'A' ( 65) |100001
                {new[] {T, F, T, T, T, F, T}, (byte) 'B'},                             // 'B' ( 66) |1011101
                {new[] {T, F, T, T, T, T, F}, (byte) 'C'},                             // 'C' ( 67) |1011110
                {new[] {T, F, T, T, T, T, T}, (byte) 'D'},                             // 'D' ( 68) |1011111
                {new[] {T, T, F, F, F, F, F}, (byte) 'E'},                             // 'E' ( 69) |1100000
                {new[] {T, T, F, F, F, F, T}, (byte) 'F'},                             // 'F' ( 70) |1100001
                {new[] {T, T, F, F, F, T, F}, (byte) 'G'},                             // 'G' ( 71) |1100010
                {new[] {T, T, F, F, F, T, T}, (byte) 'H'},                             // 'H' ( 72) |1100011
                {new[] {T, T, F, F, T, F, F}, (byte) 'I'},                             // 'I' ( 73) |1100100
                {new[] {T, T, F, F, T, F, T}, (byte) 'J'},                             // 'J' ( 74) |1100101
                {new[] {T, T, F, F, T, T, F}, (byte) 'K'},                             // 'K' ( 75) |1100110
                {new[] {T, T, F, F, T, T, T}, (byte) 'L'},                             // 'L' ( 76) |1100111
                {new[] {T, T, F, T, F, F, F}, (byte) 'M'},                             // 'M' ( 77) |1101000
                {new[] {T, T, F, T, F, F, T}, (byte) 'N'},                             // 'N' ( 78) |1101001
                {new[] {T, T, F, T, F, T, F}, (byte) 'O'},                             // 'O' ( 79) |1101010
                {new[] {T, T, F, T, F, T, T}, (byte) 'P'},                             // 'P' ( 80) |1101011
                {new[] {T, T, F, T, T, F, F}, (byte) 'Q'},                             // 'Q' ( 81) |1101100
                {new[] {T, T, F, T, T, F, T}, (byte) 'R'},                             // 'R' ( 82) |1101101
                {new[] {T, T, F, T, T, T, F}, (byte) 'S'},                             // 'S' ( 83) |1101110
                {new[] {T, T, F, T, T, T, T}, (byte) 'T'},                             // 'T' ( 84) |1101111
                {new[] {T, T, T, F, F, F, F}, (byte) 'U'},                             // 'U' ( 85) |1110000
                {new[] {T, T, T, F, F, F, T}, (byte) 'V'},                             // 'V' ( 86) |1110001
                {new[] {T, T, T, F, F, T, F}, (byte) 'W'},                             // 'W' ( 87) |1110010
                {new[] {T, T, T, T, T, T, F, F}, (byte) 'X'},                          // 'X' ( 88) |11111100
                {new[] {T, T, T, F, F, T, T}, (byte) 'Y'},                             // 'Y' ( 89) |1110011
                {new[] {T, T, T, T, T, T, F, T}, (byte) 'Z'},                          // 'Z' ( 90) |11111101
                {new[] {T, T, T, T, T, T, T, T, T, T, F, T, T}, (byte) '['},           // '[' ( 91) |11111111|11011
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, F, F, F, F}, (byte) '\\'},  
                                                                                       // '\' ( 92) |11111111|11111110|000
                {new[] {T, T, T, T, T, T, T, T, T, T, T, F, F}, (byte) ']'},           // ']' ( 93) |11111111|11100
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, F, F}, (byte) '^'},        // '^' ( 94) |11111111|111100
                {new[] {T, F, F, F, T, F}, (byte) '_'},                                // '_' ( 95) |100010
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, T, F, T}, (byte) '`'},     // '`' ( 96) |11111111|1111101
                {new[] {F, F, F, T, T}, (byte) 'a'},                                   // 'a' ( 97) |00011
                {new[] {T, F, F, F, T, T}, (byte) 'b'},                                // 'b' ( 98) |100011
                {new[] {F, F, T, F, F}, (byte) 'c'},                                   // 'c' ( 99) |00100
                {new[] {T, F, F, T, F, F}, (byte) 'd'},                                // 'd' (100) |100100
                {new[] {F, F, T, F, T}, (byte) 'e'},                                   // 'e' (101) |00101
                {new[] {T, F, F, T, F, T}, (byte) 'f'},                                // 'f' (102) |100101
                {new[] {T, F, F, T, T, F}, (byte) 'g'},                                // 'g' (103) |100110
                {new[] {T, F, F, T, T, T}, (byte) 'h'},                                // 'h' (104) |100111
                {new[] {F, F, T, T, F}, (byte) 'i'},                                   // 'i' (105) |00110
                {new[] {T, T, T, F, T, F, F}, (byte) 'j'},                             // 'j' (106) |1110100
                {new[] {T, T, T, F, T, F, T}, (byte) 'k'},                             // 'k' (107) |1110101
                {new[] {T, F, T, F, F, F}, (byte) 'l'},                                // 'l' (108) |101000
                {new[] {T, F, T, F, F, T}, (byte) 'm'},                                // 'm' (109) |101001
                {new[] {T, F, T, F, T, F}, (byte) 'n'},                                // 'n' (110) |101010
                {new[] {F, F, T, T, T}, (byte) 'o'},                                   // 'o' (111) |00111
                {new[] {T, F, T, F, T, T}, (byte) 'p'},                                // 'p' (112) |101011
                {new[] {T, T, T, F, T, T, F}, (byte) 'q'},                             // 'q' (113) |1110110
                {new[] {T, F, T, T, F, F}, (byte) 'r'},                                // 'r' (114) |101100
                {new[] {F, T, F, F, F}, (byte) 's'},                                   // 's' (115) |01000
                {new[] {F, T, F, F, T}, (byte) 't'},                                   // 't' (116) |01001
                {new[] {T, F, T, T, F, T}, (byte) 'u'},                                // 'u' (117) |101101
                {new[] {T, T, T, F, T, T, T}, (byte) 'v'},                             // 'v' (118) |1110111
                {new[] {T, T, T, T, F, F, F}, (byte) 'w'},                             // 'w' (119) |1111000
                {new[] {T, T, T, T, F, F, T}, (byte) 'x'},                             // 'x' (120) |1111001
                {new[] {T, T, T, T, F, T, F}, (byte) 'y'},                             // 'y' (121) |1111010
                {new[] {T, T, T, T, F, T, T}, (byte) 'z'},                             // 'z' (122) |1111011
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, T, T, F}, (byte) '{'},     // '{' (123) |11111111|1111110
                {new[] {T, T, T, T, T, T, T, T, T, F, F}, (byte) '|'},                 // '|' (124) |11111111|100
                {new[] {T, T, T, T, T, T, T, T, T, T, T, T, F, T}, (byte) '}'},        // '}' (125) |11111111|111101
                {new[] {T, T, T, T, T, T, T, T, T, T, T, F, T}, (byte) '~'},           // '~' (126) |11111111|11101
            };

        #endregion

        // see spec 08 - > Appendix C.  Huffman Codes
        public static readonly bool[] Eos = new[] { T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T, T };
        // EOS (256)  |11111111|11111111|11111111|111111  

        public static readonly bool[] ZeroOctet = new [] { T, T, T, T, T, T, T, T, T, T, F, F, F };
    }
}
