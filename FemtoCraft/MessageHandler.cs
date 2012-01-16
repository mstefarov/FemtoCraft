using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FemtoCraft {
    static class MessageHandler {
        public static void Parse( Player player, string message ) {}


        public static bool ContainsInvalidChars( IEnumerable<char> message ) {
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }
    }
}
