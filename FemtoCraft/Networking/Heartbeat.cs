// Part of FemtoCraft | Copyright 2012 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;

namespace FemtoCraft {
    static class Heartbeat {
        const string MinecraftUri = "http://www.minecraft.net/heartbeat.jsp";
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds( 10 );
        static readonly TimeSpan Delay = TimeSpan.FromSeconds( 25 );
        const string UrlFileName = "externalurl.txt";


        public static void Start() {
            Thread heartbeatThread = new Thread( Beat ) { IsBackground = true };
            heartbeatThread.Start();
        }


        static void Beat() {
            while( true ) {
                try {
                    string requestUri =
                        String.Format( "{0}?public={1}&max={2}&users={3}&port={4}&version=7&salt={5}&name={6}",
                                       MinecraftUri,
                                       Config.Public,
                                       Config.MaxPlayers,
                                       Server.Players.Length,
                                       Config.Port,
                                       Uri.EscapeDataString( Server.Salt ),
                                       Uri.EscapeDataString( Config.ServerName ) );

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create( requestUri );
                    request.Method = "GET";
                    request.Timeout = (int)Timeout.TotalMilliseconds;
                    request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );
                    request.UserAgent = Server.VersionString;
                    request.ServicePoint.BindIPEndPointDelegate = BindIPEndPointCallback;

                    using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() ) {
                        using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                            string responseText = responseReader.ReadToEnd().Trim();
                            Uri newUri;
                            if( Uri.TryCreate( responseText, UriKind.Absolute, out newUri ) ) {
                                if( newUri != Server.Uri ) {
                                    File.WriteAllText( UrlFileName, newUri.ToString() );
                                    Server.Uri = newUri;
                                    Logger.Log( "Heartbeat: {0}", newUri );
                                }
                            } else {
                                Logger.LogWarning( "Heartbeat: Minecraft.net replied with: {0}", responseText );
                            }
                        }
                    }
                    Thread.Sleep( Delay );

                } catch( Exception ex ) {
                    Logger.LogError( "Heartbeat: {0}: {1}", ex.GetType().Name, ex.Message );
                }
            }
        }


        static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( Config.IP, 0 );
        }
    }
}