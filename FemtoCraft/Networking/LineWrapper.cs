// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
// #define DEBUG_LINE_WRAPPER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace FemtoCraft {
    sealed class LineWrapper : IEnumerable<Packet>, IEnumerator<Packet> {
        const string DefaultPrefixString = "> ";
        static readonly byte[] DefaultPrefix;


        static LineWrapper() {
            DefaultPrefix = Encoding.ASCII.GetBytes( DefaultPrefixString );
        }


        const int PacketSize = 66; // opCode + id + 64
        const byte DefaultColor = (byte)'f';

        public Packet Current { get; private set; }

        bool expectingColor;// whether next input character is expected to be a color code
        byte color,         // color that the next inserted character should be
             lastColor;     // used to detect duplicate color codes

        bool hadColor,      // used to see if white (&f) color codes should be inserted
             canWrap;       // used to see if a word needs to be forcefully wrapped (i.e. doesn't fit in one line)

        int spaceCount,     // used to track spacing between words
            wordLength;     // used to see whether to wrap at hyphens

        readonly byte[] prefix;

        readonly byte[] input;
        int inputIndex;

        byte[] output;
        int outputStart,
            outputIndex;

        int wrapInputIndex,     // index of the nearest line-wrapping opportunity in the input buffer 
            wrapOutputIndex;    // corresponding index in the output buffer
        byte wrapColor;         // value of "color" field at the wrapping point


        public LineWrapper( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            input = Encoding.ASCII.GetBytes( message );
            prefix = DefaultPrefix;
            Reset();
        }


        public void Reset() {
            color = DefaultColor;
            wordLength = 0;
            inputIndex = 0;
            wrapInputIndex = 0;
            wrapOutputIndex = 0;
        }


        public bool MoveNext() {
            if( inputIndex >= input.Length ) {
                return false;
            }

            output = new byte[PacketSize];
            output[0] = (byte)OpCode.Message;
            Current = new Packet( output );

            hadColor = false;
            canWrap = false;
            expectingColor = false;

            outputStart = 2;
            outputIndex = outputStart;
            spaceCount = 0;

            lastColor = DefaultColor;

            wrapOutputIndex = outputStart;
            wrapColor = color;

            // Prepend line prefix, if needed
            if( inputIndex > 0 && prefix.Length > 0 ) {
                int preBufferInputIndex = inputIndex;
                byte preBufferColor = color;
                color = DefaultColor;
                inputIndex = 0;
                wrapInputIndex = 0;
                wordLength = 0;
                while( inputIndex < prefix.Length ) {
                    byte ch = prefix[inputIndex];
                    if( ProcessChar( ch ) ) {
                        // Should never happen, since prefix is under 48 chars
                        throw new Exception( "Prefix required wrapping." );
                    }
                    inputIndex++;
                }
                inputIndex = preBufferInputIndex;
                color = preBufferColor;
                wrapColor = preBufferColor;
            }

            wordLength = 0;
            wrapInputIndex = inputIndex;
            canWrap = false; // to prevent line-wrapping at prefix

            // Append as much of the remaining input as possible
            while( inputIndex < input.Length ) {
                byte ch = input[inputIndex];
                if( ProcessChar( ch ) ) {
                    // Line wrap is needed
                    PrepareOutput();
                    return true;
                }
                inputIndex++;
            }

            // No more input (last line)
            PrepareOutput();
            return true;
        }


        bool ProcessChar( byte ch ) {
            switch( ch ) {
                case (byte)' ':
                    canWrap = true;
                    expectingColor = false;
                    if( spaceCount == 0 ) {
                        // first space after a word, set wrapping point
                        wrapInputIndex = inputIndex;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                    }
                    spaceCount++;
                    break;

                case (byte)'&':
                    // skip double ampersands
                    expectingColor = !expectingColor;
                    break;

                case (byte)'-':
                    if( spaceCount > 0 ) {
                        // set wrapping point, if at beginning of a word
                        wrapInputIndex = inputIndex;
                        wrapColor = color;
                    }
                    expectingColor = false;
                    if( !Append( ch ) ) {
                        if( canWrap ) {
                            // word doesn't fit in line, backtrack to wrapping point
                            inputIndex = wrapInputIndex;
                            outputIndex = wrapOutputIndex;
                            color = wrapColor;
                        } // else force wrap (word is too long), don't backtrack
                        return true;
                    }
                    spaceCount = 0;
                    if( wordLength > 2 ) {
                        // allow wrapping after hyphen, if at least 2 word characters precede this hyphen
                        wrapInputIndex = inputIndex + 1;
                        wrapOutputIndex = outputIndex;
                        wrapColor = color;
                        wordLength = 0;
                        canWrap = true;
                    }
                    break;

                case (byte)'\n':
                    // break the line early
                    inputIndex++;
                    return true;

                default:
                    if( expectingColor ) {
                        expectingColor = false;
                        if( ProcessColor( ref ch ) ) {
                            // valid color code
                            color = ch;
                            hadColor = true;
                        } // else color code is invalid, skip
                    } else {
                        if( spaceCount > 0 ) {
                            // set wrapping point, if at beginning of a word
                            wrapInputIndex = inputIndex;
                            wrapColor = color;
                        }
                        if( ch == 0 || ch > 127 ) {
                            // replace unprintable chars with '?'
                            ch = (byte)'?';
                        }
                        if( !Append( ch ) ) {
                            if( canWrap ) {
                                inputIndex = wrapInputIndex;
                                outputIndex = wrapOutputIndex;
                                color = wrapColor;
                            } // else word is too long, don't backtrack to wrap
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }


        void PrepareOutput() {
            // pad the packet with spaces
            for( int i = outputIndex; i < PacketSize; i++ ) {
                output[i] = (byte)' ';
            }
        }


        bool Append( byte ch ) {
            bool prependColor =
                // color changed since last inserted character, OR
                lastColor != color ||
                // a color code is needed to preserve leading whitespace
                (color == DefaultColor && hadColor && outputIndex == outputStart && spaceCount > 0);

            // calculate the number of characters to insert
            int bytesToInsert = 1 + spaceCount;
            if( prependColor ) bytesToInsert += 2;
            if( outputIndex + bytesToInsert > PacketSize ) {
                return false;
            }

            // append color, if changed since last inserted character
            if( prependColor ) {
                output[outputIndex++] = (byte)'&';
                output[outputIndex++] = color;
                lastColor = color;
            }

            if( spaceCount > 0 && outputIndex > outputStart ) {
                // append spaces that accumulated since last word
                while( spaceCount > 0 ) {
                    output[outputIndex++] = (byte)' ';
                    spaceCount--;
                }
                wordLength = 0;
            }
            wordLength += bytesToInsert;

            // append character
            output[outputIndex++] = ch;
            return true;
        }


        static bool ProcessColor( ref byte ch ) {
            if( ch >= (byte)'A' && ch <= (byte)'Z' ) {
                ch += 32;
            }
            return ch >= (byte)'a' && ch <= (byte)'f' ||
                   ch >= (byte)'0' && ch <= (byte)'9';
        }


        [NotNull]
        object IEnumerator.Current {
            get { return Current; }
        }


        void IDisposable.Dispose() { }


        #region IEnumerable<Packet> Members

        public IEnumerator<Packet> GetEnumerator() {
            return this;
        }


        IEnumerator IEnumerable.GetEnumerator() {
            return this;
        }

        #endregion
    }
}