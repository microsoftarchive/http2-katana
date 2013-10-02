http2-katana
============

HTTP/2.0 with Katana

Please note following
---------------------

* Be aware this code is released as a prototype and it currently handles only basic static files. 
* This code should only be used for interoperability testing.
* This prototype supports header compression (draft-ietf-httpbis-header-compression-03), and HTTP/2.0 (draft-ietf-httpbis-http2-06) features such as stream multiplexing, and negotiation mechanisms such as ALPN and HTTP upgrade, as well as the ability to establish direct HTTP/2.0 connections. 
* It does not yet implement server push. 
* Apart from the server component, this prototype also includes a test command line client that makes HTTP/2.0 protocol requests to the server.
* The following endpoints can be used for testing but will only work with an HTTP/2.0-enabled browser or HTTP/2.0 client (included in this source):
  http://http2katanatest.cloudapp.net:8080/ and 
  https://http2katanatest.cloudapp.net:8443/
* If these endpoints are hit with a non-HTTP/2.0 browser or client, the connection will timeout. 
* Please open a bug in the repo if you encounter any issues with this code or the endpoints
* An enhanced implementation is under development that can be used to host ‘real’ web sites.
* This does not yet support TLS > 1.0. 


Build and deployment instructions
---------------------------------

1. Build Http2.Katana solution
2. Navigate to Drop folder
3. In Drop folder you can start Http2.Owin.StaticFiles.Sample.exe or Http2.Owin.WebApi.Sample.exe to run server as console app
4. Alternatively you can install server as service using Microsoft.Http2.Owin.Service.exe as service executable
   For example: sc create http2Service displayname= "http2Service" binpath= "<your path>\Microsoft.Http2.Owin.Service.exe"
   
Configuration
-------------

* You can use Http2.Owin.StaticFiles.Sample.exe.config/Http2.Owin.WebApi.Sample.exe.config to configure console server or Microsoft.Http2.Owin.Service.exe.config to configure service
* Configuration options description:
 * useSecurePort:		true or false
 * secureAddress:		address for https server that uses ALPN
 * unsecureAddress:	address for http server that uses UPGRADE
 * handshakeOptions: 	avaliable options are 'handshake' and 'no-handshake'. This option allows to enable or disable handshake (Handshake is always enabled for now)
 * prioritiesOptions:	avaliable options are 'priorities' and 'no-priorities'. This option allows to turn stream priorities on or off (Priorities are always enabled for now)
 * flowcontrolOptions:	avaliable options are 'flowcontrol' and 'no-flowcontrol'. This option allows to turn flow control on or off (FlowControl is always enabled for now)
 * securePort: 		http server port
 * unsecurePort:	 	https server port
