// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
using System.Linq;

namespace FemtoCraft {
    static class MessageHandler {
        public static void Parse( Player player, string message ) {}


        public static bool ContainsInvalidChars( IEnumerable<char> message ) {
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }
    }
}
