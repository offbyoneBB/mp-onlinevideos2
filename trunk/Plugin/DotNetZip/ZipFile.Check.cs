// ZipFile.Check.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs):
// Time-stamp: <2010-January-05 13:15:32>
//
// ------------------------------------------------------------------
//
// This module defines the methods for doing Checks on zip files.
// These are not necessary to include in the Reduced or CF
// version of the library.
//
// ------------------------------------------------------------------
//


using System;
using System.IO;
using System.Collections.Generic;

namespace Ionic.Zip
{
    public partial class ZipFile
    {
        /// <summary>
        ///   Checks a zip file to see if its directory is consistent.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   In cases of data error, the directory within a zip file can get out
        ///   of synch with the entries in the zip file.  This method checks the
        ///   given zip file and returns true if this has occurred.
        /// </para>
        ///
        /// <para> This method may take a long time to run for large zip files.  </para>
        ///
        /// <para>
        ///   This method is not supported in the Reduced or Compact Framework
        ///   versions of DotNetZip.
        /// </para>
        ///
        /// <para>
        ///   Developers using COM can use the <see
        ///   cref="ComHelper.CheckZip(String)">ComHelper.CheckZip(String)</see>
        ///   method.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zipFileName">The filename to of the zip file to check.</param>
        ///
        /// <returns>true if the named zip file checks OK. Otherwise, false. </returns>
        ///
        /// <seealso cref="FixZipDirectory(string)"/>
        /// <seealso cref="CheckZip(string,bool,out System.Collections.ObjectModel.ReadOnlyCollection&lt;String&gt;)"/>
        public static bool CheckZip(string zipFileName)
        {
            System.Collections.ObjectModel.ReadOnlyCollection<String> ignoredMessages;
            return CheckZip(zipFileName, false, out ignoredMessages);
        }


        /// <summary>
        ///   Checks a zip file to see if its directory is consistent,
        ///   and optionally fixes the directory if necessary.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   In cases of data error, the directory within a zip file can get out of
        ///   synch with the entries in the zip file.  This method checks the given
        ///   zip file, and returns true if this has occurred. It also optionally
        ///   fixes the zipfile, saving the fixed copy in <em>Name</em>_Fixed.zip.
        /// </para>
        ///
        /// <para>
        ///   This method may take a long time to run for large zip files.  It
        ///   will take even longer if the file actually needs to be fixed, and if
        ///   <c>fixIfNecessary</c> is true.
        /// </para>
        ///
        /// <para>
        ///   This method is not supported in the Reduced or Compact
        ///   Framework versions of DotNetZip.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zipFileName">The filename to of the zip file to check.</param>
        ///
        /// <param name="fixIfNecessary">If true, the method will fix the zip file if
        ///     necessary.</param>
        ///
        /// <param name="messages">
        /// a collection of messages generated while checking, indicating any problems that are found.
        /// </param>
        ///
        /// <returns>true if the named zip is OK; false if the file needs to be fixed.</returns>
        ///
        /// <seealso cref="CheckZip(string)"/>
        /// <seealso cref="FixZipDirectory(string)"/>
        public static bool CheckZip(string zipFileName, bool fixIfNecessary,
                                    out System.Collections.ObjectModel.ReadOnlyCollection<String> messages)
        {
            List<String> notes = new List<String>();
            ZipFile zip1 = null;
            ZipFile zip2 = null;
            bool isOk = true;
            try
            {
                zip1 = new ZipFile();
                zip1.FullScan = true;
                zip1.Initialize(zipFileName);

                zip2 = ZipFile.Read(zipFileName);

                foreach (var e1 in zip1)
                {
                    foreach (var e2 in zip2)
                    {
                        if (e1.FileName == e2.FileName)
                        {
                            if (e1._RelativeOffsetOfLocalHeader != e2._RelativeOffsetOfLocalHeader)
                            {
                                isOk = false;
                                notes.Add(String.Format("{0}: mismatch in RelativeOffsetOfLocalHeader  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._RelativeOffsetOfLocalHeader,
                                                        e2._RelativeOffsetOfLocalHeader));
                            }
                            if (e1._CompressedSize != e2._CompressedSize)
                            {
                                isOk = false;
                                notes.Add(String.Format("{0}: mismatch in CompressedSize  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._CompressedSize,
                                                        e2._CompressedSize));
                            }
                            if (e1._UncompressedSize != e2._UncompressedSize)
                            {
                                isOk = false;
                                notes.Add(String.Format("{0}: mismatch in UncompressedSize  (0x{1:X16} != 0x{2:X16})",
                                                        e1.FileName, e1._UncompressedSize,
                                                        e2._UncompressedSize));
                            }
                            if (e1.CompressionMethod != e2.CompressionMethod)
                            {
                                isOk = false;
                                notes.Add(String.Format("{0}: mismatch in CompressionMethod  (0x{1:X4} != 0x{2:X4})",
                                                        e1.FileName, e1.CompressionMethod,
                                                        e2.CompressionMethod));
                            }
                            if (e1.Crc != e2.Crc)
                            {
                                isOk = false;
                                notes.Add(String.Format("{0}: mismatch in Crc32  (0x{1:X4} != 0x{2:X4})",
                                                        e1.FileName, e1.Crc,
                                                        e2.Crc));
                            }

                            // found a match, so stop the inside loop
                            break;
                        }
                    }
                }

                zip2.Dispose();
                zip2 = null;

                if (!isOk && fixIfNecessary)
                {
                    string newFileName = Path.GetFileNameWithoutExtension(zipFileName);
                    newFileName = System.String.Format("{0}_fixed.zip", newFileName);
                    zip1.Save(newFileName);
                }
            }
            finally
            {
                if (zip1 != null) zip1.Dispose();
                if (zip2 != null) zip2.Dispose();
            }
            messages = notes.AsReadOnly(); // may or may not be empty
            return isOk;
        }



