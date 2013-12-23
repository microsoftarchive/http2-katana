using System.Collections.Generic;

namespace Microsoft.Http2.Protocol.Compression.Huffman
{
    using Map = Dictionary<bool[], byte>;

    internal partial class HuffmanCodesTable
    {
        #region RequestTable

        private Map _reqSymbolBitsMap = new Map
            {
                {new[] {T,T,T,F,T,F,F,F}, (byte) ' '},                                          //' ' ( 32) |11101000|
                {new[] {T,T,T,T,T,T,T,T, T,T,F,F}, (byte) '!'},                                 //'!' ( 33) |11111111|1100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,F,T,F}, (byte) '"'},                             //'"' ( 34) |11111111|111010
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,F,F}, (byte) '#'},                           //'#' ( 35) |11111111|1111100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,F,T}, (byte) '$'},                           //'$' ( 36) |11111111|1111101
                {new[] {T,F,F,T,F,F}, (byte) '%'},                                              //'%' ( 37) |100100
                {new[] {T,T,F,T,T,T,F}, (byte) '&'},                                            //'&' ( 38) |1101110
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,F}, (byte) '\''},                          //''' ( 39) |11111111|1111110
                {new[] {T,T,T,T,T,T,T,T, F,T,F}, (byte) '('},                                   //'(' ( 40) |11111111|010
                {new[] {T,T,T,T,T,T,T,T, F,T,T}, (byte) ')'},                                   //')' ( 41) |11111111|011
                {new[] {T,T,T,T,T,T,T,F, T,F}, (byte) '*'},                                     //'*' ( 42) |11111110|10
                {new[] {T,T,T,T,T,T,T,T, T,F,F}, (byte) '+'},                                   //'+' ( 43) |11111111|100
                {new[] {T,T,T,F,T,F,F,T}, (byte) ','},                                          //',' ( 44) |11101001|
                {new[] {T,F,F,T,F,T}, (byte) '-'},                                              //'-' ( 45) |100101
                {new[] {F,F,T,F,F}, (byte) '.'},                                                //'.' ( 46) |00100
                {new[] {F,F,F,F}, (byte) '/'},                                                  //'/' ( 47) |0000
                {new[] {F,F,T,F,T}, (byte) '0'},                                                //'0' ( 48) |00101                    
                {new[] {F,F,T,T,F}, (byte) '1'},                                                //'1' ( 49) |00110
                {new[] {F,F,T,T,T}, (byte) '2'},                                                //'2' ( 50) |00111
                {new[] {T,F,F,T,T,F}, (byte) '3'},                                              //'3' ( 51) |100110
                {new[] {T,F,F,T,T,T}, (byte) '4'},                                              //'4' ( 52) |100111
                {new[] {T,F,T,F,F,F}, (byte) '5'},                                              //'5' ( 53) |101000
                {new[] {T,F,T,F,F,T}, (byte) '6'},                                              //'6' ( 54) |101001
                {new[] {T,F,T,F,T,F}, (byte) '7'},                                              //'7' ( 55) |101010
                {new[] {T,F,T,F,T,T}, (byte) '8'},                                              //'8' ( 56) |101011
                {new[] {T,F,T,T,F,F}, (byte) '9'},                                              //'9' ( 57) |101100
                {new[] {T,T,T,T,F,T,T,F, F}, (byte) ':'},                                       //':' ( 58) |11110110|0
                {new[] {T,T,T,F,T,F,T,F}, (byte) ';'},                                          //';' ( 59) |11101010|
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,F}, (byte) '<'},                    //'<' ( 60) |11111111|11111111|10
                {new[] {T,F,T,T,F,T}, (byte) '='},                                              //'=' ( 61) |101101
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,F, F}, (byte) '>'},                      //'>' ( 62) |11111111|11111110|0
                {new[] {T,T,T,T,F,T,T,F ,T}, (byte) '?'},                                       //'?' ( 63) |11110110|1
                {new[] {T,T,T,T,T,T,T,T, T,T,T,F,T,T}, (byte) '@'},                             //'@' ( 64) |11111111|111011
                {new[] {T,T,F,T,T,T,T}, (byte) 'A'},                                            //'A' ( 65) |1101111
                {new[] {T,T,T,F,T,F,T,T}, (byte) 'B'},                                          //'B' ( 66) |11101011|
                {new[] {T,T,T,F,T,T,F,F}, (byte) 'C'},                                          //'C' ( 67) |11101100|
                {new[] {T,T,T,F,T,T,F,T}, (byte) 'D'},                                          //'D' ( 68) |11101101|
                {new[] {T,T,T,F,T,T,T,F}, (byte) 'E'},                                          //'E' ( 69) |11101110|
                {new[] {T,T,T,F,F,F,F}, (byte) 'F'},                                            //'F' ( 70) |1110000
                {new[] {T,T,T,T,F,T,T,T, F}, (byte) 'G'},                                       //'G' ( 71) |11110111|0
                {new[] {T,T,T,T,F,T,T,T, T}, (byte) 'H'},                                       //'H' ( 72) |11110111|1
                {new[] {T,T,T,T,T,F,F,F, F}, (byte) 'I'},                                       //'I' ( 73) |11111000|0
                {new[] {T,T,T,T,T,F,F,F, T}, (byte) 'J'},                                       //'J' ( 74) |11111000|1
                {new[] {T,T,T,T,T,T,T,F, T,T}, (byte) 'K'},                                     //'K' ( 75) |11111110|11
                {new[] {T,T,T,T,T,F,F,T, F}, (byte) 'L'},                                       //'L' ( 76) |11111001|0
                {new[] {T,T,T,F,T,T,T,T}, (byte) 'M'},                                          //'M' ( 77) |11101111|
                {new[] {T,T,T,T,T,F,F,T, T}, (byte) 'N'},                                       //'N' ( 78) |11111001|1  
                {new[] {T,T,T,T,T,F,T,F, F}, (byte) 'O'},                                       //'O' ( 79) |11111010|0
                {new[] {T,T,T,T,T,F,T,F, T}, (byte) 'P'},                                       //'P' ( 80) |11111010|1
                {new[] {T,T,T,T,T,F,T,T, F}, (byte) 'Q'},                                       //'Q' ( 81) |11111011|0
                {new[] {T,T,T,T,T,F,T,T, T}, (byte) 'R'},                                       //'R' ( 82) |11111011|1
                {new[] {T,T,T,T,F,F,F,F}, (byte) 'S'},                                          //'S' ( 83) |11110000|
                {new[] {T,T,T,T,F,F,F,T}, (byte) 'T'},                                          //'T' ( 84) |11110001|
                {new[] {T,T,T,T,T,T,F,F, F}, (byte) 'U'},                                       //'U' ( 85) |11111100|0
                {new[] {T,T,T,T,T,T,F,F, T}, (byte) 'V'},                                       //'V' ( 86) |11111100|1
                {new[] {T,T,T,T,T,T,F,T, F}, (byte) 'W'},                                       //'W' ( 87) |11111101|0
                {new[] {T,T,T,T,T,T,F,T, T}, (byte) 'X'},                                       //'X' ( 88) |11111101|1
                {new[] {T,T,T,T,T,T,T,F, F}, (byte) 'Y'},                                       //'Y' ( 89) |11111110|0
                {new[] {T,T,T,T,T,T,T,T, F,F}, (byte) 'Z'},                                     //'Z' ( 90) |11111111|00
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,F,F}, (byte) '['},                             //'[' ( 91) |11111111|111100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,T,T,T,F,T,T, F,T,F}, (byte) '\\'},//'\' ( 92) |11111111|11111111|11111011|010
                {new[] {T,T,T,T,T,T,T,T, T,T,T,F,F}, (byte) ']'},                               //']' ( 93) |11111111|11100
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,F,T}, (byte) '^'},                             //'^' ( 94) |11111111|111101
                {new[] {T,F,T,T,T,F}, (byte) '_'},                                              //'_' ( 95) |101110
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,F}, (byte) '`'},                  //'`' ( 96) |11111111|11111111|110
                {new[] {F,T,F,F,F}, (byte) 'a'},                                              //'a' ( 97) |01000
                {new[] {T,F,T,T,T,T}, (byte) 'b'},                                              //'b' ( 98) |101111
                {new[] {F,T,F,F,T}, (byte) 'c'},                                                //'c' ( 99) |01001
                {new[] {T,T,F,F,F,F}, (byte) 'd'},                                              //'d' (100) |110000
                {new[] {F,F,F,T}, (byte) 'e'},                                                  //'e' (101) |0001
                {new[] {T,T,F,F,F,T}, (byte) 'f'},                                              //'f' (102) |110001
                {new[] {T,T,F,F,T,F}, (byte) 'g'},                                              //'g' (103) |110010
                {new[] {T,T,F,F,T,T}, (byte) 'h'},                                              //'h' (104) |110011
                {new[] {F,T,F,T,F}, (byte) 'i'},                                                //'i' (105) |01010
                {new[] {T,T,T,F,F,F,T}, (byte) 'j'},                                            //'j' (106) |1110001
                {new[] {T,T,T,F,F,T,F}, (byte) 'k'},                                            //'k' (107) |1110010
                {new[] {F,T,F,T,T}, (byte) 'l'},                                                //'l' (108) |01011 
                {new[] {T,T,F,T,F,F}, (byte) 'm'},                                              //'m' (109) |110100
                {new[] {F,T,T,F,F}, (byte) 'n'},                                                //'n' (110) |01100
                {new[] {F,T,T,F,T}, (byte) 'o'},                                                //'o' (111) |01101
                {new[] {F,T,T,T,F}, (byte) 'p'},                                                //'p' (112) |01110
                {new[] {T,T,T,T,F,F,T,F}, (byte) 'q'},                                          //'q' (113) |11110010|
                {new[] {F,T,T,T,T}, (byte) 'r'},                                                //'r' (114) |01111
                {new[] {T,F,F,F,F}, (byte) 's'},                                                //'s' (115) |10000
                {new[] {T,F,F,F,T}, (byte) 't'},                                                //'t' (116) |10001
                {new[] {T,T,F,T,F,T}, (byte) 'u'},                                              //'u' (117) |110101
                {new[] {T,T,T,F,F,T,T}, (byte) 'v'},                                            //'v' (118) |1110011
                {new[] {T,T,F,T,T,F}, (byte) 'w'},                                              //'w' (119) |110110
                {new[] {T,T,T,T,F,F,T,T}, (byte) 'x'},                                          //'x' (120) |11110011|
                {new[] {T,T,T,T,F,T,F,F}, (byte) 'y'},                                          //'y' (121) |11110100|
                {new[] {T,T,T,T,F,T,F,T}, (byte) 'z'},                                          //'z' (122) |11110101|
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,F, T}, (byte) '{'},                      //'{' (123) |11111111|11111110|1
                {new[] {T,T,T,T,T,T,T,T, T,F,T}, (byte) '|'},                                   //'|' (124) |11111111|101
                {new[] {T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, F}, (byte) '}'},                      //'}' (125) |11111111|11111111|0
                {new[] {T,T,T,T,T,T,T,T, T,T,F,T}, (byte) '~'},                                 //'~' (126) |11111111|1101
            };
        #endregion

        #region Response table
        private Map _respSymbolBitsMap = new Map
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
            //{new []{T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,F,T,T,T,F,T}, Eos}      //EOS (256) |11111111|11111111|11011101|
        };
        #endregion

        public static readonly bool[] ReqEos = new[] { T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,T,T,F,T,T,T, F,F }; //|11111111|11111111|11110111|00

        public static readonly bool[] RespEos = new[] { T,T,T,T,T,T,T,T, T,T,T,T,T,T,T,T, T,T,F,T,T,T,F,T }; //|11111111|11111111|11011101|
    }
}
