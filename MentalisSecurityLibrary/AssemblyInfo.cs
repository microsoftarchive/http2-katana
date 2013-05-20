/*
 *   Mentalis.org Security Library
 * 
 *     Copyright © 2002-2005, The Mentalis.org Team
 *     All rights reserved.
 *     http://www.mentalis.org/
 *
 *
 *   Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions
 *   are met:
 *
 *     - Redistributions of source code must retain the above copyright
 *        notice, this list of conditions and the following disclaimer. 
 *
 *     - Neither the name of the Mentalis.org Team, nor the names of its contributors
 *        may be used to endorse or promote products derived from this
 *        software without specific prior written permission. 
 *
 *   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 *   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 *   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 *   FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
 *   THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 *   INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 *   (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 *   SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 *   HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 *   STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *   ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 *   OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Reflection;
using System.Security.Permissions;
using System.Runtime.CompilerServices;

//
// General Information about an assembly is controlled through the following 
// set of attributes.
//
[assembly: AssemblyTitle("Mentalis.org Security Library")]
[assembly: AssemblyDescription("Mentalis.org Security Library for the .NET runtime. Visit http://www.mentalis.org/ for more information.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("The Mentalis.org Team")]
[assembly: AssemblyProduct("Mentalis.org Security Library")]
[assembly: AssemblyCopyright("Copyright © 2002-2007, The Mentalis.org Team")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
//[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode=true)]


//
//  Thanks To
//  ~~~~~~~~~
//    We would like to thank the following people for their help with the security library:
//
//		Chris Hudel		[method implementation]
//		Jonni Faiga		[bugfix]
//		Michael J. Moore	[bugfix]
//		John Doty		[bugfix]
//		Hernan de Lahitte	[bugfix, method implementation]
//		Brandon			[bugfix]
//		Daryn Kiely		[bugfix]
//		Kevin Knoop		[method implementation, performance optimizations, bugfix]
//		Gabriele Zannoni	[method implementation]
//		Stefan Bernbo		[bugfix]
//		Martin Plante		[bugfix]
//		Neil			[bugfix]
//		Alfonso Ferrandez	[bugfix]
//		Dmytro			[bugfix]
//		Paul Grebenc		[bugfix]
//

//
// Version information for an assembly consists of the following four values:
//
[assembly: AssemblyVersion("1.0.13.718")]

// In order to sign your assembly you must specify a key to use. Refer to the 
// Microsoft .NET Framework documentation for more information on assembly signing.
//
// Use the attributes below to control which key is used for signing. 
//
// Notes: 
//   (*) If no key is specified, the assembly is not signed.
//   (*) KeyName refers to a key that has been installed in the Crypto Service
//       Provider (CSP) on your machine. KeyFile refers to a file which contains
//       a key.
//   (*) If the KeyFile and the KeyName values are both specified, the 
//       following processing occurs:
//       (1) If the KeyName can be found in the CSP, that key is used.
//       (2) If the KeyName does not exist and the KeyFile does exist, the key 
//           in the KeyFile is installed into the CSP and used.
//   (*) In order to create a KeyFile, you can use the sn.exe (Strong Name) utility.
//       When specifying the KeyFile, the location of the KeyFile should be
//       relative to the project output directory which is
//       %Project Directory%\obj\<configuration>. For example, if your KeyFile is
//       located in the project directory, you would specify the AssemblyKeyFile 
//       attribute as [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) Delay Signing is an advanced option - see the Microsoft .NET Framework
//       documentation for more information on this.
//
[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile(@"\\Mentalis\pieter\cabackup\slkey.snk")]
[assembly: AssemblyKeyName("")]
