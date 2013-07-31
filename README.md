http2-katana
============

HTTP/2.0 with Katana

Please note following:
* Be aware this code is released as a prototype and it currently handles only basic static files. 
* This code should only be used for interoperability testing.
* This prototype supports header compression (draft-ietf-httpbis-header-compression-01), and HTTP/2.0 features such as stream multiplexing, and negotiation mechanisms such as ALPN and HTTP upgrade, as well as the ability to establish direct HTTP/2.0 connections. 
* It does not yet implement server push or flow control. 
* Apart from the server component, this prototype also includes a test command line client that makes HTTP/2.0 protocol requests to the server.
* The following endpoints can be used for testing but will only work with an HTTP/2.0-enabled browser or HTTP/2.0 client (included in this source):
  http://http2katanatest.cloudapp.net:8080/ and 
  https://http2katanatest.cloudapp.net:8443/
* If these endpoints are hit with a non-HTTP/2.0 browser or client, the connection will timeout. 
* Please open a bug in the repo if you encounter any issues with this code or the endpoints
* An enhanced implementation is under development that can be used to host ‘real’ web sites.
