/*
 * FileUtilities.cs
 *
 * Authors:
 *   Rolf Bjarne Kvinge (RKvinge@novell.com)
 *   
 * Copyright 2009 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MonkeyWrench
{
	public static class FileUtilities
	{

		/// <summary>
		/// GZ-compresses a file and returns the filename of the compressed file.
		/// The caller should delete the compressed file once done with it.
		/// Returns null if the file couldn't be compressed.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static string GZCompress (string filename)
		{
			if (Environment.OSVersion.Platform != PlatformID.Unix)
				return null; /* the GZipStream compression method really sucks on MS, we could possibly implement it on mono only */

			string input = filename + ".builder";
			string gzfilename = input + ".gz";

			try {
				File.Copy (filename, input); // We need to make a copy since gzip will delete the original file.
				using (Process p = new Process ()) {
					p.StartInfo.FileName = "gzip";
					p.StartInfo.Arguments = input;
					p.Start ();
					if (!p.WaitForExit (1000 * 60 /* 1 minute*/)) {
						Console.WriteLine ("GZCompress: gzip didn't finish in time, killing it.");
						p.Kill ();
						return null;
					}
				}
			} catch (Exception ex) {
				Console.WriteLine ("GZCompress There was an exception while trying to compress the file '{0}': {1}\n{2}", filename, ex.Message, ex.StackTrace);
				return null;
			} finally {
				TryDeleteFile (input);
			}

			if (File.Exists (gzfilename))
				return gzfilename;

			return null;
		}

		public static bool GZUncompress (string filename)
		{
			string infile;
			string outfile;

			if (!filename.EndsWith (".gz")) {
				outfile = filename;
				infile = filename + ".gz";
				TryDeleteFile (infile);
				File.Move (outfile, infile);
				filename = infile;
			} else {
				outfile = filename.Substring (0, filename.Length - 3);
				infile = filename;
			}

			// Uncompress it
			using (FileStream infs = new FileStream (infile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				using (GZipStream gz = new GZipStream (infs, CompressionMode.Decompress)) {
					using (FileStream outfs = new FileStream (outfile, FileMode.Create, FileAccess.Write, FileShare.Read)) {
						byte [] buffer = new byte [1024];
						int bytes_read;
						while ((bytes_read = gz.Read (buffer, 0, buffer.Length)) > 0) {
							outfs.Write (buffer, 0, bytes_read);
						}
					}
				}
			}
			TryDeleteFile (infile);
			return true;
		}


		public static string GlobToRegExp (string expression)
		{
			char [] carr = expression.ToCharArray ();

			StringBuilder sb = new StringBuilder ();
			bool bDigit = false;

			for (int pos = 0; pos < carr.Length; pos++) {
				switch (carr [pos]) {
				case '?':
					sb.Append ('.');
					break;
				case '*':
					sb.Append (".*");
					break;
				case '#':
					if (bDigit) {
						sb.Append (@"\d{1}");
					} else {
						sb.Append (@"^\d{1}");
						bDigit = true;
					}
					break;
				case '[':
					StringBuilder gsb = ConvertGroupSubexpression (carr, ref pos);
					if (gsb.Length > 2) {
						sb.Append (gsb);
					}
					break;
				case '.':
					sb.Append ("[.]");
					break;
				default:
					sb.Append (carr [pos]);
					break;
				}
			}
			if (bDigit)
				sb.Append ('$');

			return sb.ToString ();
		}

		private static StringBuilder ConvertGroupSubexpression (char [] carr, ref int pos)
		{
			StringBuilder sb = new StringBuilder ();
			bool negate = false;

			while (!(carr [pos] == ']')) {
				if (negate) {
					sb.Append ('^');
					negate = false;
				}
				if (carr [pos] == '!') {
					sb.Remove (1, sb.Length - 1);
					negate = true;
				} else {
					sb.Append (carr [pos]);
				}
				pos++;
			}
			sb.Append (']');

			return sb;
		}

		/// <summary>
		/// Given the specified md5, returns the full path of the file as it would be stored in the filesystem.
		/// </summary>
		/// <param name="md5"></param>
		/// <returns></returns>
		public static string CreateFilename (string md5, bool is_gz_compressed, bool create_directory)
		{
			string file = md5;
			string path = Configuration.GetFilesDirectory ();

			path = Path.Combine (path, md5.Substring (0, 2));
			path = Path.Combine (path, md5.Substring (2, 2));
			
			if (create_directory && !Directory.Exists (path))
				Directory.CreateDirectory (path);

			path = Path.Combine (path, md5);

			if (is_gz_compressed)
				path += ".gz";

			return path;
		}

		private static string MD5BytesToString (byte [] bytes)
		{
			StringBuilder result = new StringBuilder (16);
			for (int i = 0; i < bytes.Length; i++)
				result.Append (bytes [i].ToString ("x2"));
			return result.ToString ();
		}

		public static string CalculateMD5 (Stream str)
		{
			using (MD5CryptoServiceProvider md5_provider = new MD5CryptoServiceProvider ()) {
					return MD5BytesToString (md5_provider.ComputeHash (str));
			}
		}

		/// <summary>
		/// Tries to delete a file, does not throw any exceptions.
		/// </summary>
		/// <param name="filename"></param>
		public static void TryDeleteFile (string filename)
		{
			try {
				if (File.Exists (filename))
					File.Delete (filename);
			} catch {
			}
		}
	}
}