        /// <summary>
        ///   Rewrite the directory within a zipfile.
        /// </summary>
        ///
        /// <remarks>
        ///
        /// <para>
        ///   In cases of data error, the directory in a zip file can get out of
        ///   synch with the entries in the zip file.  This method attempts to fix
        ///   the zip file if this has occurred.
        /// </para>
        ///
        /// <para> This can take a long time for large zip files. </para>
        ///
        /// <para> This won't work if the zip file uses a non-standard
        /// code page - neither IBM437 nor UTF-8. </para>
        ///
        /// <para>
        ///   This method is not supported in the Reduced or Compact Framework
        ///   versions of DotNetZip.
        /// </para>
        ///
        /// <para>
        ///   Developers using COM can use the <see
        ///   cref="ComHelper.FixZipDirectory(String)">ComHelper.FixZipDirectory(String)</see>
        ///   method.
        /// </para>
        ///
        /// </remarks>
        ///
        /// <param name="zipFileName">The filename to of the zip file to fix.</param>
        ///
        /// <seealso cref="CheckZip(string)"/>
        /// <seealso cref="CheckZip(string,bool,out System.Collections.ObjectModel.ReadOnlyCollection&lt;String&gt;)"/>
        public static void FixZipDirectory(string zipFileName)
        {
            using (var zip = new ZipFile())
            {
                zip.FullScan = true;
                zip.Initialize(zipFileName);
                zip.Save(zipFileName);
            }
        }


        /// <summary>
        /// Provides a human-readable string with information about the ZipFile.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The information string contains 10 lines or so, about each ZipEntry,
        ///     describing whether encryption is in use, the compressed and uncompressed
        ///     length of the entry, the offset of the entry, and so on. As a result the
        ///     information string can be very long for zip files that contain many
        ///     entries.
        ///   </para>
        ///   <para>
        ///     This information is mostly useful for diagnostic purposes.
        ///   </para>
        /// </remarks>
        public string Info
        {
            get
            {
                var builder = new System.Text.StringBuilder();
                builder.Append(string.Format("ZipFile: {0}\n", this.Name));
                if (!string.IsNullOrEmpty(this._Comment))
                {
                    builder.Append(string.Format("  Comment: {0}\n", this._Comment));
                }
                if (this._versionMadeBy != 0)
                {
                    builder.Append(string.Format("  version made by: 0x{0:X4}\n", this._versionMadeBy));
                }
                if (this._versionNeededToExtract != 0)
                {
                    builder.Append(string.Format("  version needed to extract: 0x{0:X4}\n", this._versionNeededToExtract));
                }

                builder.Append(string.Format("  uses ZIP64: {0}\n", this.InputUsesZip64));

                builder.Append(string.Format("  disk number with CD: {0}\n", this._diskNumberWithCd));
                foreach (ZipEntry entry in this._entries.Values)
                {
                    builder.Append(entry.Info);
                }
                return builder.ToString();
            }
        }


    }

}