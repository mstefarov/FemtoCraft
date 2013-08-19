// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt

namespace FemtoCraft {
    enum OpCode : byte {
        Handshake = 0,
        Ping = 1,
        MapBegin = 2,
        MapChunk = 3,
        MapEnd = 4,
        SetBlockClient = 5,
        SetBlockServer = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Move = 10,
        Rotate = 11,
        RemoveEntity = 12,
        Message = 13,
        Kick = 14,
        SetPermission = 15
    }
}