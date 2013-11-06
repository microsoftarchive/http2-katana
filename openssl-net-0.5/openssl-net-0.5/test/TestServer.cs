// Copyright (c) 2009 Ben Henderson
// Copyright (c) 2012 Frank Laub
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. The name of the author may not be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
// NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using OpenSSL;
using OpenSSL.Core;
using OpenSSL.X509;
using OpenSSL.SSL;
using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class TestServer : TestBase
	{
		public X509Chain serverCAChain = null;
		public X509Certificate serverCertificate = null;
		public X509List clientCertificateList = null;
		public X509Chain clientCAChain = null;

		private X509Certificate LoadPKCS12Certificate(string certFilename, string password) {
			using (BIO certFile = BIO.File(certFilename, "r")) {
				return X509Certificate.FromPKCS12(certFile, password);
			}
		}

		private X509Chain LoadCACertificateChain(string caFilename) {
			using (BIO bio = BIO.File(caFilename, "r")) {
				return new X509Chain(bio);
			}
		}

		/*
		 * Basic Client Server Tests
		 * 
		 * Test the default SslStream implementation in sync and async methods
		 *      No Server certificate verification
		 *      Default SSL protocols
		 *      
		 */
		public class SyncServerTests
		{
			protected TestServer testServer = null;
			protected byte[] clientMessage = Encoding.ASCII.GetBytes("This is a message from the client");
			protected byte[] serverMessage = Encoding.ASCII.GetBytes("This is a message from the server");
			protected byte[] serverReadBuffer = new byte[256];
			protected byte[] clientReadBuffer = new byte[256];
			protected TcpListener listener = null;
			protected TcpClient client = null;
			protected SslStream sslStream = null;
			protected string testName = "Not set";
			protected RemoteCertificateValidationHandler clientRemoteCertificateValidationCallback = null;
			protected LocalCertificateSelectionHandler clientLocalCertificateSelectionCallback = null;
			protected RemoteCertificateValidationHandler serverRemoteCertificateValidationCallback = null;

			public SyncServerTests(TestServer testServer) {
				this.testServer = testServer;
			}

			#region BasicSyncServerTest
			public void BasicServerTest() {
				try {
					testName = "BasicServerTest";
					AcceptConnection(); // sets the client member
					sslStream = new SslStream(client.GetStream(), false);
					sslStream.AuthenticateAsServer(testServer.serverCertificate);
					// Do the server read, and write of the messages
					if (DoServerReadWrite()) {
						Shutdown(true);
					}
					else {
						Shutdown(false);
					}
				}
				catch (Exception ex) {
					Shutdown(false);
				}
			}

			protected void AcceptConnection() {
				listener = new TcpListener(IPAddress.Any, 8443);
				listener.Start(5);
				client = listener.AcceptTcpClient();
			}

			protected bool DoServerReadWrite() {
				bool ret = true;

				// Read the client message
				sslStream.Read(serverReadBuffer, 0, serverReadBuffer.Length);
				if (String.Compare(serverReadBuffer.ToString(), clientMessage.ToString()) != 0) {
					Console.WriteLine("BasicServerTest Read Failure:\nExpected:{0}\nGot:{1}", clientMessage.ToString(), serverReadBuffer.ToString());
					ret = false;
				}
				// Write the server message
				sslStream.Write(serverMessage, 0, serverMessage.Length);

				return ret;
			}

			protected void Shutdown(bool passed) {
				if (sslStream != null) {
					sslStream.Close();
				}
				if (client != null) {
					client.Close();
				}
				if (listener != null) {
					listener.Stop();
				}
				if (passed) {
					Console.WriteLine("{0} - passed", testName);
				}
				else {
					Console.WriteLine("{0} - failed", testName);
				}
			}

			public void BasicClientTest() {
				try {
					testName = "BasicClientTest";
					client = new TcpClient("localhost", 8443);
					sslStream = new SslStream(client.GetStream(), false);
					sslStream.AuthenticateAsClient("localhost");
					if (DoClientReadWrite()) {
						Shutdown(true);
					}
					else {
						Shutdown(false);
					}
				}
				catch (Exception ex) {
					Shutdown(false);
				}
			}

			protected bool DoClientReadWrite() {
				bool ret = true;

				// Write the client message
				sslStream.Write(clientMessage, 0, clientMessage.Length);
				// Read the server message
				sslStream.Read(clientReadBuffer, 0, clientReadBuffer.Length);
				if (String.Compare(clientReadBuffer.ToString(), serverMessage.ToString()) != 0) {
					Console.WriteLine("BasicServerTest Read Failure:\nExpected:{0}\nGot:{1}", serverMessage.ToString(), clientReadBuffer.ToString());
					ret = false;
				}
				sslStream.Close();
				client.Close();

				return ret;
			}

			#endregion  //BasicSyncServerTest

		}

		public void ServerTestThreadProc() {
			// Run synchronous tests
			SyncServerTests tests = new SyncServerTests(this);
			tests.BasicServerTest();
		}

		public void ClientTestThreadProc() {
			Thread.Sleep(500);  // Ensure that the server is ready!
			// Run synchronous tests
			SyncServerTests tests = new SyncServerTests(this);
			tests.BasicClientTest();
		}
		
		[Test]
		public void TestCase() {
			string serverCertPath = @"../../test/certs/server.pfx";
			string serverPrivateKeyPassword = "p@ssw0rd";
			string caFilePath = "../../test/certs/ca_chain.pem";
			string clientCertPath = "../../test/certs/client.pfx";
			string clientPrivateKeyPassword = "p@ssw0rd";

			// Initialize OpenSSL for multithreaded use
			ThreadInitialization.InitializeThreads();
			try {
				// Intitialize server certificates
				serverCAChain = LoadCACertificateChain(caFilePath);
				serverCertificate = LoadPKCS12Certificate(serverCertPath, serverPrivateKeyPassword);

				// Kick the server thread
				Thread serverThread = new Thread(new ThreadStart(ServerTestThreadProc));
				serverThread.Start();

				// Intialize the client certificates
				clientCAChain = LoadCACertificateChain(caFilePath);
				X509Certificate clientCert = LoadPKCS12Certificate(clientCertPath, clientPrivateKeyPassword);
				// Add the cert to the client certificate list
				clientCertificateList = new X509List();
				clientCertificateList.Add(clientCert);

				// Kick the client thread
				Thread clientThread = new Thread(new ThreadStart(ClientTestThreadProc));
				clientThread.Start();

				// Wait for the threads to exit
				serverThread.Join();
				clientThread.Join();

				// Cleanup
				serverCertificate.Dispose();
				serverCAChain.Dispose();
				clientCAChain.Dispose();
				clientCert.Dispose();
			}
			catch (Exception ex) {
				Console.WriteLine("Server test failed with exception: {0}", ex.Message);
			}
			ThreadInitialization.UninitializeThreads();
		}
	}
}
