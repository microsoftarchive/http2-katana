#http2-katana

This is an implementation of Hypertext Transfer Protocol version 2 in C# with OWIN Katana.
Be aware this code is released as a prototype of static files server, test client and Bing proxy demo server.
This should only be used for interoperability testing.


##Development Status
This prototype supports **h2-14** (http://tools.ietf.org/html/draft-ietf-httpbis-http2-14) and 
**headers compression 09** (http://tools.ietf.org/html/draft-ietf-httpbis-header-compression-09).

HTTP/2 Features  | Support
------------- | -------------
ALPN          | Yes
Upgrade       | Yes
SNI           | Yes
NPN           | No
Direct        | No
ALTSVC        | No

##Public Test Server
The following endpoints are available to try out http2-katana implementation:
* https://http2katanatest.cloudapp.net:8443/root/index.html (TLS + ALPN)

  This endpoint requires TLSv1.2 and DHE or EDCHE with GCM cipher suite.
* http://http2katanatest.cloudapp.net:8080/root/index.html (Upgrade)

  h2c-14 and http/1.1
* https://http2katanatest.cloudapp.net:8448/Y3A9NTcuNjE2NjY1fjM5Ljg2NjY2NSZsdmw9MyZzdHk9ciZxPVlhcm9zbGF2bA==

  This endpoint is Bing proxy demo, requires TLSv1.2 with ALPN extension as well.

##Build instructions:
Build Http2.Katana solution. All executables (static files server, test client, Bing proxy) and required libraries will be under ```Drop``` direcrtory. You can use ```Http2.Owin.StaticFiles.Sample.exe``` to start static files server or alternatively install server as windows service using ```Http2.Owin.Service.Sample.exe```. Also use ```Http2.Owin.BingProxy.Sample.exe``` to run Bing proxy demo server.
   
##Endpoints Configuration
All of servers under ```Drop``` folder could be configured with ```*.config``` files. The follwoing options are avaliable:

Key                | Description
-------------------| -------------
useSecurePort      | use secure adress or not
secureAddress      | address for ```https``` urls (ALPN)
unsecureAddress    | address for ```http``` urls (Upgrade)
handshakeOptions   | value of 'handshake' or 'no-handshake' (response by http/1.1)
server-name        | used by SNI to validate host name
securePort         | port number for secureAddress
unsecurePort       | port number for unsecureAddress

##Tests Running
It's easy. Just install ```XUnit``` framework extension for Visual Studio and run tests from ```Test Explorer``` window.
