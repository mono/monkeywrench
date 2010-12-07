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
			return GZCompressManaged (filename);
		}

		public static string GZCompressGZ (string filename)
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

		public static string GZCompressManaged (string filename)
		{
			string outfile = null;

			try {
				outfile = Path.GetTempFileName ();

				using (FileStream infs = new FileStream (filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					using (FileStream outfs = new FileStream (outfile, FileMode.Open, FileAccess.Write, FileShare.Read)) {
						using (GZipStream gz = new GZipStream (outfs, CompressionMode.Compress)) {
							byte [] buffer = new byte [1024];
							int bytes_read;
							while ((bytes_read = infs.Read (buffer, 0, buffer.Length)) > 0) {
								gz.Write (buffer, 0, bytes_read);
							}
						}
					}
				}

				return outfile;
			} catch (Exception ex) {
				Logger.Log ("Failed to compress file: {0} {1}", filename, ex);
				FileUtilities.TryDeleteFile (outfile);
				return null;
			}
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

		public static string MD5BytesToString (byte [] bytes)
		{
			StringBuilder result = new StringBuilder (16);
			for (int i = 0; i < bytes.Length; i++)
				result.Append (bytes [i].ToString ("x2"));
			return result.ToString ();
		}

		public static string CalculateMD5 (Stream str)
		{
			return MD5BytesToString (CalculateMD5_Bytes (str));
		}

		public static byte [] CalculateMD5_Bytes (Stream str)
		{
			using (MD5CryptoServiceProvider md5_provider = new MD5CryptoServiceProvider ()) {
				return md5_provider.ComputeHash (str);
			}
		}
		/// <summary>
		/// Tries to delete a file, does not throw any exceptions.
		/// </summary>
		/// <param name="filename"></param>
		public static void TryDeleteFile (string filename)
		{
			try {
				if (!string.IsNullOrEmpty (filename) && File.Exists (filename))
					File.Delete (filename);
			} catch {
			}
		}

		/// <summary>
		/// Tries to delete a directory recursively. Will try as hard as possible to delete as much as possible,
		/// i.e. it won't stop if for instance one file can't be deleted.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="recursive"></param>
		public static void TryDeleteDirectoryRecursive (string directory)
		{
			try {
				TryDeleteDirectoryRecursive (directory, directory);
			} catch (Exception ex) {
				Logger.Log ("TryDeleteDirectoryRecursive ({0}): Could not delete directory recursively: {1}", directory, ex.Message);
			}
		}

		private static void MakeReadWrite (string path)
		{
			try {
				FileAttributes attributes = File.GetAttributes (path);
				if ((attributes & FileAttributes.ReadOnly) != 0) {
					File.SetAttributes (path, attributes & ~FileAttributes.ReadOnly);
				}
			} catch (Exception ex) {
				Logger.Log ("MakeReadWrite ({0}): Could not make RW: {2}", path, ex.Message);
			}
		}

		private static void TryDeleteRODirectory (string path)
		{
			try {
				MakeReadWrite (path);
				Directory.Delete (path);
			} catch (Exception ex) {
				Logger.Log ("TryDeleteRODirectory ({0}): Could not delete the directory: {1}", path, ex.Message);
				/* Ignore any exceptions */
			}
		}

		private static void TryDeleteROFile (string path)
		{
			try {
				MakeReadWrite (path);
				File.Delete (path);
			} catch (Exception ex) {
				Logger.Log ("TryDeleteROFile ({0}): Could not delete the file: {1}", path, ex.Message);
				/* Ignore any exceptions */
			}
		}

		private static void TryDeleteDirectoryRecursive (string root, string path)
		{
			try {
				foreach (string dir in Directory.GetDirectories (path)) {
					if ((File.GetAttributes (dir) & FileAttributes.ReparsePoint) != 0) {
						/* this is a symlink */
						TryDeleteRODirectory (dir);
					} else {
						TryDeleteDirectoryRecursive (root, dir);
					}
				}
			} catch (Exception ex) {
				Logger.Log ("TryDeleteDirectoryRecursive ({0}, {1}): Could not recurse into subdirectories: {2}", root, path, ex.Message);
			}

			foreach (string file in Directory.GetFiles (path)) {
				TryDeleteROFile (file);
			}

			TryDeleteRODirectory (path);
		}
	}
}
