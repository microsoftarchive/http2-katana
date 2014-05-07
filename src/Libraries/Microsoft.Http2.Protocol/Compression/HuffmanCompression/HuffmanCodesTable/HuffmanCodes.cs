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
        // see spec 07 - > 4.1.2.  String Literal Representation
        // String literals which use Huffman encoding are encoded with the
        // Huffman codes defined in Appendix C

        #region Huffman Codes Table

        private Map _symbolBitsMap = new Map
            {
                {new[] {F,F,T,T,F}, (byte) ' '},                                                //' ' ( 32) |00110
                {new[] {T,T,T,T,T,T,T,T, T,T,T,F,F}, (byte) '!'},                               //'!' ( 33) |11111111|11100
                {new[] {T,T,T,T,T,F,F,F, F}, (byte) '"'},                                       //'"' ( 34) |11111000|0
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,F,F}, (byte) '#'},                             //'#' ( 35) |11111111|111100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,F,F}, (byte) '$'},                           //'$' ( 36) |11111111|1111100
                {new[] {F,T,T,T,T,F}, (byte) '%'},                                              //'%' ( 37) |011110
                {new[] {T,T,F,F,T,F,F}, (byte) '&'},                                            //'&' ( 38) |1100100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,F,T}, (byte) '\''},                              //''' ( 39) |11111111|11101
                {new[] {T,T,T,T,T,T,T,F, T,F}, (byte) '('},                                     //'(' ( 40) |11111110|10
                {new[] {T,T,T,T,T,F,F,F, T}, (byte) ')'},                                       //')' ( 41) |11111000|1
                {new[] {T,T,T,T,T,T,T,F, T,T}, (byte) '*'},                                     //'*' ( 42) |11111110|11
                {new[] {T,T,T,T,T,T,T,T, F,F}, (byte) '+'},                                     //'+' ( 43) |11111111|00
                {new[] {T,T,F,F,T,F,T}, (byte) ','},                                            //',' ( 44) |1100101
                {new[] {T,T,F,F,T,T,F}, (byte) '-'},                                            //'-' ( 45) |1100110
                {new[] {F,T,T,T,T,T}, (byte) '.'},                                              //'.' ( 46) |011111
                {new[] {F,F,T,T,T}, (byte) '/'},                                                //'/' ( 47) |00111
                {new[] {F,F,F,F}, (byte) '0'},                                                  //'0' ( 48) |0000                    
                {new[] {F,F,F,T}, (byte) '1'},                                                  //'1' ( 49) |0001
                {new[] {F,F,T,F}, (byte) '2'},                                                  //'2' ( 50) |0010
                {new[] {F,T,F,F,F}, (byte) '3'},                                                //'3' ( 51) |01000
                {new[] {T,F,F,F,F,F}, (byte) '4'},                                              //'4' ( 52) |100000
                {new[] {T,F,F,F,F,T}, (byte) '5'},                                              //'5' ( 53) |100001
                {new[] {T,F,F,F,T,F}, (byte) '6'},                                              //'6' ( 54) |100010
                {new[] {T,F,F,F,T,T}, (byte) '7'},                                              //'7' ( 55) |100011
                {new[] {T,F,F,T,F,F}, (byte) '8'},                                              //'8' ( 56) |100100
                {new[] {T,F,F,T,F,T}, (byte) '9'},                                              //'9' ( 57) |100101
                {new[] {T,F,F,T,T,F}, (byte) ':'},                                              //':' ( 58) |100110
                {new[] {T,T,T,F,T,T,F,F}, (byte) ';'},                                          //';' ( 59) |11101100|
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,F, F}, (byte) '<'},                      //'<' ( 60) |11111111|11111110|0
                {new[] {T,F,F,T,T,T}, (byte) '='},                                              //'=' ( 61) |100111
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,F,T}, (byte) '>'},                           //'>' ( 62) |11111111|1111101
                {new[] {T,T,T,T,T,T,T,T ,F,T}, (byte) '?'},                                     //'?' ( 63) |11111111|01
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,F}, (byte) '@'},                           //'@' ( 64) |11111111|1111110
                {new[] {T,T,F,F,T,T,T}, (byte) 'A'},                                            //'A' ( 65) |1100111
                {new[] {T,T,T,F,T,T,F,T}, (byte) 'B'},                                          //'B' ( 66) |11101101|
                {new[] {T,T,T,F,T,T,T,F}, (byte) 'C'},                                          //'C' ( 67) |11101110|
                {new[] {T,T,F,T,F,F,F}, (byte) 'D'},                                            //'D' ( 68) |1101000
                {new[] {T,T,T,F,T,T,T,T}, (byte) 'E'},                                          //'E' ( 69) |11101111|
                {new[] {T,T,F,T,F,F,T}, (byte) 'F'},                                            //'F' ( 70) |1101001
                {new[] {T,T,F,T,F,T,F}, (byte) 'G'},                                            //'G' ( 71) |1101010
                {new[] {T,T,T,T,T,F,F,T, F}, (byte) 'H'},                                       //'H' ( 72) |11111001|0
                {new[] {T,T,T,T,F,F,F,F}, (byte) 'I'},                                          //'I' ( 73) |11110000|
                {new[] {T,T,T,T,T,F,F,T, T}, (byte) 'J'},                                       //'J' ( 74) |11111001|1
                {new[] {T,T,T,T,T,F,T,F, F}, (byte) 'K'},                                       //'K' ( 75) |11111010|0
                {new[] {T,T,T,T,T,F,T,F, T}, (byte) 'L'},                                       //'L' ( 76) |11111010|1
                {new[] {T,T,F,T,F,T,T}, (byte) 'M'},                                            //'M' ( 77) |1101011
                {new[] {T,T,F,T,T,F,F}, (byte) 'N'},                                            //'N' ( 78) |1101100  
                {new[] {T,T,T,T,F,F,F,T}, (byte) 'O'},                                          //'O' ( 79) |11110001|
                {new[] {T,T,T,T,F,F,T,F}, (byte) 'P'},                                          //'P' ( 80) |11110010|
                {new[] {T,T,T,T,T,F,T,T, F}, (byte) 'Q'},                                       //'Q' ( 81) |11111011|0
                {new[] {T,T,T,T,T,F,T,T, T}, (byte) 'R'},                                       //'R' ( 82) |11111011|1
                {new[] {T,T,F,T,T,F,T}, (byte) 'S'},                                            //'S' ( 83) |1101101
                {new[] {T,F,T,F,F,F}, (byte) 'T'},                                              //'T' ( 84) |101000
                {new[] {T,T,T,T,F,F,T,T}, (byte) 'U'},                                          //'U' ( 85) |11110011|
                {new[] {T,T,T,T,T,T,F,F, F}, (byte) 'V'},                                       //'V' ( 86) |11111100|0
                {new[] {T,T,T,T,T,T,F,F, T}, (byte) 'W'},                                       //'W' ( 87) |11111100|1
                {new[] {T,T,T,T,F,T,F,F}, (byte) 'X'},                                          //'X' ( 88) |11110100|
                {new[] {T,T,T,T,T,T,F,T, F}, (byte) 'Y'},                                       //'Y' ( 89) |11111101|0
                {new[] {T,T,T,T,T,T,F,T, T}, (byte) 'Z'},                                       //'Z' ( 90) |11111101|1
                {new[] {T,T,T,T,T,T,T,T, T,F,F}, (byte) '['},                                   //'[' ( 91) |11111111|100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,T,T,F,T,T,F, T,F}, (byte) '\\'},  //'\' ( 92) |11111111|11111111|11110110|10
                {new[] {T,T,T,T,T,T,T,T, T,F,T}, (byte) ']'},                                   //']' ( 93) |11111111|101
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,F,T}, (byte) '^'},                             //'^' ( 94) |11111111|111101
                {new[] {T,T,F,T,T,T,F}, (byte) '_'},                                            //'_' ( 95) |1101110
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,F}, (byte) '`'},                    //'`' ( 96) |11111111|11111111|10
                {new[] {F,T,F,F,T}, (byte) 'a'},                                                //'a' ( 97) |01001
                {new[] {T,T,F,T,T,T,T}, (byte) 'b'},                                            //'b' ( 98) |1101111
                {new[] {F,T,F,T,F}, (byte) 'c'},                                                //'c' ( 99) |01010
                {new[] {T,F,T,F,F,T}, (byte) 'd'},                                              //'d' (100) |101001
                {new[] {F,T,F,T,T}, (byte) 'e'},                                                //'e' (101) |01011
                {new[] {T,T,T,F,F,F,F}, (byte) 'f'},                                            //'f' (102) |1110000
                {new[] {T,F,T,F,T,F}, (byte) 'g'},                                              //'g' (103) |101010
                {new[] {T,F,T,F,T,T}, (byte) 'h'},                                              //'h' (104) |101011
                {new[] {F,T,T,F,F}, (byte) 'i'},                                                //'i' (105) |01100
                {new[] {T,T,T,T,F,T,F,T}, (byte) 'j'},                                          //'j' (106) |11110101|
                {new[] {T,T,T,T,F,T,T,F}, (byte) 'k'},                                          //'k' (107) |11110110|
                {new[] {T,F,T,T,F,F}, (byte) 'l'},                                              //'l' (108) |101100 
                {new[] {T,F,T,T,F,T}, (byte) 'm'},                                              //'m' (109) |101101
                {new[] {T,F,T,T,T,F}, (byte) 'n'},                                              //'n' (110) |101110
                {new[] {F,T,T,F,T}, (byte) 'o'},                                                //'o' (111) |01101
                {new[] {T,F,T,T,T,T}, (byte) 'p'},                                              //'p' (112) |101111
                {new[] {T,T,T,T,T,T,T,F, F}, (byte) 'q'},                                       //'q' (113) |11111110|0
                {new[] {T,T,F,F,F,F}, (byte) 'r'},                                              //'r' (114) |110000
                {new[] {T,T,F,F,F,T}, (byte) 's'},                                              //'s' (115) |110001
                {new[] {F,T,T,T,F}, (byte) 't'},                                                //'t' (116) |01110
                {new[] {T,T,T,F,F,F,T}, (byte) 'u'},                                            //'u' (117) |1110001
                {new[] {T,T,T,F,F,T,F}, (byte) 'v'},                                            //'v' (118) |1110010
                {new[] {T,T,T,F,F,T,T}, (byte) 'w'},                                            //'w' (119) |1110011
                {new[] {T,T,T,F,T,F,F}, (byte) 'x'},                                            //'x' (120) |1110100
                {new[] {T,T,T,F,T,F,T}, (byte) 'y'},                                            //'y' (121) |1110101
                {new[] {T,T,T,T,F,T,T,T}, (byte) 'z'},                                          //'z' (122) |11110111|
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,F, T}, (byte) '{'},                      //'{' (123) |11111111|11111110|1
                {new[] {T,T,T,T,T,T,T,T, T,T,F,F}, (byte) '|'},                                 //'|' (124) |11111111|1100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, F}, (byte) '}'},                      //'}' (125) |11111111|11111111|0
                {new[] {T,T,T,T,T,T,T,T, T,T,F,T}, (byte) '~'},                                 //'~' (126) |11111111|1101
            };
        #endregion

        public static readonly bool[] Eos = new[] { T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,T,T,F,T,T,T, F,F }; //|11111111|11111111|11110111|00
    }
}
