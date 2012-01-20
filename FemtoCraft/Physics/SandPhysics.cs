// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
// Contributed by Conrad "Redshift" Morgan
namespace FemtoCraft {
    sealed class SandPhysics {
        readonly Map map;

        public SandPhysics( Map map ) {
            this.map = map;
        }


        public void Trigger( int x, int y, int z, Block type ) {
            if( !Config.PhysicsSand ) return;
            if( type == Block.Sand || type == Block.Gravel ) {
                int dropHeight = Drop( x, y, z );
                if( dropHeight != z ) {
                    Send( x, y, z, dropHeight, type );
                    DropSpread( x, y, dropHeight );
                }
            }
            StartSpread( x, y, z );
        }


        void Propagate( int sx, int sy, int sz, int dx, int dy, int dz ) {
            int x = dx + sx;
            int y = dy + sy;
            int z = dz + sz;
            Block type = map.GetBlock( x, y, z );
            if( type == Block.Sand || type == Block.Gravel ) {
                int dropHeight = Drop( x, y, z );
                if( dropHeight != z ) {
                    Send( x, y, z, dropHeight, type );
                    DropSpread( x, y, dropHeight );
                    ChangeSpread( x, y, z, dx, dy, dz );
                }
            }
        }


        void StartSpread( int x, int y, int z ) {
            DropSpread( x, y, z );
            Propagate( x, y, z, 0, 0, 1 );
        }


        void DropSpread( int x, int y, int z ) {
            Propagate( x, y, z, 1, 0, 0 );
            Propagate( x, y, z, -1, 0, 0 );
            Propagate( x, y, z, 0, 1, 0 );
            Propagate( x, y, z, 0, -1, 0 );
            Propagate( x, y, z, 0, 0, -1 );
        }


        void ChangeSpread( int x, int y, int z, int dx, int dy, int dz ) {
            if( dx != -1 )
                Propagate( x, y, z, 1, 0, 0 );
            if( dx != 1 )
                Propagate( x, y, z, -1, 0, 0 );
            if( dy != -1 )
                Propagate( x, y, z, 0, 1, 0 );
            if( dy != 1 )
                Propagate( x, y, z, 0, -1, 0 );
            if( dz != 1 )
                Propagate( x, y, z, 0, 0, -1 );
            Propagate( x, y, z, 0, 0, 1 );
        }


        void Send( int x, int y, int z, int fz, Block newType ) {
            map.SetBlock( null, x, y, z, Block.Air );
            map.SetBlock( null, x, y, fz, newType );
        }


        int Drop( int x, int y, int z ) {
            for( ; z > 0; z-- ) {
                switch( map.GetBlock( x, y, z - 1 ) ) {
                    case Block.Air:
                    case Block.Water:
                    case Block.Lava:
                    case Block.StillWater:
                    case Block.StillLava:
                        continue;
                    default:
                        return z;
                }
            }
            return z;
        }
    }
}