using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    using Map = Dictionary<bool[], byte>;

    internal class HuffmanCodesTable
    {
        private const bool T = true;
        private const bool F = false;

        private Map _symbolBitsMap = new Map
        {
            {new []{F,F,F,F}, (byte) ' '},                                          //' ' ( 32) |0000
            {new []{T,T,T,T,T,T,T,T, T,F,T,F}, (byte) '!'},                         //'!' ( 33) |11111111|1010
            {new []{T,T,F,T,F,T,F}, (byte) '"'},                                    //'"' ( 34) |1101010
            {new []{T,T,T,T,T,T,T,T, T,T,F,T,F}, (byte) '#'},                       //'#' ( 35) |11111111|11010
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,F,F}, (byte) '$'},                     //'$' ( 36) |11111111|111100
            {new []{T,T,T,T,F,T,T,F, F}, (byte) '%'},                               //'%' ( 37) |11110110|0
            {new []{T,T,T,T,T,T,T,F, F,F}, (byte) '&'},                             //'&' ( 38) |11111110|00
            {new []{T,T,T,T,T,T,T,T, T,T,F,T,T}, (byte) '\''},                      //''' ( 39) |11111111|11011
            {new []{T,T,T,T,F,T,T,F, T}, (byte) '('},                               //'(' ( 40) |11110110|1
            {new []{T,T,T,T,F,T,T,T, F}, (byte) ')'},                               //')' ( 41) |11110111|0
            {new []{T,T,T,T,T,T,T,T, T,F,T,T}, (byte) '*'},                         //'*' ( 42) |11111111|1011
            {new []{T,T,T,T,T,T,T,T, F,T,F}, (byte) '+'},                           //'+' ( 43) |11111111|010
            {new []{T,F,F,F,T,F}, (byte) ','},                                      //',' ( 44) |100010
            {new []{T,F,F,F,T,T}, (byte) '-'},                                      //'-' ( 45) |100011
            {new []{T,F,F,T,F,F}, (byte) '.'},                                      //'.' ( 46) |100100
            {new []{T,T,F,T,F,T,T}, (byte) '/'},                                    //'/' ( 47) |1101011
            {new []{F,F,F,T}, (byte) '0'},                                          //'0' ( 48) |0001                    
            {new []{F,F,T,F}, (byte) '1'},                                          //'1' ( 49) |0010
            {new []{F,F,T,T}, (byte) '2'},                                          //'2' ( 50) |0011
            {new []{F,T,F,F,F}, (byte) '3'},                                        //'3' ( 51) |01000
            {new []{F,T,F,F,T}, (byte) '4'},                                        //'4' ( 52) |01001
            {new []{F,T,F,T,F}, (byte) '5'},                                        //'5' ( 53) |01010
            {new []{T,F,F,T,F,T}, (byte) '6'},                                      //'6' ( 54) |100101
            {new []{T,F,F,T,T,F}, (byte) '7'},                                      //'7' ( 55) |100110
            {new []{F,T,F,T,T}, (byte) '8'},                                        //'8' ( 56) |01011
            {new []{F,T,T,F,F}, (byte) '9'},                                        //'9' ( 57) |01100
            {new []{F,T,T,F,T}, (byte) ':'},                                        //':' ( 58) |01101
            {new []{T,T,T,T,F,T,T,T, T}, (byte) ';'},                               //';' ( 59) |11110111|1
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,F,T,F}, (byte) '<'},                 //'<' ( 60) |11111111|11111010|
            {new []{T,T,F,T,T,F,F}, (byte) '='},                                    //'=' ( 61) |1101100
            {new []{T,T,T,T,T,T,T,T, T,T,T,F,F}, (byte) '>'},                       //'>' ( 62) |11111111|11100
            {new []{T,T,T,T,T,T,T,T, T,T,F,F}, (byte) '?'},                         //'?' ( 63) |11111111|1100
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,F,T,T}, (byte) '@'},                 //'@' ( 64) |11111111|11111011|
            {new []{T,T,F,T,T,F,T}, (byte) 'A'},                                    //'A' ( 65) |1101101
            {new []{T,T,T,F,T,F,T,F}, (byte) 'B'},                                  //'B' ( 66) |11101010|
            {new []{T,T,T,F,T,F,T,T}, (byte) 'C'},                                  //'C' ( 67) |11101011|
            {new []{T,T,T,F,T,T,F,F}, (byte) 'D'},                                  //'D' ( 68) |11101100|
            {new []{T,T,T,F,T,T,F,T}, (byte) 'E'},                                  //'E' ( 69) |11101101|
            {new []{T,T,T,F,T,T,T,F}, (byte) 'F'},                                  //'F' ( 70) |11101110|
            {new []{T,F,F,T,T,T}, (byte) 'G'},                                      //'G' ( 71) |100111
            {new []{T,T,T,T,T,F,F,F, F}, (byte) 'H'},                               //'H' ( 72) |11111000|0
            {new []{T,T,T,F,T,T,T,T}, (byte) 'I'},                                  //'I' ( 73) |11101111|
            {new []{T,T,T,T,F,F,F,F}, (byte) 'J'},                                  //'J' ( 74) |11110000|
            {new []{T,T,T,T,T,T,T,F, F,T}, (byte) 'K'},                             //'K' ( 75) |11111110|01
            {new []{T,T,T,T,T,F,F,F, T}, (byte) 'L'},                               //'L' ( 76) |11111000|1
            {new []{T,F,T,F,F,F}, (byte) 'M'},                                      //'M' ( 77) |101000
            {new []{T,T,T,T,F,F,F,T}, (byte) 'N'},                                  //'N' ( 78) |11110001|  
            {new []{T,T,T,T,F,F,T,F}, (byte) 'O'},                                  //'O' ( 79) |11110010|
            {new []{T,T,T,T,T,F,F,T, F}, (byte) 'P'},                               //'P' ( 80) |11111001|0
            {new []{T,T,T,T,T,T,T,F, T,F}, (byte) 'Q'},                             //'Q' ( 81) |11111110|10
            {new []{T,T,T,T,T,F,F,T, T}, (byte) 'R'},                               //'R' ( 82) |11111001|1
            {new []{T,F,T,F,F,T}, (byte) 'S'},                                      //'S' ( 83) |101001
            {new []{F,T,T,T,F}, (byte) 'T'},                                        //'T' ( 84) |01110
            {new []{T,T,T,T,T,F,T,F, F}, (byte) 'U'},                               //'U' ( 85) |11111010|0
            {new []{T,T,T,T,T,F,T,F, T}, (byte) 'V'},                               //'V' ( 86) |11111010|1
            {new []{T,T,T,T,F,F,T,T}, (byte) 'W'},                                  //'W' ( 87) |11110011|
            {new []{T,T,T,T,T,T,T,F, T,T}, (byte) 'X'},                             //'X' ( 88) |11111110|11
            {new []{T,T,T,T,T,F,T,T, F}, (byte) 'Y'},                               //'Y' ( 89) |11111011|0
            {new []{T,T,T,T,T,T,T,T, F,F}, (byte) 'Z'},                             //'Z' ( 90) |11111111|00
            {new []{T,T,T,T,T,T,T,T, F,T,T}, (byte) '['},                           //'[' ( 91) |11111111|011
            {new []{T,T,T,T,T,T,T,T, T,T,T,F,T}, (byte) '\\'},                      //'\' ( 92) |11111111|11101
            {new []{T,T,T,T,T,T,T,T, T,F,F}, (byte) ']'},                           //']' ( 93) |11111111|100
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,F,F}, (byte) '^'},                   //'^' ( 94) |11111111|1111100
            {new []{T,T,T,T,T,F,T,T, T}, (byte) '_'},                               //'_' ( 95) |11111011|1
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, F}, (byte) '`'},              //'`' ( 96) |11111111|11111111|0
            {new []{F,T,T,T,T}, (byte) 'a'},                                        //'a' ( 97) |01111
            {new []{T,T,F,T,T,T,F}, (byte) 'b'},                                    //'b' ( 98) |1101110
            {new []{T,F,T,F,T,F}, (byte) 'c'},                                      //'c' ( 99) |101010
            {new []{T,F,T,F,T,T}, (byte) 'd'},                                      //'d' (100) |101011
            {new []{T,F,F,F,F}, (byte) 'e'},                                        //'e' (101) |10000 
            {new []{T,T,F,T,T,T,T}, (byte) 'f'},                                    //'f' (102) |1101111
            {new []{T,T,T,F,F,F,F}, (byte) 'g'},                                    //'g' (103) |1110000
            {new []{T,T,T,F,F,F,T}, (byte) 'h'},                                    //'h' (104) |1110001
            {new []{T,F,T,T,F,F}, (byte) 'i'},                                      //'i' (105) |101100
            {new []{T,T,T,T,T,T,F,F, F}, (byte) 'j'},                               //'j' (106) |11111100|0
            {new []{T,T,T,T,T,T,F,F, T}, (byte) 'k'},                               //'k' (107) |11111100|1
            {new []{T,T,T,F,F,T,F}, (byte) 'l'},                                    //'l' (108) |1110010
            {new []{T,F,T,T,F,T}, (byte) 'm'},                                      //'m' (109) |101101
            {new []{T,F,T,T,T,F}, (byte) 'n'},                                      //'n' (110) |101110
            {new []{T,F,T,T,T,T}, (byte) 'o'},                                      //'o' (111) |101111
            {new []{T,T,F,F,F,F}, (byte) 'p'},                                      //'p' (112) |110000
            {new []{T,T,T,T,T,T,F,T, F}, (byte) 'q'},                               //'q' (113) |11111101|0
            {new []{T,T,F,F,F,T}, (byte) 'r'},                                      //'r' (114) |110001
            {new []{T,T,F,F,T,F}, (byte) 's'},                                      //'s' (115) |110010
            {new []{T,T,F,F,T,T}, (byte) 't'},                                      //'t' (116) |110011
            {new []{T,T,F,T,F,F}, (byte) 'u'},                                      //'u' (117) |110100
            {new []{T,T,T,F,F,T,T}, (byte) 'v'},                                    //'v' (118) |1110011
            {new []{T,T,T,T,F,T,F,F}, (byte) 'w'},                                  //'w' (119) |11110100|
            {new []{T,T,T,F,T,F,F}, (byte) 'x'},                                    //'x' (120) |1110100
            {new []{T,T,T,T,F,T,F,T}, (byte) 'y'},                                  //'y' (121) |11110101|
            {new []{T,T,T,T,T,T,F,T, T}, (byte) 'z'},                               //'z' (122) |11111101|1
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,F,F}, (byte) '{'},                 //'{' (123) |11111111|11111100|
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,F,T}, (byte) '|'},                     //'|' (124) |11111111|111101
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,F,T}, (byte) '}'},                 //'}' (125) |11111111|11111101|
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,F}, (byte) '~'},                 //'~' (126) |11111111|11111110|
            {new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,F,T,T,T,F,T}, Eos}        //EOS (256) |11111111|11111111|11011101|
        };

        public const byte Eos = (byte)255; // TODO this is Incorrect! Eos code is 256. Think how to handle it.

        public bool this[int index]
        {
            get
            {
                int curIndex = 0;
                foreach (var bits in _symbolBitsMap.Keys)
                {
                    curIndex += bits.Length;
                    if (curIndex > index)
                    {
                        curIndex -= bits.Length;

                        return bits[index % curIndex];
                    }
                }

                throw new ArgumentOutOfRangeException("index was out of symbol table range");
            }
        }

        public int Size 
        {
            get
            {
                return _symbolBitsMap.Keys.Sum(value => value.Length);
            }
        }

        public Map HuffmanTable
        {
            get
            {
                return _symbolBitsMap;
            }
            set
            {
                Debug.Assert(value != null);
                _symbolBitsMap = value;
            }
        }

        public byte GetByte(bool[] bits)
        {
            foreach (var tableBits in _symbolBitsMap.Keys)
            {
                if (tableBits.Length != bits.Length)
                    continue;

                bool match = true;
                for (byte i = 0; i < bits.Length; i++)
                {
                    if (bits[i] != tableBits[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return _symbolBitsMap[tableBits];
                }
            }

            //TODO Add exception type
            throw new Exception("symbol does not present in the alphabeth");
        }

        public byte GetByte(List<bool> bits)
        {
            foreach (var tableBits in _symbolBitsMap.Keys)
            {
                if (tableBits.Length != bits.Count)
                    continue;

                bool match = true;
                for (byte i = 0; i < bits.Count; i++)
                {
                    if (bits[i] != tableBits[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return _symbolBitsMap[tableBits];
                }
            }

            //TODO Add exception type
            throw new Exception("symbol does not present in the alphabeth");
        }

        public bool[] GetBits(byte c)
        {
            var val = _symbolBitsMap.FirstOrDefault(pair => pair.Value == c).Key;

            if (val == null)
                throw new Exception("No such symbol in the alphabeth");

            return val;
        }
    }
}
